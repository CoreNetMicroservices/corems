using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreMs.UserMs.Infrastructure.Services;

/// <summary>
/// Stub notification service. Will be replaced with MassTransit/RabbitMQ implementation.
/// Currently just logs the notification intent.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(UserEntity user, string token, CancellationToken ct = default)
    {
        _logger.LogInformation("Email verification for {Email}: token={Token}", user.Email, token);
        return Task.CompletedTask;
    }

    public Task SendPhoneVerificationAsync(UserEntity user, string code, CancellationToken ct = default)
    {
        _logger.LogInformation("Phone verification for {Phone}: code={Code}", user.PhoneNumber, code);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(UserEntity user, string token, CancellationToken ct = default)
    {
        _logger.LogInformation("Password reset for {Email}: token={Token}", user.Email, token);
        return Task.CompletedTask;
    }
}
