using CoreMs.CommunicationMs.Client;
using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CoreMs.UserMs.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory that uses EF Core InMemory provider instead of Testcontainers.
/// Use this when Docker is not available. Trade-off: no real PostgreSQL constraints
/// (unique indexes, identity columns) but enables running tests without Docker.
/// </summary>
public class InMemoryWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"UserMsTest_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "integration-test-secret-key-minimum-32-chars!",
                ["Jwt:Issuer"] = "corems-test",
                ["Jwt:Algorithm"] = "HS256",
                ["App:FrontendBaseUrl"] = "http://localhost:8080"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core related registrations to avoid provider conflicts.
            // The Npgsql provider registers singletons that conflict with InMemory.
            var efRelated = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<UserMsDbContext>)
                    || d.ServiceType == typeof(UserMsDbContext)
                    || d.ServiceType == typeof(CoreMs.Common.Data.CoreMsDbContext)
                    || d.ServiceType == typeof(DbContext)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                    || d.ServiceType.FullName?.Contains("EntityFramework") == true
                    || d.ServiceType.FullName?.Contains("Npgsql") == true
                    || d.ImplementationType?.FullName?.Contains("Npgsql") == true
                    || d.ImplementationType?.FullName?.Contains("EntityFramework") == true
                    || (d.ServiceType.FullName?.Contains("IDbContext") == true))
                .ToList();
            foreach (var descriptor in efRelated)
                services.Remove(descriptor);

            // Also remove any remaining DbContextOptions (non-generic)
            services.RemoveAll<DbContextOptions>();

            // Remove health checks that require real infrastructure
            var healthCheckDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();
            foreach (var descriptor in healthCheckDescriptors)
                services.Remove(descriptor);
            services.AddHealthChecks();

            // Register InMemory DbContext fresh
            services.AddDbContext<UserMsDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            services.AddScoped<CoreMs.Common.Data.CoreMsDbContext>(sp =>
                sp.GetRequiredService<UserMsDbContext>());
            services.AddScoped<DbContext>(sp =>
                sp.GetRequiredService<UserMsDbContext>());

            // Stub CommunicationMsClient with a no-op HttpClient (returns 200 for all requests)
            services.RemoveAll<CommunicationMsClient>();
            var mockHandler = new NoOpHttpMessageHandler();
            services.AddScoped(_ => new CommunicationMsClient(
                new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost") }));

            // Replace authentication with test handler for role injection
            var authDescriptors = services
                .Where(d => d.ServiceType == typeof(IAuthenticationSchemeProvider)
                         || d.ServiceType == typeof(IAuthenticationHandlerProvider))
                .ToList();
            foreach (var descriptor in authDescriptors)
                services.Remove(descriptor);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // Remove hosted services (TokenCleanupService) to avoid background interference
            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var descriptor in hostedServiceDescriptors)
                services.Remove(descriptor);
        });
    }

    /// <summary>
    /// Creates an HttpClient with no authentication header (anonymous).
    /// </summary>
    public HttpClient CreateAnonymousClient() => CreateClient();

    /// <summary>
    /// Creates an HttpClient with admin credentials (USER_MS_ADMIN role).
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", $"{Guid.NewGuid()}|USER_MS_ADMIN");
        return client;
    }

    /// <summary>
    /// Creates an HttpClient with specific user UUID and roles.
    /// </summary>
    public HttpClient CreateClientWithRoles(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        var rolesStr = string.Join(",", roles);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", $"{userId}|{rolesStr}");
        return client;
    }
}

/// <summary>
/// HTTP message handler that returns 200 OK for all requests without making real network calls.
/// </summary>
internal class NoOpHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
    }
}
