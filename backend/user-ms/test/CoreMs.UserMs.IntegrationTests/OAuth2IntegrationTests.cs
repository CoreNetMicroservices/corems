using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreMs.UserMs.IntegrationTests;

/// <summary>
/// Integration tests for OAuth2/OIDC endpoints: discovery, token exchange, authorize, and revocation.
/// </summary>
public class OAuth2IntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _dbName = $"OAuth2Test_{Guid.NewGuid():N}";

    private static readonly Guid TestUserUuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string TestUserEmail = "oauth-test@example.com";
    private const string TestUserPassword = "Test1234!";

    public OAuth2IntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SecretKey"] = "integration-test-secret-key-minimum-32-chars!!",
                        ["Jwt:Issuer"] = "http://localhost",
                        ["Jwt:Audience"] = "corems",
                        ["Jwt:Algorithm"] = "HS256",
                        ["Jwt:KeyId"] = "test-key-1",
                        ["Jwt:AccessTokenExpirationMinutes"] = "10",
                        ["Jwt:RefreshTokenExpirationMinutes"] = "1440",
                        ["Jwt:IdTokenExpirationMinutes"] = "60",
                        ["Jwt:AuthorizationCodeExpirationMinutes"] = "10",
                        ["OAuth2Clients:Clients:0:ClientId"] = "corems-web",
                        ["OAuth2Clients:Clients:0:RedirectUris:0"] = "http://localhost:8080/callback",
                        ["OAuth2Clients:Clients:0:AllowedScopes:0"] = "openid",
                        ["OAuth2Clients:Clients:0:AllowedScopes:1"] = "profile",
                        ["OAuth2Clients:Clients:0:AllowedScopes:2"] = "email",
                        ["OAuth2Clients:Clients:0:AllowedGrantTypes:0"] = "authorization_code",
                        ["OAuth2Clients:Clients:0:AllowedGrantTypes:1"] = "refresh_token",
                        ["OAuth2Clients:Clients:0:RequirePkce"] = "true",
                        ["SocialAuth:Google:ClientId"] = "test",
                        ["SocialAuth:Google:ClientSecret"] = "test",
                        ["SocialAuth:GitHub:ClientId"] = "test",
                        ["SocialAuth:GitHub:ClientSecret"] = "test",
                        ["SocialAuth:LinkedIn:ClientId"] = "test",
                        ["SocialAuth:LinkedIn:ClientSecret"] = "test",
                        ["RabbitMq:Host"] = "localhost",
                        ["RabbitMq:Port"] = "5672",
                        ["RabbitMq:Username"] = "guest",
                        ["RabbitMq:Password"] = "guest",
                        ["RabbitMq:VirtualHost"] = "/",
                        ["App:FrontendBaseUrl"] = "http://localhost:8080",
                        ["App:VerificationEmailExpirationMinutes"] = "1440",
                        ["App:PasswordResetExpirationMinutes"] = "1440",
                        ["App:DefaultRoles:0"] = "USER_MS_USER",
                        ["NotificationTemplates:Email:Welcome"] = "",
                        ["NotificationTemplates:Email:EmailVerification"] = "",
                        ["NotificationTemplates:Email:PasswordReset"] = "",
                        ["NotificationTemplates:Sms:Welcome"] = "",
                        ["NotificationTemplates:Sms:VerificationCode"] = "",
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Remove real DbContext registrations
                    var dbDescriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<UserMsDbContext>)
                                 || d.ServiceType == typeof(UserMsDbContext)
                                 || d.ServiceType.FullName?.Contains("DbContextOptions") == true)
                        .ToList();
                    foreach (var d in dbDescriptors)
                        services.Remove(d);

                    // Add InMemory database
                    services.AddDbContext<UserMsDbContext>(options =>
                        options.UseInMemoryDatabase(_dbName));

                    services.AddScoped<CoreMs.Common.Data.CoreMsDbContext>(sp =>
                        sp.GetRequiredService<UserMsDbContext>());

                    // Remove health checks that require real infrastructure
                    var healthDescriptors = services
                        .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                        .ToList();
                    foreach (var d in healthDescriptors)
                        services.Remove(d);
                    services.AddHealthChecks();

                    // Remove hosted services to avoid background task interference
                    var hostedDescriptors = services
                        .Where(d => d.ServiceType == typeof(IHostedService))
                        .ToList();
                    foreach (var d in hostedDescriptors)
                        services.Remove(d);

                    // Replace auth with test handler for authorize endpoint tests
                    services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = "OAuth2TestAuth";
                            options.DefaultChallengeScheme = "OAuth2TestAuth";
                        })
                        .AddScheme<AuthenticationSchemeOptions, OAuth2TestAuthHandler>(
                            "OAuth2TestAuth", _ => { });
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserMsDbContext>();
        db.Database.EnsureCreated();

        if (db.Set<UserEntity>().Any(u => u.Email == TestUserEmail))
            return;

        var user = new UserEntity
        {
            Uuid = TestUserUuid,
            Provider = "local",
            Email = TestUserEmail,
            FirstName = "OAuth",
            LastName = "Tester",
            Password = BCrypt.Net.BCrypt.HashPassword(TestUserPassword, 12),
            EmailVerified = true,
            PhoneVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Set<UserEntity>().Add(user);
        db.SaveChanges();

        var role = new UserRoleEntity
        {
            UserId = user.Id,
            Name = "USER_MS_USER",
            UpdatedAt = DateTime.UtcNow
        };
        db.Set<UserRoleEntity>().Add(role);
        db.SaveChanges();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    #region Discovery Endpoints

    [Fact]
    public async Task GetOpenIdConfiguration_ReturnsDiscoveryDocument()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.GetProperty("issuer").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("authorization_endpoint").GetString().Should().Contain("/oauth2/authorize");
        root.GetProperty("token_endpoint").GetString().Should().Contain("/oauth2/token");
        root.GetProperty("revocation_endpoint").GetString().Should().Contain("/oauth2/revoke");
        root.GetProperty("jwks_uri").GetString().Should().Contain("/.well-known/jwks.json");
        root.GetProperty("grant_types_supported").EnumerateArray().Should().NotBeEmpty();
        root.GetProperty("response_types_supported").EnumerateArray().Should().NotBeEmpty();
        root.GetProperty("scopes_supported").EnumerateArray().Should().NotBeEmpty();
        root.GetProperty("code_challenge_methods_supported").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOpenIdConfiguration_IssuerMatchesEndpoints()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var issuer = doc.RootElement.GetProperty("issuer").GetString()!;
        var tokenEndpoint = doc.RootElement.GetProperty("token_endpoint").GetString()!;

        tokenEndpoint.Should().StartWith(issuer);
    }

    [Fact]
    public async Task GetJwks_ReturnsEmptyKeysObject()
    {
        var response = await _client.GetAsync("/.well-known/jwks.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        doc.RootElement.GetProperty("keys").GetArrayLength().Should().Be(0);
    }

    #endregion

    #region Password Grant

    [Fact]
    public async Task Token_PasswordGrant_ValidCredentials_ReturnsTokens()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = TestUserEmail,
            ["password"] = TestUserPassword,
            ["scope"] = "openid profile email"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonDocument.Parse(content).RootElement;

        tokenResponse.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        tokenResponse.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        tokenResponse.GetProperty("tokenType").GetString().Should().Be("Bearer");
        tokenResponse.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);
        tokenResponse.GetProperty("scope").GetString().Should().Contain("openid");
    }

    [Fact]
    public async Task Token_PasswordGrant_ValidCredentials_ReturnsIdToken()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = TestUserEmail,
            ["password"] = TestUserPassword,
            ["scope"] = "openid profile email"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonDocument.Parse(content).RootElement;

        tokenResponse.GetProperty("idToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Token_PasswordGrant_InvalidPassword_ReturnsUnauthorized()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = TestUserEmail,
            ["password"] = "WrongPassword123!",
            ["scope"] = "openid"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_PasswordGrant_NonExistentUser_ReturnsUnauthorized()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = "nonexistent@example.com",
            ["password"] = "SomePass123!",
            ["scope"] = "openid"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Refresh Token Grant

    [Fact]
    public async Task Token_RefreshTokenGrant_ValidToken_ReturnsNewTokens()
    {
        var refreshToken = await ObtainRefreshTokenAsync();

        var refreshForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        var response = await _client.PostAsync("/oauth2/token", refreshForm);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonDocument.Parse(content).RootElement;

        tokenResponse.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        tokenResponse.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        // Rotation: new refresh token must differ from old one
        tokenResponse.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task Token_RefreshTokenGrant_InvalidToken_ReturnsUnauthorized()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = "invalid-refresh-token-value"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_RefreshTokenGrant_UsedToken_CannotBeReused()
    {
        var refreshToken = await ObtainRefreshTokenAsync();

        var refreshForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        // First use should succeed
        var first = await _client.PostAsync("/oauth2/token", refreshForm);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second use of same token should fail (strict rotation deletes old token)
        var second = await _client.PostAsync("/oauth2/token", refreshForm);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Code Grant

    [Fact]
    public async Task Token_AuthorizationCodeGrant_ValidCode_ReturnsTokens()
    {
        var code = Guid.NewGuid().ToString();
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = ComputeCodeChallenge(codeVerifier);

        SeedAuthorizationCode(code, codeChallenge, "S256", expiresInMinutes: 10);

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = "http://localhost:8080/callback",
            ["code_verifier"] = codeVerifier,
            ["client_id"] = "corems-web"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonDocument.Parse(content).RootElement;

        tokenResponse.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        tokenResponse.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        tokenResponse.GetProperty("idToken").GetString().Should().NotBeNullOrEmpty();
        tokenResponse.GetProperty("tokenType").GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task Token_AuthorizationCodeGrant_ExpiredCode_ReturnsBadRequest()
    {
        var code = Guid.NewGuid().ToString();
        SeedAuthorizationCode(code, codeChallenge: null, codeChallengeMethod: null, expiresInMinutes: -5);

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = "http://localhost:8080/callback",
            ["client_id"] = "corems-web"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Token_AuthorizationCodeGrant_UsedCode_ReturnsBadRequest()
    {
        var code = Guid.NewGuid().ToString();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UserMsDbContext>();
            var user = db.Set<UserEntity>().First(u => u.Email == TestUserEmail);

            db.Set<AuthorizationCodeEntity>().Add(new AuthorizationCodeEntity
            {
                Code = code,
                UserId = user.Id,
                User = user,
                ClientId = "corems-web",
                RedirectUri = "http://localhost:8080/callback",
                Scope = "openid",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddMinutes(-1),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            });
            db.SaveChanges();
        }

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = "http://localhost:8080/callback",
            ["client_id"] = "corems-web"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Token_AuthorizationCodeGrant_InvalidPkceVerifier_ReturnsBadRequest()
    {
        var code = Guid.NewGuid().ToString();
        var realVerifier = GenerateCodeVerifier();
        var realChallenge = ComputeCodeChallenge(realVerifier);

        SeedAuthorizationCode(code, realChallenge, "S256", expiresInMinutes: 10);

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = "http://localhost:8080/callback",
            ["code_verifier"] = "wrong-verifier-that-will-not-match-challenge",
            ["client_id"] = "corems-web"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorize Endpoint

    [Fact]
    public async Task Authorize_AuthenticatedUser_RedirectsWithCode()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", $"{TestUserUuid}|USER_MS_USER");

        var response = await _client.GetAsync(
            "/oauth2/authorize?response_type=code&client_id=corems-web" +
            "&redirect_uri=http://localhost:8080/callback&scope=openid&state=xyz123");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var location = response.Headers.Location!.ToString();
        location.Should().StartWith("http://localhost:8080/callback");
        location.Should().Contain("code=");
        location.Should().Contain("state=xyz123");

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Authorize_UnauthenticatedUser_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(
            "/oauth2/authorize?response_type=code&client_id=corems-web" +
            "&redirect_uri=http://localhost:8080/callback&scope=openid");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token Revocation

    [Fact]
    public async Task Revoke_ValidToken_Returns200()
    {
        var refreshToken = await ObtainRefreshTokenAsync();

        var revokeForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token"
        });

        var response = await _client.PostAsync("/oauth2/revoke", revokeForm);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revoke_InvalidToken_StillReturns200()
    {
        var revokeForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = "completely-invalid-token",
            ["token_type_hint"] = "refresh_token"
        });

        var response = await _client.PostAsync("/oauth2/revoke", revokeForm);

        // RFC 7009: always return 200
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revoke_Token_CannotBeUsedAfterRevocation()
    {
        var refreshToken = await ObtainRefreshTokenAsync();

        // Revoke the token
        var revokeForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token"
        });
        await _client.PostAsync("/oauth2/revoke", revokeForm);

        // Try to use the revoked token for refresh
        var refreshForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        var response = await _client.PostAsync("/oauth2/token", refreshForm);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    private async Task<string> ObtainRefreshTokenAsync()
    {
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = TestUserEmail,
            ["password"] = TestUserPassword,
            ["scope"] = "openid"
        });

        var response = await _client.PostAsync("/oauth2/token", formData);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement.GetProperty("refreshToken").GetString()!;
    }

    private void SeedAuthorizationCode(
        string code,
        string? codeChallenge,
        string? codeChallengeMethod,
        int expiresInMinutes)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserMsDbContext>();
        var user = db.Set<UserEntity>().First(u => u.Email == TestUserEmail);

        db.Set<AuthorizationCodeEntity>().Add(new AuthorizationCodeEntity
        {
            Code = code,
            UserId = user.Id,
            User = user,
            ClientId = "corems-web",
            RedirectUri = "http://localhost:8080/callback",
            Scope = "openid profile email",
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    #endregion
}

/// <summary>
/// Test authentication handler for OAuth2 integration tests.
/// Authenticates requests with "Bearer sub|roles" format.
/// </summary>
public class OAuth2TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public OAuth2TestAuthHandler(
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
            return Task.FromResult(AuthenticateResult.Fail("Invalid test token"));

        var userId = parts[0];
        var roles = parts.Length > 1 ? parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries) : [];

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "OAuth2TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "OAuth2TestAuth");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
