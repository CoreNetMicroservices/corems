using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Client;
using CoreMs.UserMs.Core.Configuration;
using CoreMs.UserMs.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.UserMs.Core.Services;

/// <summary>
/// Sends notifications via communication-ms. Uses templates when configured, falls back to inline content.
/// </summary>
[Service]
public class NotificationService
{
    private readonly CommunicationMsClient _communicationClient;
    private readonly AppOptions _appOptions;
    private readonly NotificationTemplateOptions _templateOptions;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        CommunicationMsClient communicationClient,
        IOptions<AppOptions> appOptions,
        IOptions<NotificationTemplateOptions> templateOptions,
        ILogger<NotificationService> logger)
    {
        _communicationClient = communicationClient;
        _appOptions = appOptions.Value;
        _templateOptions = templateOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(UserEntity user, string token, CancellationToken ct = default)
    {
        var verifyUrl = $"{_appOptions.FrontendBaseUrl}/verify-email?email={Uri.EscapeDataString(user.Email)}&token={token}";
        var expirationHours = (_appOptions.VerificationEmailExpirationMinutes / 60).ToString();
        var templateId = _templateOptions.Email.EmailVerification;

        if (!string.IsNullOrEmpty(templateId))
        {
            var template = new TemplatePayload
            {
                TemplateId = templateId,
                Params = new Dictionary<string, object>
                {
                    ["firstName"] = user.FirstName ?? "there",
                    ["verificationUrl"] = verifyUrl,
                    ["expirationHours"] = expirationHours,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                }
            };

            await _communicationClient.SendEmailNotificationAsync(
                user.Email, "Verify your email - CoreMS",
                template: template, senderName: "CoreMS", ct: ct);
        }
        else
        {
            var body = $"""
                <h2>Welcome to CoreMS!</h2>
                <p>Please verify your email by clicking the link below:</p>
                <p><a href="{verifyUrl}">Verify Email</a></p>
                <p>Or copy this link: {verifyUrl}</p>
                <p>This link expires in {expirationHours} hours.</p>
                """;

            await _communicationClient.SendEmailNotificationAsync(
                user.Email, "Verify your email - CoreMS", body,
                emailType: "html", senderName: "CoreMS", ct: ct);
        }

        _logger.LogInformation("Email verification sent for {Email}", user.Email);
    }

    public async Task SendPhoneVerificationAsync(UserEntity user, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(user.PhoneNumber)) return;

        var templateId = _templateOptions.Sms.VerificationCode;

        if (!string.IsNullOrEmpty(templateId))
        {
            var template = new TemplatePayload
            {
                TemplateId = templateId,
                Params = new Dictionary<string, object>
                {
                    ["code"] = code,
                    ["expirationMinutes"] = (_appOptions.VerificationEmailExpirationMinutes).ToString()
                }
            };

            await _communicationClient.SendSmsNotificationAsync(
                user.PhoneNumber, template: template, ct: ct);
        }
        else
        {
            await _communicationClient.SendSmsNotificationAsync(
                user.PhoneNumber, $"Your CoreMS verification code is: {code}", ct: ct);
        }

        _logger.LogInformation("Phone verification sent for {Phone}", user.PhoneNumber);
    }

    public async Task SendPasswordResetAsync(UserEntity user, string token, CancellationToken ct = default)
    {
        var resetUrl = $"{_appOptions.FrontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={token}";
        var expirationHours = (_appOptions.PasswordResetExpirationMinutes / 60).ToString();
        var templateId = _templateOptions.Email.PasswordReset;

        if (!string.IsNullOrEmpty(templateId))
        {
            var template = new TemplatePayload
            {
                TemplateId = templateId,
                Params = new Dictionary<string, object>
                {
                    ["firstName"] = user.FirstName ?? "there",
                    ["resetUrl"] = resetUrl,
                    ["expirationHours"] = expirationHours,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                }
            };

            await _communicationClient.SendEmailNotificationAsync(
                user.Email, "Reset your password - CoreMS",
                template: template, senderName: "CoreMS", ct: ct);
        }
        else
        {
            var body = $"""
                <h2>Password Reset</h2>
                <p>Click the link below to reset your password:</p>
                <p><a href="{resetUrl}">Reset Password</a></p>
                <p>Or copy this link: {resetUrl}</p>
                <p>This link expires in {expirationHours} hours. If you didn't request this, ignore this email.</p>
                """;

            await _communicationClient.SendEmailNotificationAsync(
                user.Email, "Reset your password - CoreMS", body,
                emailType: "html", senderName: "CoreMS", ct: ct);
        }

        _logger.LogInformation("Password reset email sent for {Email}", user.Email);
    }

    public async Task SendPasswordChangedAsync(UserEntity user, CancellationToken ct = default)
    {
        var resetUrl = $"{_appOptions.FrontendBaseUrl}/forgot-password";
        var templateId = _templateOptions.Email.PasswordChanged;

        if (!string.IsNullOrEmpty(templateId))
        {
            var template = new TemplatePayload
            {
                TemplateId = templateId,
                Params = new Dictionary<string, object>
                {
                    ["firstName"] = user.FirstName ?? "there",
                    ["changedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"),
                    ["resetUrl"] = resetUrl,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                }
            };

            await _communicationClient.SendEmailNotificationAsync(
                user.Email, "Your password was changed - CoreMS",
                template: template, senderName: "CoreMS Security", ct: ct);
        }
        else
        {
            var body = $"""
                <h2>Password Changed</h2>
                <p>Your password was successfully changed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.</p>
                <p>If you didn't make this change, <a href="{resetUrl}">reset your password immediately</a>.</p>
                """;

            await _communicationClient.SendEmailNotificationAsync(
                user.Email, "Your password was changed - CoreMS", body,
                emailType: "html", senderName: "CoreMS Security", ct: ct);
        }

        _logger.LogInformation("Password changed notification sent for {Email}", user.Email);
    }
}
