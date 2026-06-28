using CoreMs.Common.Contracts.Messaging;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Configuration;
using CoreMs.UserMs.Core.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.UserMs.Core.Services;

/// <summary>
/// Publishes notification commands to the message bus.
/// Communication-ms consumes them and sends the actual emails/SMS.
/// </summary>
[Service]
public class NotificationService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly AppOptions _appOptions;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IPublishEndpoint publishEndpoint,
        IOptions<AppOptions> appOptions,
        ILogger<NotificationService> logger)
    {
        _publishEndpoint = publishEndpoint;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(UserEntity user, string token, CancellationToken ct = default)
    {
        var verifyUrl = $"{_appOptions.FrontendBaseUrl}/verify-email?email={Uri.EscapeDataString(user.Email)}&token={token}";

        await _publishEndpoint.Publish(new SendEmailNotificationCommand
        {
            Recipient = user.Email,
            Subject = "Verify your email - CoreMS",
            Body = $"""
                <h2>Welcome to CoreMS!</h2>
                <p>Please verify your email by clicking the link below:</p>
                <p><a href="{verifyUrl}">Verify Email</a></p>
                <p>Or copy this link: {verifyUrl}</p>
                <p>This link expires in 24 hours.</p>
                """,
            EmailType = "html",
            SenderName = "CoreMS"
        }, ct);

        _logger.LogInformation("Email verification notification published for {Email}", user.Email);
    }

    public async Task SendPhoneVerificationAsync(UserEntity user, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(user.PhoneNumber)) return;

        await _publishEndpoint.Publish(new SendSmsNotificationCommand
        {
            PhoneNumber = user.PhoneNumber,
            Message = $"Your CoreMS verification code is: {code}"
        }, ct);

        _logger.LogInformation("Phone verification notification published for {Phone}", user.PhoneNumber);
    }

    public async Task SendPasswordResetAsync(UserEntity user, string token, CancellationToken ct = default)
    {
        var resetUrl = $"{_appOptions.FrontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={token}";

        await _publishEndpoint.Publish(new SendEmailNotificationCommand
        {
            Recipient = user.Email,
            Subject = "Reset your password - CoreMS",
            Body = $"""
                <h2>Password Reset</h2>
                <p>Click the link below to reset your password:</p>
                <p><a href="{resetUrl}">Reset Password</a></p>
                <p>Or copy this link: {resetUrl}</p>
                <p>This link expires in 24 hours. If you didn't request this, ignore this email.</p>
                """,
            EmailType = "html",
            SenderName = "CoreMS"
        }, ct);

        _logger.LogInformation("Password reset notification published for {Email}", user.Email);
    }
}
