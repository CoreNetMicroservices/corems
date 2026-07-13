using Amazon.S3;
using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
using CoreMs.DocumentMs.Core.Repositories;
using CoreMs.DocumentMs.Core.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// Property 7: Pagination completeness — Paginating through all pages returns every matching
/// document exactly once with no duplicates and no omissions.
///
/// Since testing real pagination requires a database, we test the contract: ListDocumentsAsync
/// correctly passes parameters to the repository and maps results to DTOs preserving pagination metadata.
///
/// **Validates: Requirements 10.1, 10.6**
/// </summary>
public class PaginationCompletenessTests
{
    private readonly DocumentRepository _documentRepository;
    private readonly DocumentAccessTokenRepository _documentAccessTokenRepository;
    private readonly IAmazonS3 _mockS3Client;
    private readonly S3StorageService _storageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DocumentService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public PaginationCompletenessTests()
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

    private static DocumentEntity CreateEntity(int index) => new()
    {
        Id = index,
        Uuid = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = $"Document {index}",
        OriginalFilename = $"file{index}.pdf",
        Size = 1024 * (index + 1),
        Extension = "pdf",
        ContentType = "application/pdf",
        Bucket = "test-bucket",
        ObjectKey = $"user/{Guid.NewGuid():N}/file{index}.pdf",
        Visibility = DocumentVisibility.Private,
        UploadedById = Guid.NewGuid(),
        UploadedByType = UploadedByType.User,
        Checksum = $"checksum{index}",
        Description = $"Description {index}",
        Tags = [$"tag{index}"],
        Version = 1,
        CreatedAt = DateTime.UtcNow.AddMinutes(-index),
        UpdatedAt = DateTime.UtcNow.AddMinutes(-index)
    };

