using CoreMs.TranslationMs.Client;
using FluentAssertions;
using Xunit;

namespace CoreMs.TranslationMs.IntegrationTests;

/// <summary>
/// Integration tests that exercise the translation-ms service through its own typed client.
/// Proves both the client and the controller round-trip work correctly together:
/// routing, serialization, content negotiation, error responses.
/// </summary>
public class TranslationClientTests : IClassFixture<TranslationMsTestFactory>
{
    private readonly TranslationMsClient _client;

    public TranslationClientTests(TranslationMsTestFactory factory)
    {
        var http = factory.CreateAnonymousClient();
        _client = new TranslationMsClient(http);
    }

    // ---- GetTranslationsAsync ----

    [Fact]
    public async Task GetTranslations_ExistingRealmAndLang_ReturnsDictionary()
    {
        var translations = await _client.GetTranslationsAsync("corems", "en");

        translations.Should().NotBeNull();
        translations.Should().ContainKey("app.name");
        translations!["app.name"].Should().Be("CoreMS");
    }

    [Fact]
    public async Task GetTranslations_NorwegianLocale_ReturnsDifferentContent()
    {
        var translations = await _client.GetTranslationsAsync("corems", "no");

        translations.Should().NotBeNull();
        translations.Should().ContainKey("common.save");
        translations!["common.save"].Should().Be("Lagre");
    }

    [Fact]
    public async Task GetTranslations_UnknownRealm_ThrowsServiceException()
    {
        var act = async () => await _client.GetTranslationsAsync("nonexistent", "en");

        await act.Should().ThrowAsync<CoreMs.Common.Exceptions.ServiceException>();
    }

    [Fact]
    public async Task GetTranslations_UnknownLanguage_ThrowsServiceException()
    {
        var act = async () => await _client.GetTranslationsAsync("corems", "zz");

        await act.Should().ThrowAsync<CoreMs.Common.Exceptions.ServiceException>();
    }

    // ---- GetLanguagesAsync ----

    [Fact]
    public async Task GetLanguages_ExistingRealm_ReturnsLanguageList()
    {
        var languages = await _client.GetLanguagesAsync("corems");

        languages.Should().NotBeNull();
        languages.Should().Contain("en");
        languages.Should().Contain("no");
    }

    [Fact]
    public async Task GetLanguages_UnknownRealm_ReturnsEmptyList()
    {
        var languages = await _client.GetLanguagesAsync("nonexistent");

        languages.Should().NotBeNull();
        languages.Should().BeEmpty();
    }

    // ---- Round-trip correctness ----

    [Fact]
    public async Task GetTranslations_AllKeysNonNull()
    {
        var translations = await _client.GetTranslationsAsync("corems", "en");

        translations.Should().NotBeNull();
        translations!.Values.Should().AllSatisfy(v => v.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetTranslations_EnglishAndNorwegian_HaveSameKeys()
    {
        var en = await _client.GetTranslationsAsync("corems", "en");
        var no = await _client.GetTranslationsAsync("corems", "no");

        en.Should().NotBeNull();
        no.Should().NotBeNull();
        en!.Keys.Should().BeEquivalentTo(no!.Keys);
    }
}
