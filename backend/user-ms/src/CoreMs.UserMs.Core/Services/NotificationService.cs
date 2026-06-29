using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Client;
using CoreMs.UserMs.Core.Configuration;
using CoreMs.UserMs.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.UserMs.Core.Services;

/// <summary>
/// Sends notifications via communication-ms HTTP client.
/// </summary>
[Service]
public class NotificationService
{
    private readonly CommunicationMsClient _communicationClient;
    private readonly AppOptions _appOptions;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        CommunicationMsClient communicationClient,
        IOptions<AppOptions> appOptions,
        ILogger<NotificationService> logger)
    {
        _communicationClient = communicationClient;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(UserEntity user, string token, CancellationToken ct = default)
    {
        var verifyUrl = $"{_appOptions.FrontendBaseUrl}/verify-email?email={Uri.EscapeDataString(user.Email)}&token={token}";

        var body = $"""
            <h2>Welcome to CoreMS!</h2>
            <p>Please verify your email by clicking the link below:</p>
            <p><a href="{verifyUrl}">Verify Email</a></p>
            <p>Or copy this link: {verifyUrl}</p>
            <p>This link expires in 24 hours.</p>
            """;

        await _communicationClient.SendEmailNotificationAsync(
            user.Email, "Verify your email - CoreMS", body,
            emailType: "html", senderName: "CoreMS", ct: ct);

        _logger.LogInformation("Email verification sent for {Email}", user.Email);
    }

    public async Task SendPhoneVerificationAsync(UserEntity user, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(user.PhoneNumber)) return;

        await _communicationClient.SendSmsNotificationAsync(
            user.PhoneNumber, $"Your CoreMS verification code is: {code}", ct);

        _logger.LogInformation("Phone verification sent for {Phone}", user.PhoneNumber);
    }

    public async Task SendPasswordResetAsync(UserEntity user, string token, CancellationToken ct = default)
    {
        var resetUrl = $"{_appOptions.FrontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={token}";

        var body = $"""
            <h2>Password Reset</h2>
            <p>Click the link below to reset your password:</p>
            <p><a href="{resetUrl}">Reset Password</a></p>
            <p>Or copy this link: {resetUrl}</p>
            <p>This link expires in 24 hours. If you didn't request this, ignore this email.</p>
            """;

        await _communicationClient.SendEmailNotificationAsync(
            user.Email, "Reset your password - CoreMS", body,
            emailType: "html", senderName: "CoreMS", ct: ct);

        _logger.LogInformation("Password reset email sent for {Email}", user.Email);
    }
}
