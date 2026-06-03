namespace CoreMs.UserMs.Core.Models;

public record ChangePasswordRequest(
    string OldPassword,
    string NewPassword,
    string ConfirmPassword);
