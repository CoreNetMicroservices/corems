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
    // User Microservice
    public const string UserMsAdmin = "USER_MS_ADMIN";
    public const string UserMsUser = "USER_MS_USER";

    // Communication Microservice
    public const string CommunicationMsAdmin = "COMMUNICATION_MS_ADMIN";
    public const string CommunicationMsUser = "COMMUNICATION_MS_USER";

    // Translation Microservice
    public const string TranslationMsAdmin = "TRANSLATION_MS_ADMIN";

    // Document Microservice
    public const string DocumentMsAdmin = "DOCUMENT_MS_ADMIN";
    public const string DocumentMsUser = "DOCUMENT_MS_USER";

    // System roles
    public const string System = "SYSTEM";
    public const string SuperAdmin = "SUPER_ADMIN";
}
```

## Identity Resolution
- Use `ICurrentUserService` (registered via DI):
  ```csharp
  public interface ICurrentUserService
  {
      Guid? UserId { get; }
      string? Email { get; }
      IReadOnlyList<string> Roles { get; }
      bool IsAuthenticated { get; }
  }
  ```
- Resolve identity from `HttpContext.User` claims only
- DO NOT rely on custom headers (X-User) or client-supplied identity

## Role-Based Access
```csharp
[Authorize(Roles = CoreMsRoles.UserMsAdmin)]
[HttpGet]
public async Task<ActionResult<PagedResult<UserInfoDto>>> GetUsers(
    [FromQuery] QueryParameters parameters)
{
    // Only USER_MS_ADMIN can access
}

// Multiple roles (OR logic)
[Authorize(Roles = $"{CoreMsRoles.UserMsAdmin},{CoreMsRoles.SuperAdmin}")]
[HttpDelete("{userId:guid}")]
public async Task<IActionResult> DeleteUser(Guid userId) { }
```

## JWT Configuration

### appsettings.json
```json
{
  "Jwt": {
    "Algorithm": "HS256",
    "Issuer": "http://localhost:5000",
    "Audience": "corems",
    "KeyId": "corems-1",
    "SecretKey": "",
    "PrivateKey": "",
    "PublicKey": "",
    "AccessTokenExpirationMinutes": 10,
    "RefreshTokenExpirationMinutes": 1440
  }
}
```

### Registration in Program.cs
```csharp
builder.Services.AddCoreMsSecurity(builder.Configuration);
// This registers JWT bearer authentication, authorization policies, and ICurrentUserService
```

## Sender Identity (Messages/Notifications)
Populate sender metadata in service layer:

```csharp
public class MessageService
{
    private readonly ICurrentUserService _currentUser;

    public async Task CreateMessage(CreateMessageDto dto)
    {
        var entity = new MessageEntity();

        if (_currentUser.IsAuthenticated && _currentUser.UserId.HasValue)
        {
            entity.SentById = _currentUser.UserId.Value;
            entity.SentByType = SenderType.User;
        }
        else
        {
            entity.SentByType = SenderType.System;
            // Do NOT populate SentById
        }
    }
}
```

## Security Best Practices
- Use `[Authorize]` attribute on controllers/actions (not manual token checks)
- Use policy-based authorization for complex rules
- Never store secrets in appsettings.json — use User Secrets or environment variables
- Use HTTPS in production
- Validate all input with FluentValidation or DataAnnotations on DTOs
- Use parameterized queries (EF Core handles this automatically)
