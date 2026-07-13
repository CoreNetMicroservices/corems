using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using CoreMs.TranslationMs.Core.Entities;
using CoreMs.TranslationMs.Core.Models;
using CoreMs.TranslationMs.Infrastructure.Data;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
/// Uses SQLite database and a test authentication handler.
/// </summary>
public class TranslationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection;

    public TranslationTestFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core / Npgsql registrations
            var efRelated = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<TranslationMsDbContext>)
                    || d.ServiceType == typeof(TranslationMsDbContext)
                    || d.ServiceType == typeof(CoreMs.Common.Data.CoreMsDbContext)
                    || d.ServiceType == typeof(DbContext)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                    || d.ServiceType.FullName?.Contains("EntityFramework") == true
                    || d.ServiceType.FullName?.Contains("Npgsql") == true
                    || d.ImplementationType?.FullName?.Contains("Npgsql") == true
                    || d.ImplementationType?.FullName?.Contains("EntityFramework") == true)
                .ToList();
            foreach (var descriptor in efRelated)
                services.Remove(descriptor);

            services.RemoveAll<DbContextOptions>();

            // Remove health checks that depend on infrastructure
            var healthCheckDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();
            foreach (var descriptor in healthCheckDescriptors)
                services.Remove(descriptor);
            services.AddHealthChecks();

            // Register SQLite DbContext
            services.AddDbContext<TranslationMsDbContext>((_, options) =>
            {
                options.UseSqlite(_connection);
            });
            services.AddScoped<CoreMs.Common.Data.CoreMsDbContext>(sp =>
                sp.GetRequiredService<TranslationMsDbContext>());
            services.AddScoped<DbContext>(sp =>
                sp.GetRequiredService<TranslationMsDbContext>());

            // Replace authentication with test handler
            var authDescriptors = services
                .Where(d => d.ServiceType == typeof(IAuthenticationSchemeProvider)
                         || d.ServiceType == typeof(IAuthenticationHandlerProvider))
                .ToList();
            foreach (var descriptor in authDescriptors)
                services.Remove(descriptor);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TranslationTestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TranslationTestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TranslationTestAuthHandler>(
                    TranslationTestAuthHandler.SchemeName, _ => { });

            // Remove hosted services
            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var descriptor in hostedServiceDescriptors)
                services.Remove(descriptor);
        });
    }

    public async Task InitializeAsync()
    {
        // Create the database schema
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslationMsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _connection.Close();
        await Task.CompletedTask;
    }

    public HttpClient CreateAnonymousClient() => CreateClient();

    public HttpClient CreateClientWithRoles(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        var rolesStr = string.Join(",", roles);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", $"{userId}|{rolesStr}");
        return client;
    }
}

/// <summary>
/// Test authentication handler for translation-ms.
/// Token format: "sub|role1,role2,..."
/// Uses short claim names ("role") to match the JWT configuration in Program.cs.
/// </summary>
public class TranslationTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";

    public TranslationTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = authHeader["Bearer ".Length..];
        var parts = token.Split('|');
        if (parts.Length < 1)
            return Task.FromResult(AuthenticateResult.Fail("Invalid test token format"));

        var userId = parts[0];
        var roles = parts.Length > 1 ? parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries) : [];

        var claims = new List<Claim>
        {
            new("sub", userId),
            new("email", $"{userId}@test.com")
        };

        // Use short "role" claim name to match RoleClaimType = "role" in Program.cs
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName, "sub", "role");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
