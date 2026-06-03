namespace CoreMs.UserMs.Core.Models;

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword);
