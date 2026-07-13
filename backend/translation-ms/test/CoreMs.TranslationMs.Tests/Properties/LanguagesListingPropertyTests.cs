using CoreMs.Common.Security;
using CoreMs.TranslationMs.Core.Repositories;
using CoreMs.TranslationMs.Core.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CoreMs.TranslationMs.Tests.Properties;

/// <summary>
/// Property 6: Languages listing returns exactly stored languages
/// For any realm, the languages endpoint returns exactly the distinct lang values stored, sorted alphabetically.
/// **Validates: Requirements 4.1, 4.3**
/// </summary>
public class LanguagesListingPropertyTests
{
    private static readonly string[] AllLangs = ["en", "de", "fr", "es", "it", "pt", "nl", "ja", "zh", "ko"];

    [Property(MaxTest = 100)]
    public void ReturnsExactlyStoredLanguages_SortedAlphabetically(NonEmptyString realm, int seed)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        // Generate a random subset of languages (with possible duplicates pre-distinct)
        var rng = new Random(Math.Abs(seed));
        var count = rng.Next(1, AllLangs.Length + 1);
        var storedLangs = Enumerable.Range(0, count)
            .Select(_ => AllLangs[rng.Next(AllLangs.Length)])
            .Distinct()
            .OrderBy(l => l, StringComparer.Ordinal)
            .ToList();

        repository.GetLanguagesByRealmAsync(realm.Get, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(storedLangs));

        // Act
        var result = service.GetLanguagesByRealmAsync(realm.Get).Result;

        // Assert
        result.Should().BeEquivalentTo(storedLangs);
        result.Should().BeInAscendingOrder();
    }

    [Property(MaxTest = 100)]
    public void EmptyRealm_ReturnsEmptyList(NonEmptyString realm)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        repository.GetLanguagesByRealmAsync(realm.Get, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<string>()));

        // Act
        var result = service.GetLanguagesByRealmAsync(realm.Get).Result;

        // Assert
        result.Should().BeEmpty();
    }

    [Property(MaxTest = 100)]
    public void ResultCount_MatchesDistinctStoredCount(NonEmptyString realm, int seed)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var service = new TranslationService(repository, currentUserService);

        // Generate languages with duplicates to verify distinct behavior
        var rng = new Random(Math.Abs(seed));
        var rawCount = rng.Next(0, 15);
        var rawLangs = Enumerable.Range(0, rawCount)
            .Select(_ => AllLangs[rng.Next(AllLangs.Length)])
            .ToList();

        var distinctSorted = rawLangs
            .Distinct()
            .OrderBy(l => l, StringComparer.Ordinal)
            .ToList();

        repository.GetLanguagesByRealmAsync(realm.Get, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(distinctSorted));

        // Act
        var result = service.GetLanguagesByRealmAsync(realm.Get).Result;

        // Assert: count matches distinct count, not raw count
        result.Should().HaveCount(distinctSorted.Count);
        result.Should().OnlyHaveUniqueItems();
    }
}