    /// <summary>
    /// For any valid page/pageSize and total count, ListDocumentsAsync passes parameters
    /// to GetPagedAsync and returns a PagedResult with correct metadata preserved.
    /// Validates: Requirements 10.1, 10.6
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(PaginationArbitraries)])]
    public void ListDocuments_PreservesPaginationMetadata(PaginationInput input)
    {
        var itemCount = Math.Min(input.PageSize, Math.Max(0, input.TotalElements - (input.Page - 1) * input.PageSize));
        var entities = Enumerable.Range(0, itemCount).Select(CreateEntity).ToList();

        var repoResult = new PagedResult<DocumentEntity>(entities, input.TotalElements, input.Page, input.PageSize);

        _documentRepository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(repoResult));

        var parameters = new QueryParameters { Page = input.Page, PageSize = input.PageSize };
        var result = _sut.ListDocumentsAsync(parameters).GetAwaiter().GetResult();

        Assert.Equal(input.Page, result.Page);
        Assert.Equal(input.PageSize, result.PageSize);
        Assert.Equal(input.TotalElements, result.TotalElements);
        Assert.Equal(itemCount, result.Items.Count);
    }

    /// <summary>
    /// For any set of entities returned by the repository, every entity is mapped to a DTO
    /// exactly once (no duplicates, no omissions).
    /// Validates: Requirements 10.1, 10.6
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(PaginationArbitraries)])]
    public void ListDocuments_MapsAllEntitiesToDtos_ExactlyOnce(ItemCount count)
    {
        var entities = Enumerable.Range(0, count.Value).Select(CreateEntity).ToList();
        var repoResult = new PagedResult<DocumentEntity>(entities, count.Value, 1, 20);

        _documentRepository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(repoResult));

        var parameters = new QueryParameters { Page = 1, PageSize = 20 };
        var result = _sut.ListDocumentsAsync(parameters).GetAwaiter().GetResult();

        // No duplicates
        var uniqueUuids = result.Items.Select(d => d.Uuid).Distinct().Count();
        Assert.Equal(result.Items.Count, uniqueUuids);

        // Every entity is represented
        Assert.All(entities, e => Assert.Contains(result.Items, d => d.Uuid == e.Uuid));

        // No extra DTOs beyond entities
        Assert.All(result.Items, d => Assert.Contains(entities, e => e.Uuid == d.Uuid));
    }

    /// <summary>
    /// DTO field values match entity field values after mapping.
    /// Validates: Requirements 10.1
    /// </summary>
    [Fact]
    public async Task ListDocuments_MappedDtos_HaveCorrectFieldValues()
    {
        var entity = CreateEntity(0);
        var repoResult = new PagedResult<DocumentEntity>([entity], 1, 1, 20);

        _documentRepository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(repoResult);

        var parameters = new QueryParameters { Page = 1, PageSize = 20 };
        var result = await _sut.ListDocumentsAsync(parameters);

        result.Items.Should().HaveCount(1);
        var dto = result.Items[0];

        dto.Uuid.Should().Be(entity.Uuid);
        dto.UserId.Should().Be(entity.UserId);
        dto.Name.Should().Be(entity.Name);
        dto.OriginalFilename.Should().Be(entity.OriginalFilename);
        dto.Size.Should().Be(entity.Size);
        dto.Extension.Should().Be(entity.Extension);
        dto.ContentType.Should().Be(entity.ContentType);
        dto.Visibility.Should().Be(entity.Visibility);
        dto.UploadedById.Should().Be(entity.UploadedById);
        dto.UploadedByType.Should().Be(entity.UploadedByType);
        dto.Checksum.Should().Be(entity.Checksum);
        dto.Description.Should().Be(entity.Description);
        dto.Tags.Should().BeEquivalentTo(entity.Tags);
        dto.Version.Should().Be(entity.Version);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    /// <summary>
    /// QueryParameters are passed through to repository without modification.
    /// Validates: Requirements 10.6
    /// </summary>
    [Fact]
    public async Task ListDocuments_DelegatesToRepository_WithExactParameters()
    {
        var repoResult = new PagedResult<DocumentEntity>([], 0, 3, 15);

        _documentRepository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(repoResult);

        var parameters = new QueryParameters
        {
            Page = 3,
            PageSize = 15,
            Search = "invoice",
            Sort = "Name:asc",
            Filters = ["Visibility:eq:Public"]
        };

        await _sut.ListDocumentsAsync(parameters);

        await _documentRepository.Received(1).GetPagedAsync(
            Arg.Is<QueryParameters>(p =>
                p.Page == 3 &&
                p.PageSize == 15 &&
                p.Search == "invoice" &&
                p.Sort == "Name:asc" &&
                p.Filters != null && p.Filters.Contains("Visibility:eq:Public")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Empty result from repository produces empty PagedResult with correct metadata.
    /// Validates: Requirements 10.1
    /// </summary>
    [Fact]
    public async Task ListDocuments_EmptyResult_ReturnsEmptyPagedResult()
    {
        var repoResult = new PagedResult<DocumentEntity>([], 0, 1, 20);

        _documentRepository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(repoResult);

        var parameters = new QueryParameters { Page = 1, PageSize = 20 };
        var result = await _sut.ListDocumentsAsync(parameters);

        result.Items.Should().BeEmpty();
        result.TotalElements.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }
}

#region Custom Types and Arbitraries for FsCheck 3.x

public record PaginationInput(int Page, int PageSize, int TotalCount);
public record ItemCount(int Value);

public class PaginationArbitraries
{
    public static Arbitrary<PaginationInput> PaginationInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<PaginationInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var page = rng.Next(1, 11);
            var pageSize = rng.Next(1, 21);
            var totalCount = rng.Next(0, 51);
            return new PaginationInput(page, pageSize, totalCount);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<ItemCount> ItemCountArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(1, 20);
        Gen<ItemCount> gen = FsCheck.Fluent.Gen.Select(seedGen,
            n => new ItemCount(n));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
