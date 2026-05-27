namespace CoreMs.UserMs.Domain.Interfaces;

/// <summary>
/// Handles forgot-password and reset-password flows.
/// </summary>
public interface IPasswordService
{
    Task ForgotPasswordAsync(string email, CancellationToken ct = default);
    Task ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword, CancellationToken ct = default);
}
