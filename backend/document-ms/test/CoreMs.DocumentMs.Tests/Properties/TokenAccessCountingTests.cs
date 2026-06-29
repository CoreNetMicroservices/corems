using System.Security.Claims;
using System.Text;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
using CoreMs.DocumentMs.Core.Repositories;
using CoreMs.DocumentMs.Core.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// Property 12: Token access counting — After N valid accesses, access count equals N
/// and last accessed timestamp is most recent.
///
/// **Validates: Requirements 6.5**
/// </summary>
public class TokenAccessCountingTests
{
    private const string SigningKey = "test-key-minimum-32-characters-long!!";

    private readonly DocumentRepository _documentRepository;
    private readonly DocumentAccessTokenRepository _documentAccessTokenRepository;
    private readonly S3StorageService _storageService;
    private readonly PublicDocumentService _sut;
    private readonly Guid _documentUuid = Guid.NewGuid();

    public TokenAccessCountingTests()
    {
        var dbContext = Substitute.For<DbContext>();
        _documentRepository = Substitute.ForPartsOf<DocumentRepository>(dbContext);
        _documentAccessTokenRepository = Substitute.ForPartsOf<DocumentAccessTokenRepository>(dbContext);

        var mockS3Client = Substitute.For<Amazon.S3.IAmazonS3>();
        var storageOptions = Options.Create(new StorageOptions
        {
            Endpoint = "http://localhost:9000",
            AccessKey = "key",
            SecretKey = "secret",
            Bucket = "test-bucket",
            ForcePathStyle = true
        });
        var storageLogger = Substitute.For<ILogger<S3StorageService>>();
        _storageService = new S3StorageService(mockS3Client, storageOptions, storageLogger);

        var documentOptions = Options.Create(new DocumentOptions
        {
            LinkSigningKey = SigningKey,
            BaseUrl = "http://localhost:5102"
        });
        var logger = Substitute.For<ILogger<PublicDocumentService>>();

        _sut = new PublicDocumentService(
            _documentRepository,
            _documentAccessTokenRepository,
            _storageService,
            documentOptions,
            logger);
    }

