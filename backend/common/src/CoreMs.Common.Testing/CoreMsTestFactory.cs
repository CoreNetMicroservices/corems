using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CoreMs.Common.Data;
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

namespace CoreMs.Common.Testing;

/// <summary>
/// Base WebApplicationFactory for CoreMS integration tests.
/// Swaps PostgreSQL for in-memory SQLite, replaces JWT auth with a test handler,
/// removes hosted services, and provides helper methods for creating test clients.
///
/// Usage:
///   public class MyTestFactory : CoreMsTestFactory&lt;Program, MyMsDbContext&gt; { }
///   public class MyTests : IClassFixture&lt;MyTestFactory&gt; { ... }
/// </summary>
public abstract class CoreMsTestFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
    where TDbContext : CoreMsDbContext
{
    private readonly SqliteConnection _connection;

    protected CoreMsTestFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            RemoveInfrastructureServices<TDbContext>(services);
            RegisterSqliteDbContext<TDbContext>(services, _connection);
            ReplaceAuthWithTestHandler(services);
            RemoveHostedServices(services);
            ConfigureTestServices(services);
        });
    }

    /// <summary>
    /// Override to register additional test-specific services or mocks.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services) { }

    public virtual async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _connection.Close();
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Client factory helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates an unauthenticated HTTP client.</summary>
    public HttpClient CreateAnonymousClient() => CreateClient();

    /// <summary>Creates an authenticated HTTP client with the specified user and roles.</summary>
    public HttpClient CreateClientWithRoles(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        var rolesStr = string.Join(",", roles);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", $"{userId}|{rolesStr}");
        return client;
    }

    /// <summary>Creates an authenticated HTTP client with a random user and the specified roles.</summary>
    public HttpClient CreateClientWithRoles(params string[] roles)
        => CreateClientWithRoles(Guid.NewGuid(), roles);

    // -------------------------------------------------------------------------
    // Infrastructure swap helpers
    // -------------------------------------------------------------------------

    private static void RemoveInfrastructureServices<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<TContext>)
                || d.ServiceType == typeof(TContext)
                || d.ServiceType == typeof(CoreMsDbContext)
                || d.ServiceType == typeof(DbContext)
                || (d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                || d.ServiceType.FullName?.Contains("EntityFramework") == true
                || d.ServiceType.FullName?.Contains("Npgsql") == true
                || d.ImplementationType?.FullName?.Contains("Npgsql") == true
                || d.ImplementationType?.FullName?.Contains("EntityFramework") == true)
            .ToList();

        foreach (var descriptor in toRemove)
            services.Remove(descriptor);

        services.RemoveAll<DbContextOptions>();

        // Remove health checks that depend on infrastructure
        var healthChecks = services
            .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
            .ToList();
        foreach (var descriptor in healthChecks)
            services.Remove(descriptor);
        services.AddHealthChecks();
    }

    private static void RegisterSqliteDbContext<TContext>(IServiceCollection services, SqliteConnection connection)
        where TContext : CoreMsDbContext
    {
        services.AddDbContext<TContext>((_, options) => options.UseSqlite(connection));
        services.AddScoped<CoreMsDbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
    }

    private static void ReplaceAuthWithTestHandler(IServiceCollection services)
    {
        var authDescriptors = services
            .Where(d => d.ServiceType == typeof(IAuthenticationSchemeProvider)
                     || d.ServiceType == typeof(IAuthenticationHandlerProvider))
            .ToList();
        foreach (var descriptor in authDescriptors)
            services.Remove(descriptor);

        services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = CoreMsTestAuthHandler.SchemeName;
                o.DefaultChallengeScheme = CoreMsTestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, CoreMsTestAuthHandler>(
                CoreMsTestAuthHandler.SchemeName, _ => { });
    }

    private static void RemoveHostedServices(IServiceCollection services)
    {
        var hostedServices = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .ToList();
        foreach (var descriptor in hostedServices)
            services.Remove(descriptor);
    }
}

/// <summary>
/// Test authentication handler for CoreMS integration tests.
/// Token format: "userId|role1,role2,..."
/// Uses short claim names to match the JWT configuration.
/// </summary>
public class CoreMsTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestScheme";

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

        foreach (var role in roles)
            claims.Add(new Claim("role", role));

        var identity = new ClaimsIdentity(claims, SchemeName, "sub", "role");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
