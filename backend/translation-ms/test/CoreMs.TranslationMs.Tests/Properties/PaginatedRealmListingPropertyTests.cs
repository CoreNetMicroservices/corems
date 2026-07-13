using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.TranslationMs.Core.Entities;
using CoreMs.TranslationMs.Core.Repositories;
using CoreMs.TranslationMs.Core.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CoreMs.TranslationMs.Tests.Properties;

/// <summary>
/// Property 11: Paginated realm listing
/// Results respect page/size parameters with total count metadata,
/// each realm contains complete language list.
/// **Validates: Requirements 6.1, 6.2**
/// </summary>
public class PaginatedRealmListingPropertyTests
{
    private static readonly string[] Realms = ["auth", "dashboard", "common", "settings", "profile"];
    private static readonly string[] Langs = ["en", "de", "fr", "es", "it", "pt", "nl", "ja"];

    [Property(MaxTest = 100)]
    public void ListRealms_PreservesPageAndSizeMetadata(PositiveInt page, PositiveInt size)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        var pageValue = Math.Max(1, page.Get % 10);
        var sizeValue = Math.Max(1, Math.Min(size.Get % 50, 50));

        var bundles = new List<TranslationBundleEntity>
        {
            new() { Id = 1, Realm = "auth", Lang = "en", Data = new() { ["k1"] = "v1" }, UpdatedAt = DateTime.UtcNow, UpdatedBy = Guid.NewGuid() },
            new() { Id = 2, Realm = "auth", Lang = "de", Data = new() { ["k1"] = "v1" }, UpdatedAt = DateTime.UtcNow, UpdatedBy = Guid.NewGuid() },
            new() { Id = 3, Realm = "dashboard", Lang = "en", Data = new() { ["k1"] = "v1" }, UpdatedAt = DateTime.UtcNow, UpdatedBy = Guid.NewGuid() },
        };

        var totalCount = 15;
        var pagedResult = new PagedResult<TranslationBundleEntity>(bundles, totalCount, pageValue, sizeValue);

        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pagedResult));

        var parameters = new QueryParameters { Page = pageValue, PageSize = sizeValue };

        // Act
        var result = service.ListRealmsAsync(parameters).Result;

        // Assert: pagination metadata is passed through correctly
        result.TotalElements.Should().Be(totalCount);
        result.Page.Should().Be(pageValue);
        result.PageSize.Should().Be(sizeValue);
    }

    [Property(MaxTest = 100)]
    public void ListRealms_EachRealmContainsAllItsLanguages(int seed)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        var rng = new Random(Math.Abs(seed));

        // Generate random bundles across multiple realms
        var bundles = new List<TranslationBundleEntity>();
        var expectedRealmLangs = new Dictionary<string, HashSet<string>>();
        var id = 1L;

        var realmCount = rng.Next(1, Realms.Length + 1);
        for (var r = 0; r < realmCount; r++)
        {
            var realm = Realms[r];
            var langCount = rng.Next(1, 5);
            var selectedLangs = Langs.OrderBy(_ => rng.Next()).Take(langCount).ToList();

            expectedRealmLangs[realm] = new HashSet<string>(selectedLangs);

            foreach (var lang in selectedLangs)
            {
                bundles.Add(new TranslationBundleEntity
                {
                    Id = id++,
                    Realm = realm,
                    Lang = lang,
                    Data = new Dictionary<string, string> { [$"key_{lang}"] = $"value_{lang}" },
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = Guid.NewGuid()
                });
            }
        }

        var pagedResult = new PagedResult<TranslationBundleEntity>(bundles, bundles.Count, 1, 20);

        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pagedResult));

        // Act
        var result = service.ListRealmsAsync(new QueryParameters { Page = 1, PageSize = 20 }).Result;

        // Assert: each realm entry contains exactly its languages
        foreach (var (realm, expectedLangs) in expectedRealmLangs)
        {
            var realmEntry = result.Items.FirstOrDefault(r => r.Realm == realm);
            realmEntry.Should().NotBeNull($"realm '{realm}' should be in results");
            realmEntry!.Languages.Select(l => l.Lang).Should().BeEquivalentTo(expectedLangs);
        }

        // Assert: no extra realms in result
        result.Items.Select(r => r.Realm).Should().BeEquivalentTo(expectedRealmLangs.Keys);
    }

    [Property(MaxTest = 100)]
    public void ListRealms_LanguagesAreSortedAlphabetically(int seed)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        var rng = new Random(Math.Abs(seed));

        var bundles = new List<TranslationBundleEntity>();
        var id = 1L;
        var langCount = rng.Next(2, Langs.Length + 1);
        var selectedLangs = Langs.OrderBy(_ => rng.Next()).Take(langCount).ToList();

        foreach (var lang in selectedLangs)
        {
            bundles.Add(new TranslationBundleEntity
            {
                Id = id++,
                Realm = "test_realm",
                Lang = lang,
                Data = new Dictionary<string, string> { ["k"] = "v" },
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid()
            });
        }

        var pagedResult = new PagedResult<TranslationBundleEntity>(bundles, bundles.Count, 1, 20);

        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pagedResult));

        // Act
        var result = service.ListRealmsAsync(new QueryParameters { Page = 1, PageSize = 20 }).Result;

        // Assert: languages within each realm are sorted alphabetically
        var realmEntry = result.Items.First(r => r.Realm == "test_realm");
        var langs = realmEntry.Languages.Select(l => l.Lang).ToList();
        langs.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ListRealms_EmptyResult_ReturnsEmptyItemsWithMetadata()
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        var emptyResult = new PagedResult<TranslationBundleEntity>(
            new List<TranslationBundleEntity>(), 0, 1, 20);

        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(emptyResult));

        // Act
        var result = await service.ListRealmsAsync(new QueryParameters { Page = 1, PageSize = 20 });

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalElements.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Property(MaxTest = 50)]
    public void ListRealms_PassesQueryParametersToRepository(PositiveInt page, PositiveInt size)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        var pageValue = Math.Max(1, page.Get % 10);
        var sizeValue = Math.Max(1, Math.Min(size.Get % 50, 50));

        var emptyResult = new PagedResult<TranslationBundleEntity>(
            new List<TranslationBundleEntity>(), 0, pageValue, sizeValue);

        QueryParameters? capturedParams = null;
        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedParams = callInfo.Arg<QueryParameters>();
                return Task.FromResult(emptyResult);
            });

        var parameters = new QueryParameters { Page = pageValue, PageSize = sizeValue };

        // Act
        _ = service.ListRealmsAsync(parameters).Result;

        // Assert: parameters are passed through to the repository
        capturedParams.Should().NotBeNull();
        capturedParams!.Page.Should().Be(pageValue);
        capturedParams.PageSize.Should().Be(sizeValue);
    }
}
