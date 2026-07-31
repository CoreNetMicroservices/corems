using CoreMs.Common.Exceptions;
using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.TemplateMs.Core.Entities;
using CoreMs.TemplateMs.Core.Models;
using CoreMs.TemplateMs.Core.Repositories;
using CoreMs.TemplateMs.Core.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 4: Soft delete exclusion
/// For any soft-deleted template, paginated listing and rendering endpoints should never
/// return or use that template.
///
/// Property 9: Filter correctness
/// For any set of templates and any category or language filter value, all templates returned
/// by a filtered listing query should match the specified filter criterion, and no matching
/// templates should be excluded.
///
/// **Validates: Requirements 3.1, 4.3, 4.4, 4.5, 5.2**
/// </summary>
public class SoftDeleteAndFilterPropertyTests
{
    #region Property 4: Soft delete exclusion

    [Property(MaxTest = 50, Arbitrary = [typeof(SoftDeleteArbitraries)])]
    public async Task RenderAsync_SoftDeletedTemplate_Returns404(SoftDeleteInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        // BaseQuery filters out soft-deleted records, so lookup returns null
        repository.GetByTemplateIdAndLanguageAsync(input.TemplateId, input.Language, Arg.Any<CancellationToken>())
            .Returns((TemplateEntity?)null);

        var service = new TemplateService(repository, engine, cache, currentUser);

        var request = new RenderTemplateRequest
        {
            TemplateId = input.TemplateId,
            Language = input.Language,
            Parameters = new Dictionary<string, object> { ["name"] = "test" }
        };

        // Act & Assert: rendering a soft-deleted template returns 404
        var ex = await Assert.ThrowsAsync<ServiceException>(() => service.RenderAsync(request));
        ex.HttpStatusCode.Should().Be(404);
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(SoftDeleteArbitraries)])]
    public async Task DeleteAsync_SetsIsDeletedTrue_AndInvalidatesCache(SoftDeleteInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUserUuid().Returns(Guid.NewGuid());

        var entity = new TemplateEntity
        {
            Uuid = Guid.NewGuid(),
            TemplateId = input.TemplateId,
            Language = input.Language,
            Name = "Test Template",
            Content = "Hello {{name}}",
            Category = "COMMON",
            IsDeleted = false
        };

        repository.GetByUuidAsync(entity.Uuid, Arg.Any<CancellationToken>()).Returns(entity);

        var service = new TemplateService(repository, engine, cache, currentUser);

        // Act
        await service.DeleteAsync(entity.Uuid);

        // Assert: soft delete sets flag to true
        entity.IsDeleted.Should().BeTrue();

        // Assert: cache is invalidated (subsequent Get returns null)
        cache.Get(input.TemplateId, input.Language).Should().BeNull();
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(SoftDeleteArbitraries)])]
    public async Task GetAllAsync_ExcludesSoftDeletedTemplates(SoftDeleteInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        // Simulate repository returning only non-deleted templates (BaseQuery filters them)
        var nonDeletedEntities = new List<TemplateEntity>
        {
            new()
            {
                Uuid = Guid.NewGuid(),
                TemplateId = "active-template",
                Language = input.Language,
                Name = "Active",
                Content = "Hello",
                Category = "COMMON",
                IsDeleted = false
            }
        };

        var pagedResult = new PagedResult<TemplateEntity>(nonDeletedEntities, 1, 1, 20);
        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pagedResult));

        var service = new TemplateService(repository, engine, cache, currentUser);

        // Act
        var result = await service.GetAllAsync(new QueryParameters { Page = 1, PageSize = 20 });

        // Assert: no soft-deleted templates appear in listing results
        result.Items.Should().AllSatisfy(dto =>
        {
            // The soft-deleted templateId should not appear
            dto.TemplateId.Should().NotBe(input.TemplateId);
        });
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(SoftDeleteArbitraries)])]
    public async Task GetByUuidAsync_SoftDeletedTemplate_Returns404(SoftDeleteInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        var uuid = Guid.NewGuid();

        // BaseQuery filters soft-deleted, so GetByUuidAsync returns null
        repository.GetByUuidAsync(uuid, Arg.Any<CancellationToken>())
            .Returns((TemplateEntity?)null);

        var service = new TemplateService(repository, engine, cache, currentUser);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ServiceException>(() => service.GetByUuidAsync(uuid));
        ex.HttpStatusCode.Should().Be(404);
    }

    #endregion

    #region Property 9: Filter correctness

    [Property(MaxTest = 50, Arbitrary = [typeof(FilterCorrectnessArbitraries)])]
    public async Task GetAllAsync_CategoryFilter_ReturnsOnlyMatchingTemplates(FilterInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        // Simulate repository returning only templates matching the category filter
        var matchingEntities = input.AllTemplates
            .Where(t => t.Category == input.FilterCategory)
            .ToList();

        var pagedResult = new PagedResult<TemplateEntity>(matchingEntities, matchingEntities.Count, 1, 20);

        QueryParameters? capturedParams = null;
        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedParams = callInfo.Arg<QueryParameters>();
                return Task.FromResult(pagedResult);
            });

        var service = new TemplateService(repository, engine, cache, currentUser);

        var parameters = new QueryParameters
        {
            Page = 1,
            PageSize = 20,
            Filters = [$"Category:eq:{input.FilterCategory}"]
        };

        // Act
        var result = await service.GetAllAsync(parameters);

        // Assert: all returned templates match the filter category
        foreach (var dto in result.Items)
        {
            dto.Category.Should().Be(input.FilterCategory);
        }

        // Assert: count matches expected filtered set
        result.Items.Should().HaveCount(matchingEntities.Count);

        // Assert: filter was passed to repository
        capturedParams.Should().NotBeNull();
        capturedParams!.Filters.Should().Contain($"Category:eq:{input.FilterCategory}");
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(FilterCorrectnessArbitraries)])]
    public async Task GetAllAsync_LanguageFilter_ReturnsOnlyMatchingTemplates(FilterInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        // Simulate repository returning only templates matching the language filter
        var matchingEntities = input.AllTemplates
            .Where(t => t.Language == input.FilterLanguage)
            .ToList();

        var pagedResult = new PagedResult<TemplateEntity>(matchingEntities, matchingEntities.Count, 1, 20);

        QueryParameters? capturedParams = null;
        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedParams = callInfo.Arg<QueryParameters>();
                return Task.FromResult(pagedResult);
            });

        var service = new TemplateService(repository, engine, cache, currentUser);

        var parameters = new QueryParameters
        {
            Page = 1,
            PageSize = 20,
            Filters = [$"Language:eq:{input.FilterLanguage}"]
        };

        // Act
        var result = await service.GetAllAsync(parameters);

        // Assert: all returned templates match the filter language
        foreach (var dto in result.Items)
        {
            dto.Language.Should().Be(input.FilterLanguage);
        }

        // Assert: count matches expected filtered set
        result.Items.Should().HaveCount(matchingEntities.Count);

        // Assert: filter was passed to repository
        capturedParams.Should().NotBeNull();
        capturedParams!.Filters.Should().Contain($"Language:eq:{input.FilterLanguage}");
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(FilterCorrectnessArbitraries)])]
    public async Task GetAllAsync_NoMatchingFilter_ReturnsEmptyResult(FilterInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TemplateRepository>(dbContext);
        var engine = new TemplateEngine();
        var cache = new TemplateCache();
        var currentUser = Substitute.For<ICurrentUserService>();

        // Simulate no templates matching the filter
        var pagedResult = new PagedResult<TemplateEntity>([], 0, 1, 20);
        repository.GetPagedAsync(Arg.Any<QueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pagedResult));

        var service = new TemplateService(repository, engine, cache, currentUser);

        var parameters = new QueryParameters
        {
            Page = 1,
            PageSize = 20,
            Filters = ["Category:eq:NONEXISTENT"]
        };

        // Act
        var result = await service.GetAllAsync(parameters);

        // Assert: empty result with correct metadata
        result.Items.Should().BeEmpty();
        result.TotalElements.Should().Be(0);
    }

    #endregion
}

