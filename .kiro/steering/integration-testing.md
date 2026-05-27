---
inclusion: fileMatch
fileMatchPattern: "**/*IntegrationTest*.cs"
---

# Integration Testing Guidelines (.NET)

## Overview
Integration tests verify the full request/response cycle through the actual HTTP layer using `WebApplicationFactory<Program>` and `HttpClient`.

## Test Structure

### Use WebApplicationFactory with HttpClient
```csharp
public class UserMsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserMsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
}
```

### Custom WebApplicationFactory
```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DB with Testcontainers PostgreSQL
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<UserMsDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<UserMsDbContext>(options =>
                options.UseNpgsql(TestDatabaseConnection));
        });
    }
}
```

### Authentication in Integration Tests

**CRITICAL**: Use real authentication flow — don't mock JWT tokens for integration tests.

```csharp
// ✅ Correct - Use real authentication flow
private async Task<string> CreateUserAndAuthenticate()
{
    var signUpRequest = new SignUpRequest { Email = "test@example.com", Password = "Test123!" };
    await _client.PostAsJsonAsync("/api/auth/signup", signUpRequest);

    var signInRequest = new SignInRequest { Email = "test@example.com", Password = "Test123!" };
    var response = await _client.PostAsJsonAsync("/oauth2/token", signInRequest);
    var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", tokenResponse!.AccessToken);

    return tokenResponse.AccessToken;
}

// ❌ Wrong - Don't use fake/mocked tokens
_client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", "fake-token-here");
```

### Public vs Protected Endpoints

**Public endpoints** (no auth required):
- `/api/auth/signup`
- `/api/auth/verify-email`
- `/oauth2/token`
- Test directly without authentication setup

**Protected endpoints** (auth required):
- Call `CreateUserAndAuthenticate()` first
- Token is set on `HttpClient.DefaultRequestHeaders`

## Test Annotations & Organization

```csharp
[Collection("Integration")]
public class UserApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;

    public UserApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Setup: create test user, authenticate, etc.
        await CreateUserAndAuthenticate();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUserInfo()
    {
        var response = await _client.GetAsync("/api/profile");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserInfoDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
    }
}
```

## Exception Handling in Tests

```csharp
[Fact]
public async Task SignIn_WithInvalidCredentials_Returns400()
{
    var request = new SignInRequest { Email = "wrong@example.com", Password = "wrong" };
    var response = await _client.PostAsJsonAsync("/oauth2/token", request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    error!.ReasonCode.Should().Be("user.invalid_credentials");
}
```

## Test Organization

Group tests by endpoint type:
1. **Public Endpoints**: No auth required (signup, signin, verify)
2. **Protected Endpoints**: Require authentication (profile, user management)
3. **Unauthorized Access Tests**: Verify 401 responses without token

## Common Patterns

### Test Data Setup
```csharp
private SignUpRequest CreateUniqueSignUpRequest()
{
    var uniqueEmail = $"testuser{Guid.NewGuid():N}@example.com";
    return new SignUpRequest
    {
        Email = uniqueEmail,
        Password = "TestPassword123!",
        FirstName = "Test",
        LastName = "User"
    };
}
```

### Verifying Unauthorized Access
```csharp
[Fact]
public async Task ProtectedEndpoint_WithoutToken_Returns401()
{
    var client = _factory.CreateClient(); // Fresh client, no auth header
    var response = await client.GetAsync("/api/profile");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## Testing Libraries
- **xUnit**: Test framework
- **FluentAssertions**: Readable assertions (`.Should().Be()`)
- **Testcontainers**: Real PostgreSQL for integration tests
- **Bogus**: Fake data generation (optional)

## Testcontainers Setup
```csharp
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("corems_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```
