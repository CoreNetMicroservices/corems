using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace CoreMs.UserMs.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory with test authentication handler that allows injecting
/// specific user identities and roles. Uses EF Core in-memory database.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<UserMsDbContext>)
                         || d.ServiceType == typeof(UserMsDbContext)
                         || d.ServiceType.FullName?.Contains("DbContextOptions") == true)
                .ToList();
            foreach (var descriptor in dbDescriptors)
                services.Remove(descriptor);

            // Remove health checks that require real infrastructure
            var healthCheckDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();
            foreach (var descriptor in healthCheckDescriptors)
                services.Remove(descriptor);
            services.AddHealthChecks();

            // Register DbContext with in-memory database
            services.AddDbContext<UserMsDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            services.AddScoped<CoreMs.Common.Data.CoreMsDbContext>(sp =>
                sp.GetRequiredService<UserMsDbContext>());
            services.AddScoped<DbContext>(sp =>
                sp.GetRequiredService<UserMsDbContext>());

            // Stub NotificationService to avoid real messaging
            var notifDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(NotificationService));
            if (notifDescriptor != null) services.Remove(notifDescriptor);
            services.AddScoped(_ => Substitute.For<NotificationService>());

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

    /// <summary>
    /// Creates an HttpClient with no authentication.
    /// </summary>
    public HttpClient CreateAnonymousClient()
    {
        return CreateClient();
    }
}
