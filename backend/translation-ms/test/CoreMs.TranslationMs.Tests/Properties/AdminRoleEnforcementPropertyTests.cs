using System.Net;
using System.Net.Http.Json;
using CoreMs.Common.Testing;
using CoreMs.TranslationMs.Core.Models;
using CoreMs.TranslationMs.Infrastructure.Data;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.TranslationMs.Tests.Properties;

/// <summary>
/// Property 4: Admin role enforcement
/// Unauthenticated requests to admin endpoints return 401;
/// authenticated without TRANSLATION_MS_ADMIN return 403.
/// **Validates: Requirements 5.3, 6.3, 7.7, 8.6, 9.4, 11.3, 11.4**
/// </summary>
public class AdminRoleEnforcementPropertyTests : IClassFixture<TranslationTestFactory>
{
    private readonly TranslationTestFactory _factory;

    public AdminRoleEnforcementPropertyTests(TranslationTestFactory factory)
    {
        _factory = factory;
    }

    private static readonly (string Method, string Path)[] AdminEndpoints =
    [
        ("GET", "/api/admin/translations/test-realm/en"),
        ("GET", "/api/admin/translations/realms"),
        ("POST", "/api/admin/translations/test-realm/en"),
        ("PUT", "/api/admin/translations/test-realm/en"),
        ("DELETE", "/api/admin/translations/test-realm/en"),
    ];

    [Fact]
    public async Task Unauthenticated_AllAdminEndpoints_Return401()
    {
        var client = _factory.CreateAnonymousClient();

        foreach (var (method, path) in AdminEndpoints)
        {
            var response = await SendRequest(client, method, path);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"{method} {path} should require authentication");
        }
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(NonAdminRoleArbitraries)])]
    public async Task AuthenticatedWithoutAdminRole_AllAdminEndpoints_Return403(NonAdminRole role)
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), role.Value);

        foreach (var (method, path) in AdminEndpoints)
        {
            var response = await SendRequest(client, method, path);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"{method} {path} with role '{role.Value}' should return 403");
        }
    }

    private static async Task<HttpResponseMessage> SendRequest(HttpClient client, string method, string path)
    {
        var body = new TranslationRequest { Data = new Dictionary<string, string> { ["k"] = "v" } };

        return method switch
        {
            "GET" => await client.GetAsync(path),
            "POST" => await client.PostAsJsonAsync(path, body),
            "PUT" => await client.PutAsJsonAsync(path, body),
            "DELETE" => await client.DeleteAsync(path),
            _ => throw new ArgumentException($"Unknown method: {method}")
        };
    }
}

/// <summary>
/// Wrapper for non-admin role strings used in FsCheck property tests.
/// </summary>
public record NonAdminRole(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Generates role strings that are NOT TRANSLATION_MS_ADMIN.
/// </summary>
public class NonAdminRoleArbitraries
{
    private static readonly string[] NonAdminRoles =
    [
        "USER_MS_USER",
        "USER_MS_ADMIN",
        "DOCUMENT_MS_ADMIN",
        "DOCUMENT_MS_USER",
        "SUPER_ADMIN_FAKE",
        "VIEWER",
        "EDITOR",
        "RANDOM_ROLE"
    ];

    public static Arbitrary<NonAdminRole> NonAdminRoleArbitrary()
    {
        Gen<int> indexGen = FsCheck.Fluent.Gen.Choose(0, NonAdminRoles.Length - 1);
        Gen<NonAdminRole> gen = FsCheck.Fluent.Gen.Select(indexGen, i => new NonAdminRole(NonAdminRoles[i]));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

/// <summary>
/// WebApplicationFactory for translation-ms integration tests.
/// Uses SQLite database and a shared test authentication handler from CoreMs.Common.Testing.
/// </summary>
public class TranslationTestFactory : CoreMsTestFactory<Program, TranslationMsDbContext> { }
