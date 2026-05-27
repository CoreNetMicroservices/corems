---
inclusion: fileMatch
fileMatchPattern: "**/Exceptions/**"
---

# Exception Handling (.NET)

## Architecture

All business errors use `ServiceException` with `ErrorInfo` static fields. Zero reflection, zero enums with attributes.

### Core Types (in `CoreMs.Common.Exceptions`)

```csharp
// Defines an error: reason code + HTTP status + description
public record ErrorInfo(string ErrorCode, int HttpStatusCode, string Description);

// A single error in the response
public record Error(string ReasonCode, string Description, string? Details = null);

// The response envelope
public record ErrorResponse(IReadOnlyList<Error> Errors);

// The exception itself
public class ServiceException : Exception
{
    public int HttpStatusCode { get; }
    public IReadOnlyList<Error> Errors { get; }

    public static ServiceException Of(ErrorInfo errorInfo, string? details = null);
    public static ServiceException Of(IReadOnlyList<Error> errors, int httpStatusCode);
}
```

## Throwing Errors

```csharp
// Simple — uses ErrorInfo description
throw ServiceException.Of(DefaultErrors.NotFound);

// With details — adds context without changing the reason code
throw ServiceException.Of(UserErrors.UserNotFound, $"No user with email '{email}'");

// Multiple errors at once
throw ServiceException.Of(errors, 400);
```

## Defining Error Codes

### Common errors (`DefaultErrors` in CoreMs.Common)

```csharp
public static class DefaultErrors
{
    public static readonly ErrorInfo ServerError = new("server.error", 500, "Unexpected error. Try again later.");
    public static readonly ErrorInfo NotFound = new("resource.not.found", 404, "Resource is not found");
    public static readonly ErrorInfo ValidationFailed = new("validation.failed", 400, "Validation failed");
    public static readonly ErrorInfo InvalidInput = new("invalid.data", 400, "Invalid input data");
    public static readonly ErrorInfo Conflict = new("resource.conflict", 409, "Resource conflict detected");
    public static readonly ErrorInfo Unauthorized = new("user.unauthorized", 401, "User is unauthorized");
    public static readonly ErrorInfo Forbidden = new("user.forbidden", 403, "Access to this resource is forbidden");
    public static readonly ErrorInfo RequestCancelled = new("request.cancelled", 499, "Request was cancelled");
}
```

### Service-specific errors (in Domain layer)

```csharp
public static class UserErrors
{
    public static readonly ErrorInfo UserExists = new("user.exists", 409, "User already exists");
    public static readonly ErrorInfo UserNotFound = new("user.not_found", 404, "User not found");
    public static readonly ErrorInfo InvalidCredentials = new("auth.invalid_credentials", 401, "Invalid credentials");
    public static readonly ErrorInfo AccountDisabled = new("auth.account_disabled", 403, "Account is disabled");
}
```

### Naming convention for error codes
- Lowercase with dots: `user.not_found`, `auth.invalid_credentials`
- Prefix with domain: `user.*`, `auth.*`, `template.*`

## Response Format

All errors return:
```json
{
  "errors": [
    {
      "reasonCode": "user.not_found",
      "description": "User not found",
      "details": "No user with email 'foo@bar.com'"
    }
  ]
}
```

## GlobalExceptionHandler

Located in `CoreMs.Common.Middleware`. Implements `IExceptionHandler` and handles:

| Exception Type | Behavior |
|---|---|
| `ServiceException` | Maps directly to ErrorResponse |
| `ValidationException` (FluentValidation) | Maps each failure to an Error with `validation.failed` code |
| `JsonException` | Returns `format.invalid` 400 |
| `BadHttpRequestException` | Returns `invalid.request` with original status |
| `OperationCanceledException` | Returns `request.cancelled` 499 |
| `UnauthorizedAccessException` | Returns `user.unauthorized` 401 |
| Any other exception | Logs error, returns `server.error` 500 |

## Rules

1. **Never create custom exception classes** — always use `ServiceException.Of(ErrorInfo, details?)`
2. **Never use enums with attributes** — use `static readonly ErrorInfo` fields
3. **One error class per service** — e.g., `UserErrors`, `TemplateErrors`
4. **Place error classes in Domain layer** — `CoreMs.<Service>Ms.Domain/Exceptions/`
5. **Use `DefaultErrors` for generic cases** — don't redefine common codes per service
6. **Log before throwing** when wrapping external exceptions
