using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Interfaces;

/// <summary>
/// Handles user registration, email/phone verification, and resend flows.
/// </summary>
public interface IRegistrationService
{
    Task<UserEntity> SignUpAsync(SignUpRequest request, CancellationToken ct = default);
    Task VerifyEmailAsync(string email, string token, CancellationToken ct = default);
    Task VerifyPhoneAsync(string phoneNumber, string code, CancellationToken ct = default);
    Task ResendVerificationAsync(string email, string type, CancellationToken ct = default);
}