    private string CreateValidToken(Guid documentUuid, DateTime? expires = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", "document-access"),
                new Claim("doc", documentUuid.ToString())
            ]),
            IssuedAt = DateTime.UtcNow,
            Expires = expires ?? DateTime.UtcNow.AddHours(24),
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

    private void SetupMocks(string token, DocumentAccessTokenEntity tokenEntity)
    {
        var tokenHash = DocumentService.ComputeTokenHash(token);

        _documentAccessTokenRepository.GetByTokenHashAndDocumentAsync(
                tokenHash, _documentUuid, Arg.Any<CancellationToken>())
            .Returns(tokenEntity);

        var documentEntity = new DocumentEntity
        {
            Uuid = _documentUuid,
            UserId = Guid.NewGuid(),
            Name = "test-doc",
            OriginalFilename = "test.pdf",
            Size = 1024,
            Extension = "pdf",
            ContentType = "application/pdf",
            Bucket = "test-bucket",
            ObjectKey = "test-key",
            Visibility = DocumentVisibility.ByLink,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _documentRepository.GetByUuidAsync(_documentUuid, Arg.Any<CancellationToken>())
            .Returns(documentEntity);
    }

    /// <summary>
    /// After a single valid token access, AccessCount equals 1 and LastAccessedAt is set.
    /// Validates: Requirements 6.5
    /// </summary>
    [Fact]
    public async Task ValidTokenAccess_SingleCall_IncrementsAccessCountToOne()
    {
        var token = CreateValidToken(_documentUuid);
        var tokenEntity = new DocumentAccessTokenEntity
        {
            DocumentUuid = _documentUuid,
            TokenHash = DocumentService.ComputeTokenHash(token),
            CreatedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            AccessCount = 0,
            LastAccessedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        SetupMocks(token, tokenEntity);

        var beforeCall = DateTime.UtcNow;
        await _sut.GetDocumentByTokenAsync(token);
        var afterCall = DateTime.UtcNow;

        tokenEntity.AccessCount.Should().Be(1);
        tokenEntity.LastAccessedAt.Should().NotBeNull();
        tokenEntity.LastAccessedAt.Should().BeOnOrAfter(beforeCall);
        tokenEntity.LastAccessedAt.Should().BeOnOrBefore(afterCall);
    }

    /// <summary>
    /// After N valid accesses, AccessCount equals N and LastAccessedAt reflects the most recent.
    /// Validates: Requirements 6.5
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ValidTokenAccess_MultipleCalls_AccessCountEqualsCallCount(int n)
    {
        var token = CreateValidToken(_documentUuid);
        var tokenEntity = new DocumentAccessTokenEntity
        {
            DocumentUuid = _documentUuid,
            TokenHash = DocumentService.ComputeTokenHash(token),
            CreatedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            AccessCount = 0,
            LastAccessedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        SetupMocks(token, tokenEntity);

        DateTime lastCallTime = DateTime.MinValue;
        for (var i = 0; i < n; i++)
        {
            lastCallTime = DateTime.UtcNow;
            await _sut.GetDocumentByTokenAsync(token);
        }

        tokenEntity.AccessCount.Should().Be(n);
        tokenEntity.LastAccessedAt.Should().NotBeNull();
        tokenEntity.LastAccessedAt.Should().BeOnOrAfter(lastCallTime);
    }

    /// <summary>
    /// Each access updates LastAccessedAt to a value >= the previous LastAccessedAt (monotonically increasing).
    /// Validates: Requirements 6.5
    /// </summary>
    [Fact]
    public async Task ValidTokenAccess_LastAccessedAt_IsMonotonicallyIncreasing()
    {
        var token = CreateValidToken(_documentUuid);
        var tokenEntity = new DocumentAccessTokenEntity
        {
            DocumentUuid = _documentUuid,
            TokenHash = DocumentService.ComputeTokenHash(token),
            CreatedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            AccessCount = 0,
            LastAccessedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        SetupMocks(token, tokenEntity);

        DateTime? previousTimestamp = null;
        for (var i = 0; i < 5; i++)
        {
            await _sut.GetDocumentByTokenAsync(token);

            if (previousTimestamp.HasValue)
            {
                tokenEntity.LastAccessedAt.Should().BeOnOrAfter(previousTimestamp.Value);
            }

            previousTimestamp = tokenEntity.LastAccessedAt;
        }
    }

    /// <summary>
    /// Update is called on the repository for each access (verifying persistence tracking).
    /// Validates: Requirements 6.5
    /// </summary>
    [Fact]
    public async Task ValidTokenAccess_CallsUpdateOnRepository()
    {
        var token = CreateValidToken(_documentUuid);
        var tokenEntity = new DocumentAccessTokenEntity
        {
            DocumentUuid = _documentUuid,
            TokenHash = DocumentService.ComputeTokenHash(token),
            CreatedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            AccessCount = 0,
            LastAccessedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        SetupMocks(token, tokenEntity);

        await _sut.GetDocumentByTokenAsync(token);

        _documentAccessTokenRepository.Received(1).Update(tokenEntity);
    }

    /// <summary>
    /// AccessCount starts from a non-zero value and is still incremented correctly.
    /// Validates: Requirements 6.5
    /// </summary>
    [Theory]
    [InlineData(5, 3)]
    [InlineData(10, 1)]
    [InlineData(0, 7)]
    public async Task ValidTokenAccess_WithInitialCount_IncrementsCorrectly(int initialCount, int additionalAccesses)
    {
        var token = CreateValidToken(_documentUuid);
        var tokenEntity = new DocumentAccessTokenEntity
        {
            DocumentUuid = _documentUuid,
            TokenHash = DocumentService.ComputeTokenHash(token),
            CreatedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            AccessCount = initialCount,
            LastAccessedAt = initialCount > 0 ? DateTime.UtcNow.AddMinutes(-10) : null,
            CreatedAt = DateTime.UtcNow
        };

        SetupMocks(token, tokenEntity);

        for (var i = 0; i < additionalAccesses; i++)
        {
            await _sut.GetDocumentByTokenAsync(token);
        }

        tokenEntity.AccessCount.Should().Be(initialCount + additionalAccesses);
    }
}
