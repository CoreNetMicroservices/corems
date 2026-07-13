using System.Net;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.TranslationMs.Tests.Properties;

/// <summary>
/// Property 5: Public endpoints accessible without authentication
/// Public endpoints respond successfully (200 or 404) without any authentication token.
/// **Validates: Requirements 3.3, 4.2**
/// </summary>
public class PublicEndpointsAccessiblePropertyTests : IClassFixture<TranslationTestFactory>
{
    private readonly HttpClient _client;

    public PublicEndpointsAccessiblePropertyTests(TranslationTestFactory factory)
    {
        _client = factory.CreateAnonymousClient();
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(PublicEndpointArbitraries)])]
    public void PublicTranslationEndpoint_NoAuth_Returns200Or404(RealmLangInput input)
    {
        var response = _client.GetAsync($"/api/translations/{input.Realm}/{input.Lang}").Result;

        // Public endpoints must never return 401 or 403
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        // Should be either 200 (data found) or 404 (not found)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(PublicEndpointArbitraries)])]
    public void PublicLanguagesEndpoint_NoAuth_Returns200(RealmLangInput input)
    {
        var response = _client.GetAsync($"/api/translations/{input.Realm}/languages").Result;

        // Public endpoints must never return 401 or 403
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        // Languages endpoint always returns 200 (empty list if no bundles)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>
/// Input record for public endpoint property tests.
/// </summary>
public record RealmLangInput(string Realm, string Lang)
{
    public override string ToString() => $"Realm={Realm}, Lang={Lang}";
}

/// <summary>
/// Generates valid URL-safe realm and lang values for public endpoint testing.
/// </summary>
public class PublicEndpointArbitraries
{
    private static readonly string[] Realms =
        ["auth", "dashboard", "common", "settings", "profile", "admin", "landing", "checkout"];

    private static readonly string[] Langs =
        ["en", "de", "fr", "es", "it", "pt", "nl", "ja", "zh", "ko", "ru", "ar"];

    public static Arbitrary<RealmLangInput> RealmLangInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<RealmLangInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var realm = Realms[rng.Next(Realms.Length)];
            var lang = Langs[rng.Next(Langs.Length)];
            return new RealmLangInput(realm, lang);
        });

        return FsCheck.Fluent.Arb.From(gen);
    }
}
