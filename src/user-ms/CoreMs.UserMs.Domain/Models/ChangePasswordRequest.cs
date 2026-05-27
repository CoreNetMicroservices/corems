namespace CoreMs.UserMs.Domain.Models;

public record ChangePasswordRequest(
    string OldPassword,
    string NewPassword,
    string ConfirmPassword);
