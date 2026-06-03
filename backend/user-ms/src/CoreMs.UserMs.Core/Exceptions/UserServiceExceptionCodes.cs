using CoreMs.Common.Exceptions;

namespace CoreMs.UserMs.Core.Exceptions;

/// <summary>
/// Error definitions specific to the User Management Service.
/// </summary>
public static class UserErrors
{
    public static readonly ErrorInfo UserExists = new("user.exists", 409, "User already exists");
    public static readonly ErrorInfo UserNotFound = new("user.not_found", 404, "User not found");
    public static readonly ErrorInfo InvalidRole = new("user.invalid_role", 400, "Invalid role specified");
    public static readonly ErrorInfo TokenNotFound = new("token.not_found", 400, "Token not found");
    public static readonly ErrorInfo InvalidCredentials = new("auth.invalid_credentials", 401, "Invalid credentials");
    public static readonly ErrorInfo AccountDisabled = new("auth.account_disabled", 403, "Account is disabled");
    public static readonly ErrorInfo EmailNotVerified = new("auth.email_not_verified", 403, "Email not verified");
    public static readonly ErrorInfo TokenExpired = new("auth.token_expired", 410, "Token has expired");
    public static readonly ErrorInfo TokenConsumed = new("auth.token_consumed", 410, "Token already used");
    public static readonly ErrorInfo PasswordMismatch = new("auth.password_mismatch", 400, "Password mismatch");
    public static readonly ErrorInfo InvalidRequest = new("auth.invalid_request", 400, "Invalid request");
    public static readonly ErrorInfo InvalidToken = new("auth.invalid_token", 401, "Invalid token");
}
