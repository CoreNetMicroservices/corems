using Amazon.S3;
using Amazon.S3.Model;
using CoreMs.Common.Security;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
using CoreMs.DocumentMs.Core.Models;
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
/// Property 4: Duplicate detection correctness — For any userId and originalFilename pair,
/// uploading with replace=true results in exactly one document.
///
/// **Validates: Requirements 3.1, 3.2**
/// </summary>
public class DuplicateDetectionTests
{
    private readonly DocumentRepository _documentRepository;
    private readonly DocumentAccessTokenRepository _documentAccessTokenRepository;
    private readonly IAmazonS3 _mockS3Client;
    private readonly S3StorageService _storageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DocumentService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public DuplicateDetectionTests()
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
    /// When replace=true and an existing document is found, the old document is removed
    /// from storage and repository, and a new one is added — resulting in exactly one document.
    /// Validates: Requirements 3.1, 3.2
    /// </summary>
    [Fact]
    public async Task ReplaceTrue_WithExistingDocument_RemovesOldAndCreatesNew()
    {
        var filename = "report.pdf";
        var existingEntity = new DocumentEntity
        {
            UserId = _userId,
            OriginalFilename = filename,
            ObjectKey = "old-object-key",
            Visibility = DocumentVisibility.Private
        };

        _documentRepository.GetByUserIdAndFilenameAsync(_userId, filename, Arg.Any<CancellationToken>())
            .Returns(existingEntity);

        _mockS3Client.DeleteObjectAsync(Arg.Any<DeleteObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse());
        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());

        var content = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(content);
        var request = new UploadDocumentRequest(null, null, null, null, Replace: true);

        await _sut.UploadAsync(stream, filename, content.Length, "application/pdf", request);

        // Old document removed from storage (S3 delete called)
        await _mockS3Client.Received(1).DeleteObjectAsync(
            Arg.Is<DeleteObjectRequest>(r => r.Key == existingEntity.ObjectKey),
            Arg.Any<CancellationToken>());
        // Old entity removed from repository
        _documentRepository.Received(1).Remove(existingEntity);
        // New entity added
        _documentRepository.Received(1).Add(Arg.Any<DocumentEntity>());
    }

    /// <summary>
    /// When replace=false, duplicates are not checked — a new document is created alongside any existing ones.
    /// Validates: Requirements 3.3
    /// </summary>
    [Fact]
    public async Task ReplaceFalse_WithExistingDocument_CreatesNewWithoutRemovingOld()
    {
        var filename = "report.pdf";

        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());

        var content = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(content);
        var request = new UploadDocumentRequest(null, null, null, null, Replace: false);

        await _sut.UploadAsync(stream, filename, content.Length, "application/pdf", request);

        // Should NOT check for duplicates when replace=false
        await _documentRepository.DidNotReceive().GetByUserIdAndFilenameAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        // New entity added
        _documentRepository.Received(1).Add(Arg.Any<DocumentEntity>());
    }

    /// <summary>
    /// When replace=true but no existing document is found, just create the new document without removal.
    /// Validates: Requirements 3.1, 3.2
    /// </summary>
    [Fact]
    public async Task ReplaceTrue_NoExistingDocument_CreatesNew()
    {
        var filename = "new-file.pdf";

        _documentRepository.GetByUserIdAndFilenameAsync(_userId, filename, Arg.Any<CancellationToken>())
            .Returns((DocumentEntity?)null);
        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());

        var content = new byte[] { 10, 20, 30 };
        using var stream = new MemoryStream(content);
        var request = new UploadDocumentRequest(null, null, null, null, Replace: true);

        await _sut.UploadAsync(stream, filename, content.Length, "application/pdf", request);

        // No S3 delete since nothing exists
        await _mockS3Client.DidNotReceive().DeleteObjectAsync(
            Arg.Any<DeleteObjectRequest>(), Arg.Any<CancellationToken>());
        _documentRepository.DidNotReceive().Remove(Arg.Any<DocumentEntity>());
        // New entity added
        _documentRepository.Received(1).Add(Arg.Any<DocumentEntity>());
    }

    /// <summary>
    /// After replace=true with existing document, the new entity has the correct userId and filename.
    /// Validates: Requirements 3.2
    /// </summary>
    [Fact]
    public async Task ReplaceTrue_NewDocument_HasSameUserIdAndFilename()
    {
        var filename = "data.txt";
        var existingEntity = new DocumentEntity
        {
            UserId = _userId,
            OriginalFilename = filename,
            ObjectKey = "old-key",
            Visibility = DocumentVisibility.Private
        };

        _documentRepository.GetByUserIdAndFilenameAsync(_userId, filename, Arg.Any<CancellationToken>())
            .Returns(existingEntity);
        _mockS3Client.DeleteObjectAsync(Arg.Any<DeleteObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse());
        _mockS3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());

        var content = new byte[] { 99, 98, 97 };
        using var stream = new MemoryStream(content);
        var request = new UploadDocumentRequest(null, null, null, null, Replace: true);

        await _sut.UploadAsync(stream, filename, content.Length, "text/plain", request);

        _documentRepository.Received(1).Add(Arg.Is<DocumentEntity>(e =>
            e.UserId == _userId && e.OriginalFilename == filename));
    }
}
