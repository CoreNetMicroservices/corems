using CoreMs.Common.Security;
using CoreMs.TranslationMs.Core.Entities;
using CoreMs.TranslationMs.Core.Models;
using CoreMs.TranslationMs.Core.Repositories;
using CoreMs.TranslationMs.Core.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CoreMs.TranslationMs.Tests.Properties;

/// <summary>
/// Property 1: Translation bundle data round-trip
/// For any valid realm (non-empty string), lang (non-empty string), and data dictionary
/// (non-empty Dictionary&lt;string, string&gt;), creating a bundle and retrieving it returns
/// the exact same key-value data.
///
/// **Validates: Requirements 2.1, 2.4, 3.1, 7.1, 7.2**
/// </summary>
public class TranslationBundleRoundTripPropertyTests
{
    [Property(MaxTest = 100, Arbitrary = [typeof(TranslationArbitraries)])]
    public async Task CreateAndRetrieve_ReturnsExactSameData(TranslationInput input)
    {
        // Arrange
        var dbContext = Substitute.For<DbContext>();
        var repository = Substitute.ForPartsOf<TranslationBundleRepository>(dbContext);
        var currentUserService = Substitute.For<ICurrentUserService>();
        var userId = Guid.NewGuid();
        currentUserService.GetCurrentUserUuid().Returns(userId);

        var service = new TranslationService(repository, currentUserService);

        var realm = input.Realm;
        var lang = input.Lang;
        var data = input.Data;

        // Setup: no existing bundle for this realm+lang
        repository.ExistsByRealmAndLangAsync(realm, lang, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Capture the entity passed to Add
        TranslationBundleEntity? capturedEntity = null;
        repository.When(r => r.Add(Arg.Any<TranslationBundleEntity>()))
            .Do(ci => capturedEntity = ci.Arg<TranslationBundleEntity>());

        // Act: Create the bundle
        var request = new TranslationRequest { Data = data };
        var createResult = await service.CreateTranslationAsync(realm, lang, request);

        // Assert: entity passed to repository.Add has exact same data, realm, and lang
        capturedEntity.Should().NotBeNull();
        capturedEntity!.Data.Should().BeEquivalentTo(data);
        capturedEntity.Realm.Should().Be(realm);
        capturedEntity.Lang.Should().Be(lang);

        // Assert: the DTO returned from create has correct data
        createResult.Data.Should().BeEquivalentTo(data);
        createResult.Realm.Should().Be(realm);
        createResult.Lang.Should().Be(lang);

        // Setup: repository returns the captured entity for retrieval
        repository.GetByRealmAndLangAsync(realm, lang, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TranslationBundleEntity?>(capturedEntity));

        // Act: Retrieve the translations via the public method
        var retrievedData = await service.GetTranslationsAsync(realm, lang);

        // Assert: retrieved data matches original input exactly
        retrievedData.Should().BeEquivalentTo(data);
    }
}

/// <summary>
/// Wrapper record for translation round-trip test input.
/// </summary>
public record TranslationInput(string Realm, string Lang, Dictionary<string, string> Data)
{
    public override string ToString() => $"Realm={Realm}, Lang={Lang}, DataKeys=[{string.Join(",", Data.Keys.Take(3))}...]";
}

/// <summary>
/// Custom FsCheck 3.x arbitraries for translation property tests.
/// Generates valid non-empty realms, langs, and data dictionaries.
/// </summary>
public class TranslationArbitraries
{
    private static readonly string[] RealmPrefixes = ["auth", "dashboard", "common", "settings", "profile", "admin"];
    private static readonly string[] LangCodes = ["en", "de", "fr", "es", "it", "pt", "nl", "ja", "zh", "ko"];
    private static readonly string[] KeyPrefixes = ["btn", "lbl", "msg", "err", "title", "desc", "hint", "placeholder"];
    private static readonly string[] ValueWords = ["Hello", "World", "Save", "Cancel", "Delete", "Edit", "Submit", "OK", "Error", "Success"];

    public static Arbitrary<TranslationInput> TranslationInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<TranslationInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);

            var realm = RealmPrefixes[rng.Next(RealmPrefixes.Length)] + "_" + rng.Next(1000);
            var lang = LangCodes[rng.Next(LangCodes.Length)];

            var entryCount = rng.Next(1, 20);
            var data = new Dictionary<string, string>();
            for (var i = 0; i < entryCount; i++)
            {
                var key = KeyPrefixes[rng.Next(KeyPrefixes.Length)] + "." + rng.Next(100);
                var value = ValueWords[rng.Next(ValueWords.Length)] + " " + rng.Next(1000);
                data.TryAdd(key, value);
            }

            // Ensure at least one entry
            if (data.Count == 0)
                data["fallback.key"] = "fallback value";

            return new TranslationInput(realm, lang, data);
        });

        return FsCheck.Fluent.Arb.From(gen);
    }
}
