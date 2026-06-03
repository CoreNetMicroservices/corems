---
inclusion: fileMatch
fileMatchPattern: "**/{Security,Auth,Controllers,Services}/**/*.cs"
---

# Security Guidelines (.NET)

## Roles
Use role constants from `CoreMs.Common.Security.CoreMsRoles`:

```csharp
public static class CoreMsRoles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string UserMsAdmin = "USER_MS_ADMIN";
    public const string UserMsUser = "USER_MS_USER";
}
```

Add new role constants here as new services are introduced.

## Identity Resolution
- Use `ICurrentUserService` (registered via DI in Program.cs):
  ```csharp
  public interface ICurrentUserService
  {
      Guid GetCurrentUserUuid();
      string GetCurrentUserEmail();
      IReadOnlyList<string> GetCurrentUserRoles();
      bool IsInRole(string role);
  }
  ```
- Resolves identity from `HttpContext.User` claims only
- Throws `InvalidOperationException` if user is not authenticated (use only behind `[Authorize]`)
- DO NOT rely on custom headers (X-User) or client-supplied identity

### Registration in Program.cs
```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
```

## Role-Based Access
```csharp
[Authorize(Roles = CoreMsRoles.UserMsAdmin)]
[HttpGet]
public async Task<ActionResult<PagedResult<UserInfoDto>>> GetUsers(
    [FromQuery] QueryParameters parameters, CancellationToken ct)
{
    // Only USER_MS_ADMIN can access
}

// Multiple roles (OR logic)
[Authorize(Roles = $"{CoreMsRoles.UserMsAdmin},{CoreMsRoles.SuperAdmin}")]
[HttpDelete("{userId:guid}")]
public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct) { }
```

## JWT Configuration

### appsettings.json (JwtOptions)
```json
{
  "Jwt": {
    "Algorithm": "HS256",
    "Issuer": "corems-user-ms",
    "Audience": "corems",
    "KeyId": "corems-1",
    "SecretKey": "",
    "AccessTokenExpirationMinutes": 10,
    "RefreshTokenExpirationMinutes": 1440
  }
}
```

### Registration in Program.cs (User Service handles JWT directly)
```csharp
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();
```

## Using ICurrentUserService in Controllers

```csharp
[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;
    private readonly ICurrentUserService _currentUserService;

    public ProfileController(ProfileService profileService, ICurrentUserService currentUserService)
    {
        _profileService = profileService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<UserInfoDto>> GetProfile(CancellationToken ct)
    {
        var userUuid = _currentUserService.GetCurrentUserUuid();
        var user = await _profileService.GetProfileAsync(userUuid, ct);
        return Ok(user);
    }
}
```

## Rate Limiting

Applied via policies in Program.cs:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("registration", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromHours(1) }));
});
```

Used on controllers:
```csharp
[EnableRateLimiting("login")]
[HttpPost("/oauth2/token")]
public async Task<IActionResult> Token([FromBody] TokenRequest request, CancellationToken ct) { }
```

## Security Best Practices
- Use `[Authorize]` attribute on controllers/actions (not manual token checks)
- Use policy-based authorization for complex rules
- Never store secrets in appsettings.json — use User Secrets or environment variables
- Use HTTPS in production
- Validate all input with FluentValidation (validators in Api/Validators/)
- Use parameterized queries (EF Core handles this automatically)
- Rate limit sensitive endpoints (login, registration, password reset)
