using Amazon.S3;
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
/// Property 13: Partial update field preservation — Fields not included in an update request
/// retain their original values after the update.
///
/// **Validates: Requirements 7.1, 7.2**
/// </summary>
public class PartialUpdatePreservationTests
{
    private readonly DocumentRepository _documentRepository;
    private readonly DocumentAccessTokenRepository _documentAccessTokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly DocumentService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public PartialUpdatePreservationTests()
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
        var storageService = new S3StorageService(mockS3Client, storageOptions, storageLogger);

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
            storageService,
            _currentUserService,
            documentOptions,
            storageOptions,
            logger);
    }

    private DocumentEntity CreateKnownEntity() => new()
    {
        Id = 1,
        Uuid = Guid.NewGuid(),
        UserId = _userId,
        Name = "Original Name",
        OriginalFilename = "original.pdf",
        Size = 1024,
        Extension = "pdf",
        ContentType = "application/pdf",
        Bucket = "test-bucket",
        ObjectKey = "some/object/key.pdf",
        Visibility = DocumentVisibility.Private,
        UploadedById = _userId,
        UploadedByType = UploadedByType.User,
        Checksum = "abc123",
        Description = "Original Description",
        Version = 1,
        Tags = ["tag1", "tag2"],
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    /// <summary>
    /// When only Name is provided, Description, Visibility, and Tags remain unchanged.
    /// Validates: Requirements 7.1, 7.2
    /// </summary>
    [Fact]
    public async Task UpdateOnlyName_PreservesDescriptionVisibilityAndTags()
    {
        var entity = CreateKnownEntity();
        var uuid = entity.Uuid;

        _documentRepository.GetByUuidAsync(uuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var request = new UpdateDocumentRequest(
            Name: "Updated Name",
            Description: null,
            Visibility: null,
            Tags: null
        );

        var result = await _sut.UpdateDocumentAsync(uuid, request);

        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Original Description");
        result.Visibility.Should().Be(DocumentVisibility.Private);
        result.Tags.Should().BeEquivalentTo(new List<string> { "tag1", "tag2" });
    }

    /// <summary>
    /// When only Description is provided, Name, Visibility, and Tags remain unchanged.
    /// Validates: Requirements 7.1, 7.2
    /// </summary>
    [Fact]
    public async Task UpdateOnlyDescription_PreservesNameVisibilityAndTags()
    {
        var entity = CreateKnownEntity();
        var uuid = entity.Uuid;

        _documentRepository.GetByUuidAsync(uuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var request = new UpdateDocumentRequest(
            Name: null,
            Description: "Updated Description",
            Visibility: null,
            Tags: null
        );

        var result = await _sut.UpdateDocumentAsync(uuid, request);

        result.Name.Should().Be("Original Name");
        result.Description.Should().Be("Updated Description");
        result.Visibility.Should().Be(DocumentVisibility.Private);
        result.Tags.Should().BeEquivalentTo(new List<string> { "tag1", "tag2" });
    }

    /// <summary>
    /// When all fields are provided, all are updated accordingly.
    /// Validates: Requirements 7.1, 7.2
    /// </summary>
    [Fact]
    public async Task UpdateAllFields_UpdatesEverything()
    {
        var entity = CreateKnownEntity();
        var uuid = entity.Uuid;

        _documentRepository.GetByUuidAsync(uuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var request = new UpdateDocumentRequest(
            Name: "New Name",
            Description: "New Description",
            Visibility: DocumentVisibility.Public,
            Tags: ["newTag"]
        );

        var result = await _sut.UpdateDocumentAsync(uuid, request);

        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New Description");
        result.Visibility.Should().Be(DocumentVisibility.Public);
        result.Tags.Should().BeEquivalentTo(new List<string> { "newTag" });
    }

    /// <summary>
    /// When no fields are provided (all null), nothing changes except UpdatedAt.
    /// Validates: Requirements 7.1, 7.2
    /// </summary>
    [Fact]
    public async Task UpdateNoFields_PreservesAllExceptUpdatedAt()
    {
        var entity = CreateKnownEntity();
        var uuid = entity.Uuid;
        var originalUpdatedAt = entity.UpdatedAt;

        _documentRepository.GetByUuidAsync(uuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var request = new UpdateDocumentRequest(
            Name: null,
            Description: null,
            Visibility: null,
            Tags: null
        );

        var result = await _sut.UpdateDocumentAsync(uuid, request);

        result.Name.Should().Be("Original Name");
        result.Description.Should().Be("Original Description");
        result.Visibility.Should().Be(DocumentVisibility.Private);
        result.Tags.Should().BeEquivalentTo(new List<string> { "tag1", "tag2" });
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
