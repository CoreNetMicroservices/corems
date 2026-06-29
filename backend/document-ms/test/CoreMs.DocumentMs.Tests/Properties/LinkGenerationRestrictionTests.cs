using Amazon.S3;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Security;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
using CoreMs.DocumentMs.Core.Exceptions;
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
/// Property 11: Link generation restricted to BY_LINK visibility —
/// PUBLIC/PRIVATE documents reject link generation; BY_LINK documents allow it for owner/admin.
///
/// **Validates: Requirements 5.1, 5.2**
/// </summary>
public class LinkGenerationRestrictionTests
{
    private readonly DocumentRepository _documentRepository;
    private readonly DocumentAccessTokenRepository _documentAccessTokenRepository;
    private readonly IAmazonS3 _mockS3Client;
    private readonly S3StorageService _storageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DocumentService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public LinkGenerationRestrictionTests()
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
            LinkSigningKey = "test-key-minimum-32-characters-long!!",
            BaseUrl = "http://localhost:5102"
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
    /// BY_LINK document allows link generation — returns a valid DocumentLinkDto.
    /// Validates: Requirements 5.1
    /// </summary>
    [Fact]
    public async Task ByLinkDocument_GenerateAccessLink_Succeeds()
    {
        var docUuid = Guid.NewGuid();
        var entity = new DocumentEntity
        {
            Uuid = docUuid,
            UserId = _userId,
            Visibility = DocumentVisibility.ByLink,
            Name = "shared-file",
            OriginalFilename = "shared-file.pdf",
            ObjectKey = "some-key"
        };

        _documentRepository.GetByUuidAsync(docUuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var request = new GenerateLinkRequest(ExpiresInMinutes: 60);

        var result = await _sut.GenerateAccessLinkAsync(docUuid, request);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Url.Should().Contain(result.Token);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    /// <summary>
    /// PUBLIC document rejects link generation with LinkGenerationNotAllowed error.
    /// Validates: Requirements 5.2
    /// </summary>
    [Fact]
    public async Task PublicDocument_GenerateAccessLink_ThrowsLinkGenerationNotAllowed()
    {
        var docUuid = Guid.NewGuid();
        var entity = new DocumentEntity
        {
            Uuid = docUuid,
            UserId = _userId,
            Visibility = DocumentVisibility.Public,
            Name = "public-file",
            OriginalFilename = "public-file.pdf",
            ObjectKey = "some-key"
        };

        _documentRepository.GetByUuidAsync(docUuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var request = new GenerateLinkRequest(ExpiresInMinutes: 60);

        var act = () => _sut.GenerateAccessLinkAsync(docUuid, request);

        var ex = await act.Should().ThrowAsync<ServiceException>();
        ex.Which.Errors[0].ReasonCode.Should().Be(DocumentServiceErrors.LinkGenerationNotAllowed.ErrorCode);
        ex.Which.HttpStatusCode.Should().Be(400);
    }

    /// <summary>
    /// PRIVATE document rejects link generation with LinkGenerationNotAllowed error.
    /// Validates: Requirements 5.2
    /// </summary>
    [Fact]
    public async Task PrivateDocument_GenerateAccessLink_ThrowsLinkGenerationNotAllowed()
    {
        var docUuid = Guid.NewGuid();
        var entity = new DocumentEntity
        {
            Uuid = docUuid,
            UserId = _userId,
            Visibility = DocumentVisibility.Private,
            Name = "private-file",
            OriginalFilename = "private-file.pdf",
            ObjectKey = "some-key"
        };

        _documentRepository.GetByUuidAsync(docUuid, Arg.Any<CancellationToken>())
            .Returns(entity);

        var request = new GenerateLinkRequest(ExpiresInMinutes: 60);

        var act = () => _sut.GenerateAccessLinkAsync(docUuid, request);

        var ex = await act.Should().ThrowAsync<ServiceException>();
        ex.Which.Errors[0].ReasonCode.Should().Be(DocumentServiceErrors.LinkGenerationNotAllowed.ErrorCode);
        ex.Which.HttpStatusCode.Should().Be(400);
    }
}