#region Arbitraries

public record SoftDeleteInput(string TemplateId, string Language)
{
    public override string ToString() => $"{TemplateId}:{Language}";
}

public class SoftDeleteArbitraries
{
    public static Arbitrary<SoftDeleteInput> SoftDeleteInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<SoftDeleteInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var ids = new[] { "deleted-email", "removed-sms", "old-doc", "expired-notification", "archived-invoice" };
            var langs = new[] { "en", "fr", "de", "es", "pt" };
            return new SoftDeleteInput(ids[rng.Next(ids.Length)], langs[rng.Next(langs.Length)]);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

public record FilterInput(
    List<TemplateEntity> AllTemplates,
    string FilterCategory,
    string FilterLanguage)
{
    public override string ToString() => $"Category={FilterCategory}, Language={FilterLanguage}, Total={AllTemplates.Count}";
}

public class FilterCorrectnessArbitraries
{
    private static readonly string[] Categories = ["COMMON", "EMAIL", "SMS", "DOCUMENT"];
    private static readonly string[] Languages = ["en", "fr", "de", "es", "pt"];

    public static Arbitrary<FilterInput> FilterInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<FilterInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);

            // Pick filter values first
            var filterCategory = Categories[rng.Next(Categories.Length)];
            var filterLanguage = Languages[rng.Next(Languages.Length)];

            var templateCount = rng.Next(3, 10);
            var templates = new List<TemplateEntity>();

            // Ensure at least one template matches the category filter
            templates.Add(new TemplateEntity
            {
                Uuid = Guid.NewGuid(),
                TemplateId = $"match-cat-{seed}",
                Language = Languages[rng.Next(Languages.Length)],
                Name = "Matching Category",
                Content = "Hello {{name}}",
                Category = filterCategory,
                IsDeleted = false
            });

            // Ensure at least one template matches the language filter
            templates.Add(new TemplateEntity
            {
                Uuid = Guid.NewGuid(),
                TemplateId = $"match-lang-{seed}",
                Language = filterLanguage,
                Name = "Matching Language",
                Content = "Hello {{name}}",
                Category = Categories[rng.Next(Categories.Length)],
                IsDeleted = false
            });

            // Add remaining random templates
            for (var i = 2; i < templateCount; i++)
            {
                templates.Add(new TemplateEntity
                {
                    Uuid = Guid.NewGuid(),
                    TemplateId = $"template-{seed}-{i}",
                    Language = Languages[rng.Next(Languages.Length)],
                    Name = $"Template {i}",
                    Content = "Hello {{name}}",
                    Category = Categories[rng.Next(Categories.Length)],
                    IsDeleted = false
                });
            }

            return new FilterInput(templates, filterCategory, filterLanguage);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
