using Amazon.S3;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Security;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
using CoreMs.DocumentMs.Core.Exceptions;
using CoreMs.DocumentMs.Core.Repositories;
using CoreMs.DocumentMs.Core.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// Property 3: Visibility-based access control — PUBLIC always accessible,
/// PRIVATE only by owner/admin, BY_LINK by owner/admin or valid token holder.
///
/// **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 11.1, 11.3**
/// </summary>
public class VisibilityAccessControlTests
{
    private readonly DocumentRepository _documentRepository;
    private readonly DocumentAccessTokenRepository _documentAccessTokenRepository;
    private readonly S3StorageService _storageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DocumentService _sut;
    private readonly Guid _ownerId = Guid.NewGuid();

    public VisibilityAccessControlTests()
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

        _currentUserService = Substitute.For<ICurrentUserService>();

        var documentOptions = Options.Create(new DocumentOptions
        {
            MaxUploadSizeBytes = 10 * 1024 * 1024,
            AllowedExtensions = ["pdf", "png", "txt"],
            LinkSigningKey = "test-key-minimum-32-characters-long!!"
        });
        var logger = Substitute.For<ILogger<DocumentService>>();

        _sut = new DocumentService(
            _documentRepository,
            _documentAccessTokenRepository,
            _storageService,
            _currentUserService,
            documentOptions,
            storageOptions,
            logger);
    }

    private DocumentEntity CreateDocument(Guid ownerId, DocumentVisibility visibility)
    {
        return new DocumentEntity
        {
            Uuid = Guid.NewGuid(),
            UserId = ownerId,
            Name = "test-doc",
            OriginalFilename = "test.pdf",
            Size = 1024,
            Extension = "pdf",
            ContentType = "application/pdf",
            Bucket = "test-bucket",
            ObjectKey = $"{ownerId}/test-key.pdf",
            Visibility = visibility,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void SetCurrentUser(Guid userId, params string[] roles)
    {
        _currentUserService.GetCurrentUserUuid().Returns(userId);
        _currentUserService.GetCurrentUserRoles().Returns(roles.ToList());
    }

    /// <summary>
    /// PUBLIC document — accessible by any user (different user than owner).
    /// Validates: Requirements 4.1, 11.1
    /// </summary>
    [Fact]
    public async Task PublicDocument_AccessibleByAnyUser()
    {
        var document = CreateDocument(_ownerId, DocumentVisibility.Public);
        var differentUser = Guid.NewGuid();
        SetCurrentUser(differentUser);

        _documentRepository.GetByUuidAsync(document.Uuid, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _sut.GetDocumentAsync(document.Uuid);

        result.Uuid.Should().Be(document.Uuid);
    }

    /// <summary>
    /// PRIVATE document — accessible by owner.
    /// Validates: Requirements 4.2, 4.3
    /// </summary>
    [Fact]
    public async Task PrivateDocument_AccessibleByOwner()
    {
        var document = CreateDocument(_ownerId, DocumentVisibility.Private);
        SetCurrentUser(_ownerId);

        _documentRepository.GetByUuidAsync(document.Uuid, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _sut.GetDocumentAsync(document.Uuid);

        result.Uuid.Should().Be(document.Uuid);
    }

    /// <summary>
    /// PRIVATE document — accessible by admin (DOCUMENT_MS_ADMIN role).
    /// Validates: Requirements 4.2, 4.4
    /// </summary>
    [Fact]
    public async Task PrivateDocument_AccessibleByAdmin()
    {
        var document = CreateDocument(_ownerId, DocumentVisibility.Private);
        var adminUser = Guid.NewGuid();
        SetCurrentUser(adminUser, CoreMsRoles.DocumentMsAdmin);

        _documentRepository.GetByUuidAsync(document.Uuid, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _sut.GetDocumentAsync(document.Uuid);

        result.Uuid.Should().Be(document.Uuid);
    }

    /// <summary>
    /// PRIVATE document — rejected for non-owner non-admin.
    /// Validates: Requirements 4.3
    /// </summary>
    [Fact]
    public async Task PrivateDocument_RejectedForNonOwnerNonAdmin()
    {
        var document = CreateDocument(_ownerId, DocumentVisibility.Private);
        var randomUser = Guid.NewGuid();
        SetCurrentUser(randomUser);

        _documentRepository.GetByUuidAsync(document.Uuid, Arg.Any<CancellationToken>())
            .Returns(document);

        var act = () => _sut.GetDocumentAsync(document.Uuid);

        var exception = await act.Should().ThrowAsync<ServiceException>();
        exception.Which.HttpStatusCode.Should().Be(403);
        exception.Which.Errors[0].ReasonCode.Should().Be("document.access_denied");
    }

    /// <summary>
    /// BY_LINK document — accessible by owner.
    /// Validates: Requirements 4.2, 11.3
    /// </summary>
    [Fact]
    public async Task ByLinkDocument_AccessibleByOwner()
    {
        var document = CreateDocument(_ownerId, DocumentVisibility.ByLink);
        SetCurrentUser(_ownerId);

        _documentRepository.GetByUuidAsync(document.Uuid, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _sut.GetDocumentAsync(document.Uuid);

        result.Uuid.Should().Be(document.Uuid);
    }

    /// <summary>
    /// BY_LINK document — rejected for non-owner non-admin (without token, via authenticated endpoint).
    /// Validates: Requirements 4.3, 11.3
    /// </summary>
    [Fact]
    public async Task ByLinkDocument_RejectedForNonOwnerNonAdmin()
    {
        var document = CreateDocument(_ownerId, DocumentVisibility.ByLink);
        var randomUser = Guid.NewGuid();
        SetCurrentUser(randomUser);

        _documentRepository.GetByUuidAsync(document.Uuid, Arg.Any<CancellationToken>())
            .Returns(document);

        var act = () => _sut.GetDocumentAsync(document.Uuid);

        var exception = await act.Should().ThrowAsync<ServiceException>();
        exception.Which.HttpStatusCode.Should().Be(403);
        exception.Which.Errors[0].ReasonCode.Should().Be("document.access_denied");
    }
}
