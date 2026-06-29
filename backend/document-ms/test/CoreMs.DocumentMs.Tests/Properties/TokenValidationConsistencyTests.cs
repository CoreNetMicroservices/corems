using System.Security.Claims;
using System.Text;
using Amazon.S3;
using CoreMs.Common.Exceptions;
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
/// Property 5: Token validation consistency — Token rejected if and only if
/// expired, revoked, or invalid signature.
///
/// **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
/// </summary>
public class TokenValidationConsistencyTests
{
    private const string SigningKey = "test-signing-key-minimum-32-characters-long!!";

    private readonly DocumentRepository _documentRepository;
    private readonly DocumentAccessTokenRepository _documentAccessTokenRepository;
    private readonly S3StorageService _storageService;
    private readonly PublicDocumentService _sut;
    private readonly Guid _documentUuid = Guid.NewGuid();

    public TokenValidationConsistencyTests()
    {
        var dbContext = Substitute.For<DbContext>();
        _documentRepository = Substitute.ForPartsOf<DocumentRepository>(dbContext);
        _documentAccessTokenRepository = Substitute.ForPartsOf<DocumentAccessTokenRepository>(dbContext);

        var mockS3Client = Substitute.For<IAmazonS3>();
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
            LinkSigningKey = SigningKey
        });
        var logger = Substitute.For<ILogger<PublicDocumentService>>();

        _sut = new PublicDocumentService(
            _documentRepository,
            _documentAccessTokenRepository,
            _storageService,
            documentOptions,
            logger);
    }

    private string CreateToken(Guid documentUuid, DateTime expiresAt)
    {
        var handler = new JsonWebTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", "document-access"), new Claim("doc", documentUuid.ToString())]),
            IssuedAt = DateTime.UtcNow,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return handler.CreateToken(descriptor);
    }

    private void SetupValidTokenEntity(string token)
    {
        var tokenHash = DocumentService.ComputeTokenHash(token);
        var tokenEntity = new DocumentAccessTokenEntity
        {
            DocumentUuid = _documentUuid,
            TokenHash = tokenHash,
            CreatedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsRevoked = false,
            AccessCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _documentAccessTokenRepository.GetByTokenHashAndDocumentAsync(tokenHash, _documentUuid, Arg.Any<CancellationToken>())
            .Returns(tokenEntity);
    }

    private void SetupByLinkDocument()
    {
        var document = new DocumentEntity
        {
            Uuid = _documentUuid,
            UserId = Guid.NewGuid(),
            Name = "test-doc",
            OriginalFilename = "test.pdf",
            Size = 1024,
            Extension = "pdf",
            ContentType = "application/pdf",
            ObjectKey = "some-object-key",
            Visibility = DocumentVisibility.ByLink,
            IsDeleted = false
        };

        _documentRepository.GetByUuidAsync(_documentUuid, Arg.Any<CancellationToken>())
            .Returns(document);
    }

    /// <summary>
    /// A valid token (not expired, not revoked, valid signature) succeeds.
    /// Validates: Requirements 6.1
    /// </summary>
    [Fact]
    public async Task ValidToken_NotExpired_NotRevoked_ValidSignature_Succeeds()
    {
        var token = CreateToken(_documentUuid, DateTime.UtcNow.AddHours(1));
        SetupValidTokenEntity(token);
        SetupByLinkDocument();

        var result = await _sut.GetDocumentByTokenAsync(token);

        result.Should().NotBeNull();
        result.Uuid.Should().Be(_documentUuid);
    }

    /// <summary>
    /// An expired token is rejected with TokenExpired error.
    /// Validates: Requirements 6.3
    /// </summary>
    [Fact]
    public async Task ExpiredToken_Rejected_WithTokenExpired()
    {
        // Create a token that was issued in the past and has already expired
        var handler = new JsonWebTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", "document-access"), new Claim("doc", _documentUuid.ToString())]),
            IssuedAt = DateTime.UtcNow.AddHours(-2),
            NotBefore = DateTime.UtcNow.AddHours(-2),
            Expires = DateTime.UtcNow.AddHours(-1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        var token = handler.CreateToken(descriptor);

        var act = () => _sut.GetDocumentByTokenAsync(token);

        var exception = await act.Should().ThrowAsync<ServiceException>();
        exception.Which.Errors[0].ReasonCode.Should().Be("document.token_expired");
    }

    /// <summary>
    /// A revoked token is rejected with TokenRevoked error.
    /// Validates: Requirements 6.4
    /// </summary>
    [Fact]
    public async Task RevokedToken_Rejected_WithTokenRevoked()
    {
        var token = CreateToken(_documentUuid, DateTime.UtcNow.AddHours(1));
        var tokenHash = DocumentService.ComputeTokenHash(token);

        var revokedTokenEntity = new DocumentAccessTokenEntity
        {
            DocumentUuid = _documentUuid,
            TokenHash = tokenHash,
            CreatedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsRevoked = true,
            RevokedBy = Guid.NewGuid(),
            RevokedAt = DateTime.UtcNow.AddMinutes(-5),
            AccessCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _documentAccessTokenRepository.GetByTokenHashAndDocumentAsync(tokenHash, _documentUuid, Arg.Any<CancellationToken>())
            .Returns(revokedTokenEntity);

        var act = () => _sut.GetDocumentByTokenAsync(token);

        var exception = await act.Should().ThrowAsync<ServiceException>();
        exception.Which.Errors[0].ReasonCode.Should().Be("document.token_revoked");
    }

    /// <summary>
    /// A token with invalid signature (garbled token) is rejected with TokenInvalid error.
    /// Validates: Requirements 6.2
    /// </summary>
    [Fact]
    public async Task InvalidSignature_GarbledToken_Rejected_WithTokenInvalid()
    {
        var garbledToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkb2N1bWVudC1hY2Nlc3MiLCJkb2MiOiIxMjM0NTY3OC0xMjM0LTEyMzQtMTIzNC0xMjM0NTY3ODkwYWIifQ.invalidsignaturehere";

        var act = () => _sut.GetDocumentByTokenAsync(garbledToken);

        var exception = await act.Should().ThrowAsync<ServiceException>();
        exception.Which.Errors[0].ReasonCode.Should().Be("document.token_invalid");
    }

    /// <summary>
    /// A token signed with a different key is rejected with TokenInvalid error.
    /// Validates: Requirements 6.2
    /// </summary>
    [Fact]
    public async Task TokenSignedWithDifferentKey_Rejected_WithTokenInvalid()
    {
        var handler = new JsonWebTokenHandler();
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("completely-different-signing-key-32chars!!"));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", "document-access"), new Claim("doc", _documentUuid.ToString())]),
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256)
        };
        var token = handler.CreateToken(descriptor);

        var act = () => _sut.GetDocumentByTokenAsync(token);

        var exception = await act.Should().ThrowAsync<ServiceException>();
        exception.Which.Errors[0].ReasonCode.Should().Be("document.token_invalid");
    }

    /// <summary>
    /// A token without the "doc" claim is rejected with TokenInvalid error.
    /// Validates: Requirements 6.2
    /// </summary>
    [Fact]
    public async Task TokenWithoutDocClaim_Rejected_WithTokenInvalid()
    {
        var handler = new JsonWebTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", "document-access")]),
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        var token = handler.CreateToken(descriptor);

        var act = () => _sut.GetDocumentByTokenAsync(token);

        var exception = await act.Should().ThrowAsync<ServiceException>();
        exception.Which.Errors[0].ReasonCode.Should().Be("document.token_invalid");
    }
}
