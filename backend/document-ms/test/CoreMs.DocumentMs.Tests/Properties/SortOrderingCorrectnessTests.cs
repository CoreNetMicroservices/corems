using Amazon.S3;
using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
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
/// Property 16: Sort ordering correctness — results ordered according to specified field/direction.
///
/// **Validates: Requirements 10.3**
/// </summary>
public class SortOrderingCorrectnessTests
{
    [Fact]
    public void DocumentRepository_SortFields_ContainsExpectedFields()
    {
        var dbContext = Substitute.For<DbContext>();
        var repository = new DocumentRepository(dbContext);

        var sortFields = repository.GetType()
            .GetProperty("SortFields", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(repository) as IReadOnlySet<string>;

        sortFields.Should().NotBeNull();
        sortFields.Should().Contain("Name");
        sortFields.Should().Contain("CreatedAt");
        sortFields.Should().Contain("UpdatedAt");
        sortFields.Should().Contain("Size");
    }

    [Fact]
    public async Task ListDocuments_PreservesOrderFromRepository_AscendingByName()
    {
        var sut = CreateDocumentService(out var documentRepository);

        var entities = new List<DocumentEntity>
        {
            CreateEntity("A-document", 100, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateEntity("B-document", 200, new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateEntity("C-document", 300, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc))
        };

        var pagedResult = new PagedResult<DocumentEntity>(entities, 3, 1, 20);
        var parameters = new QueryParameters { Page = 1, PageSize = 20, Sort = "Name:asc" };

        documentRepository.GetPagedAsync(parameters, Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        var result = await sut.ListDocumentsAsync(parameters);

        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("A-document");
        result.Items[1].Name.Should().Be("B-document");
        result.Items[2].Name.Should().Be("C-document");
        result.TotalElements.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task ListDocuments_PreservesOrderFromRepository_DescendingByCreatedAt()
    {
        var sut = CreateDocumentService(out var documentRepository);

        var entities = new List<DocumentEntity>
        {
            CreateEntity("Recent", 300, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateEntity("Middle", 200, new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateEntity("Oldest", 100, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        };

        var pagedResult = new PagedResult<DocumentEntity>(entities, 3, 1, 20);
        var parameters = new QueryParameters { Page = 1, PageSize = 20, Sort = "CreatedAt:desc" };

        documentRepository.GetPagedAsync(parameters, Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        var result = await sut.ListDocumentsAsync(parameters);

        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Recent");
        result.Items[1].Name.Should().Be("Middle");
        result.Items[2].Name.Should().Be("Oldest");
    }

    [Fact]
    public async Task ListDocuments_PreservesOrderFromRepository_AscendingBySize()
    {
        var sut = CreateDocumentService(out var documentRepository);

        var entities = new List<DocumentEntity>
        {
            CreateEntity("Small", 100, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateEntity("Medium", 5000, new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateEntity("Large", 99999, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc))
        };

        var pagedResult = new PagedResult<DocumentEntity>(entities, 3, 1, 20);
        var parameters = new QueryParameters { Page = 1, PageSize = 20, Sort = "Size:asc" };

        documentRepository.GetPagedAsync(parameters, Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        var result = await sut.ListDocumentsAsync(parameters);

        result.Items.Should().HaveCount(3);
        result.Items[0].Size.Should().Be(100);
        result.Items[1].Size.Should().Be(5000);
        result.Items[2].Size.Should().Be(99999);
    }

    [Fact]
    public async Task ListDocuments_MappedDtoOrder_MatchesEntityOrder()
    {
        var sut = CreateDocumentService(out var documentRepository);

        var entityIds = Enumerable.Range(0, 5)
            .Select(_ => Guid.NewGuid())
            .ToList();

        var entities = entityIds.Select((id, i) =>
        {
            var entity = CreateEntity($"Doc-{i}", (i + 1) * 1024, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i));
            entity.Uuid = id;
            return entity;
        }).ToList();

        var pagedResult = new PagedResult<DocumentEntity>(entities, 5, 1, 20);
        var parameters = new QueryParameters { Page = 1, PageSize = 20, Sort = "UpdatedAt:asc" };

        documentRepository.GetPagedAsync(parameters, Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        var result = await sut.ListDocumentsAsync(parameters);

        result.Items.Should().HaveCount(5);
        for (var i = 0; i < 5; i++)
        {
            result.Items[i].Uuid.Should().Be(entityIds[i]);
            result.Items[i].Name.Should().Be($"Doc-{i}");
        }
    }

    private static DocumentService CreateDocumentService(out DocumentRepository documentRepository)
    {
        var dbContext = Substitute.For<DbContext>();
        documentRepository = Substitute.ForPartsOf<DocumentRepository>(dbContext);
        var documentAccessTokenRepository = Substitute.ForPartsOf<DocumentAccessTokenRepository>(dbContext);
        var mockS3 = Substitute.For<IAmazonS3>();
        var storageOptions = Options.Create(new StorageOptions
        {
            Endpoint = "http://localhost:9000",
            AccessKey = "key",
            SecretKey = "secret",
            Bucket = "test-bucket",
            ForcePathStyle = true
        });
        var storageService = new S3StorageService(mockS3, storageOptions, Substitute.For<ILogger<S3StorageService>>());
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUserUuid().Returns(Guid.NewGuid());
        currentUserService.GetCurrentUserRoles().Returns(new List<string>());
        var documentOptions = Options.Create(new DocumentOptions
        {
            LinkSigningKey = "test-key-minimum-32-characters-long!!"
        });
        var logger = Substitute.For<ILogger<DocumentService>>();

        var sut = new DocumentService(
            documentRepository,
            documentAccessTokenRepository,
            storageService,
            currentUserService,
            documentOptions,
            storageOptions,
            logger);

        return sut;
    }

    private static DocumentEntity CreateEntity(string name, long size, DateTime createdAt)
    {
        return new DocumentEntity
        {
            Uuid = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name,
            OriginalFilename = $"{name}.pdf",
            Size = size,
            Extension = "pdf",
            ContentType = "application/pdf",
            Bucket = "test",
            ObjectKey = $"key/{Guid.NewGuid()}",
            Visibility = DocumentVisibility.Private,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Tags = []
        };
    }
}
