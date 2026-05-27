using CoreMs.UserMs.Domain.Entities;

namespace CoreMs.UserMs.Domain.Interfaces;

/// <summary>
/// Abstraction for sending notifications (email, SMS). Implemented via MassTransit.
/// </summary>
public interface INotificationService
{
    Task SendEmailVerificationAsync(UserEntity user, string token, CancellationToken ct = default);
    Task SendPhoneVerificationAsync(UserEntity user, string code, CancellationToken ct = default);
    Task SendPasswordResetAsync(UserEntity user, string token, CancellationToken ct = default);
}
