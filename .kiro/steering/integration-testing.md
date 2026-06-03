---
inclusion: fileMatch
fileMatchPattern: "**/*IntegrationTest*.cs"
---

# Integration Testing Guidelines (.NET)

## Overview
Integration tests verify the full request/response cycle through the actual HTTP layer using `WebApplicationFactory<Program>` and `HttpClient`.

## Test Structure

### CustomWebApplicationFactory

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace real DB with in-memory database
            // Remove real DbContext registrations
            services.AddDbContext<UserMsDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
            services.AddScoped<CoreMsDbContext>(sp => sp.GetRequiredService<UserMsDbContext>());
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<UserMsDbContext>());

            // Stub external dependencies (NotificationService, etc.)
            services.AddScoped(_ => Substitute.For<INotificationService>());

            // Replace authentication with test handler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

            // Remove hosted services to avoid background interference
        });
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", $"{Guid.NewGuid()}|USER_MS_ADMIN");
        return client;
    }

    public HttpClient CreateClientWithRoles(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        var rolesStr = string.Join(",", roles);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", $"{userId}|{rolesStr}");
        return client;
    }

    public HttpClient CreateAnonymousClient() => CreateClient();
}
```

### Test Authentication Handler

The `TestAuthHandler` parses the Bearer token as `{userId}|{roles}` and creates claims:

```csharp
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var parts = token.Split('|');
        var userId = parts[0];
        var roles = parts.Length > 1 ? parts[1].Split(',') : Array.Empty<string>();

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

## Writing Tests

### Test Class Structure
```csharp
public class UserApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _adminClient;

    public UserApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateAdminClient();
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        var request = new CreateUserRequest
        {
            Email = $"test{Guid.NewGuid():N}@example.com",
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _adminClient.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserCreatedDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(request.Email);
    }
}
```

### Testing Protected Endpoints
```csharp
[Fact]
public async Task GetUsers_AsAdmin_ReturnsOk()
{
    var client = _factory.CreateAdminClient();
    var response = await client.GetAsync("/api/users");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}

[Fact]
public async Task GetUsers_AsAnonymous_Returns401()
{
    var client = _factory.CreateAnonymousClient();
    var response = await client.GetAsync("/api/users");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task GetUsers_AsRegularUser_Returns403()
{
    var client = _factory.CreateClientWithRoles(Guid.NewGuid(), CoreMsRoles.UserMsUser);
    var response = await client.GetAsync("/api/users");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### Testing Error Responses
```csharp
[Fact]
public async Task CreateUser_WithDuplicateEmail_Returns409()
{
    var email = $"dup{Guid.NewGuid():N}@example.com";
    var request = new CreateUserRequest { Email = email, Password = "Pass123!" };

    await _adminClient.PostAsJsonAsync("/api/users", request);
    var response = await _adminClient.PostAsJsonAsync("/api/users", request);

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    error!.Errors[0].ReasonCode.Should().Be("user.exists");
}
```

## Test Data

### Unique Test Data
Always use unique identifiers to avoid test interference:
```csharp
var uniqueEmail = $"testuser{Guid.NewGuid():N}@example.com";
```

### Database Isolation
Each `CustomWebApplicationFactory` instance creates a unique in-memory database (`TestDb_{Guid}`), so test classes are isolated from each other.

## Testing Libraries
- **xUnit**: Test framework
- **FluentAssertions**: Readable assertions (`.Should().Be()`)
- **NSubstitute**: Mocking (for external services)
- **Microsoft.AspNetCore.Mvc.Testing**: WebApplicationFactory

## Key Patterns

1. **No real database** — uses EF Core InMemory provider for speed
2. **Test auth handler** — injects any user identity/roles without real JWT
3. **Stub external services** — mock NotificationService and other outbound calls
4. **No background services** — hosted services are removed to avoid interference
5. **Health checks simplified** — real infrastructure health checks are replaced
