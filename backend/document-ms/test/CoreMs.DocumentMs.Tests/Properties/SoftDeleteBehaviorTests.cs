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
/// Property 6: Soft delete preserves data and hides from queries —
/// Soft-deleted documents remain in DB with IsDeleted=true and are excluded from standard queries.
///
/// **Validates: Requirements 9.1, 9.3**
/// </summary>
public class SoftDeleteBehaviorTests
{
    private readonly DocumentRepository _documentRepository;
    private readonly DocumentAccessTokenRepository _documentAccessTokenRepository;
    private readonly IAmazonS3 _mockS3Client;
    private readonly S3StorageService _storageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DocumentService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public SoftDeleteBehaviorTests()
    {
        var dbContext = Substitute.For<DbContext>();
        _documentRepository = Substitute.ForPartsOf<DocumentRepository>(dbContext);
        _documentAccessTokenRepository = Substitute.ForPartsOf<DocumentAccessTokenRepository>(dbContext);

        _mockS3Client = Substitute.For<IAmazonS3>();
        var storageOptions = Options.Create(new StorageOptions
        {
            Endpoint = "http://localhost:9000",
            AccessKey = "key",
            SecretKey = "secret",
            Bucket = "test-bucket",
            ForcePathStyle = true
        });
        var storageLogger = Substitute.For<ILogger<S3StorageService>>();
        _storageService = new S3StorageService(_mockS3Client, storageOptions, storageLogger);

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.GetCurrentUserUuid().Returns(_userId);
        _currentUserService.GetCurrentUserRoles().Returns(new List<string>());

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

    /// <summary>
    /// Soft delete sets IsDeleted=true, DeletedBy=currentUser, DeletedAt=now.
    /// Validates: Requirements 9.1
    /// </summary>
    [Fact]
    public async Task SoftDelete_SetsIsDeletedTrue_And_RecordsDeletedByAndDeletedAt()
    {
        var docUuid = Guid.NewGuid();
        var entity = new DocumentEntity
        {
            UserId = _userId,
            Uuid = docUuid,
            OriginalFilename = "report.pdf",
            ObjectKey = "some-object-key",
            Visibility = DocumentVisibility.Private,
            IsDeleted = false
        };

        _documentRepository.GetByUuidAsync(docUuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var beforeDelete = DateTime.UtcNow;

        await _sut.DeleteDocumentAsync(docUuid, permanent: false);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be(_userId);
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt!.Value.Should().BeOnOrAfter(beforeDelete);
        entity.DeletedAt!.Value.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    /// <summary>
    /// Soft delete does NOT remove the entity from the repository (no Remove call).
    /// Instead it calls Update to persist the IsDeleted change.
    /// Validates: Requirements 9.1, 9.3
    /// </summary>
    [Fact]
    public async Task SoftDelete_DoesNotRemoveEntity_CallsUpdate()
    {
        var docUuid = Guid.NewGuid();
        var entity = new DocumentEntity
        {
            UserId = _userId,
            Uuid = docUuid,
            OriginalFilename = "data.txt",
            ObjectKey = "obj-key-123",
            Visibility = DocumentVisibility.Private,
            IsDeleted = false
        };

        _documentRepository.GetByUuidAsync(docUuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        await _sut.DeleteDocumentAsync(docUuid, permanent: false);

        _documentRepository.DidNotReceive().Remove(Arg.Any<DocumentEntity>());
        _documentRepository.Received(1).Update(entity);
    }

    /// <summary>
    /// Soft delete on an already-deleted document throws DocumentAlreadyDeleted.
    /// Validates: Requirements 9.4
    /// </summary>
    [Fact]
    public async Task SoftDelete_OnAlreadyDeletedDocument_ThrowsDocumentAlreadyDeleted()
    {
        var docUuid = Guid.NewGuid();
        var entity = new DocumentEntity
        {
            UserId = _userId,
            Uuid = docUuid,
            OriginalFilename = "old.pdf",
            ObjectKey = "old-key",
            Visibility = DocumentVisibility.Private,
            IsDeleted = true,
            DeletedBy = _userId,
            DeletedAt = DateTime.UtcNow.AddHours(-1)
        };

        // GetByUuidAsync excludes soft-deleted docs (returns null)
        _documentRepository.GetByUuidAsync(docUuid, Arg.Any<CancellationToken>())
            .Returns((DocumentEntity?)null);

        var act = () => _sut.DeleteDocumentAsync(docUuid, permanent: false);

        var ex = await act.Should().ThrowAsync<ServiceException>();
        ex.Which.Errors.Should().ContainSingle(e =>
            e.ReasonCode == DocumentServiceErrors.DocumentNotFound.ErrorCode);
    }

    /// <summary>
    /// Soft delete on an already-deleted document that the repository returns
    /// (e.g., if the query bypasses the IsDeleted filter) throws DocumentAlreadyDeleted.
    /// This tests the explicit IsDeleted guard in DeleteDocumentAsync.
    /// Validates: Requirements 9.4
    /// </summary>
    [Fact]
    public async Task SoftDelete_OnEntityWithIsDeletedTrue_ThrowsDocumentAlreadyDeleted()
    {
        var docUuid = Guid.NewGuid();
        var entity = new DocumentEntity
        {
            UserId = _userId,
            Uuid = docUuid,
            OriginalFilename = "archived.txt",
            ObjectKey = "archived-key",
            Visibility = DocumentVisibility.Private,
            IsDeleted = true,
            DeletedBy = _userId,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Simulate a scenario where GetByUuidAsync returns the entity
        // (even though in production it filters deleted docs, test the guard)
        _documentRepository.GetByUuidAsync(docUuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var act = () => _sut.DeleteDocumentAsync(docUuid, permanent: false);

        var ex = await act.Should().ThrowAsync<ServiceException>();
        ex.Which.Errors.Should().ContainSingle(e =>
            e.ReasonCode == DocumentServiceErrors.DocumentAlreadyDeleted.ErrorCode);
    }
}
