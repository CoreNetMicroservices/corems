using System.Security.Cryptography;
using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Enums;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Repositories;

namespace CoreMs.UserMs.Core.Services;

[Service]
public class PasswordService(
    UserRepository userRepository,
    ActionTokenRepository actionTokenRepository,
    TokenService tokenService,
    NotificationService notificationService)
{
    private const int BcryptWorkFactor = 12;
    private static readonly TimeSpan TokenExpiration = TimeSpan.FromHours(1);

    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(email, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User not found with email '{email}'");

        await actionTokenRepository.DeleteByUserIdAndActionTypeAsync(user.Id, ActionTokenType.PasswordReset, ct);

        var (rawToken, tokenHash) = GenerateActionToken();
        var actionToken = new ActionTokenEntity
        {
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ActionType = ActionTokenType.PasswordReset,
            ExpiresAt = DateTime.UtcNow.Add(TokenExpiration)
        };
        actionTokenRepository.Add(actionToken);

        await notificationService.SendPasswordResetAsync(user, rawToken, ct);
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword, CancellationToken ct = default)
    {
        if (newPassword != confirmPassword)
            throw ServiceException.Of(UserErrors.PasswordMismatch, "Password confirmation does not match");

        var tokenHash = ComputeSha256Hash(token);
        var actionToken = await actionTokenRepository.GetByTokenHashAsync(tokenHash, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidToken, "Invalid or expired reset token");

        if (actionToken.ActionType != ActionTokenType.PasswordReset)
            throw ServiceException.Of(UserErrors.InvalidToken, "Token type mismatch");

        if (actionToken.Used)
            throw ServiceException.Of(UserErrors.TokenConsumed, "Token already used");

        if (actionToken.ExpiresAt < DateTime.UtcNow)
            throw ServiceException.Of(UserErrors.TokenExpired, "Token has expired");

        var user = await userRepository.GetByEmailAsync(email, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User not found with email '{email}'");

        if (actionToken.UserId != user.Id)
            throw ServiceException.Of(UserErrors.InvalidToken, "Token does not belong to this user");

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor);
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        actionToken.Used = true;
        actionToken.UsedAt = DateTime.UtcNow;
        actionTokenRepository.Update(actionToken);

        await tokenService.RevokeAllUserTokensAsync(user.Id, ct);
    }

    private static (string RawToken, string TokenHash) GenerateActionToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
        var tokenHash = ComputeSha256Hash(rawToken);
        return (rawToken, tokenHash);
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
