namespace CoreMs.UserMs.Domain.Models;

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword);
