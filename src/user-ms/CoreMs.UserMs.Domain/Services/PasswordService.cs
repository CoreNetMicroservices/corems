using System.Security.Cryptography;
using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Enums;
using CoreMs.UserMs.Domain.Exceptions;
using CoreMs.UserMs.Domain.Interfaces;

namespace CoreMs.UserMs.Domain.Services;

public class PasswordService : IPasswordService
{
    private const int BcryptWorkFactor = 12;
    private static readonly TimeSpan TokenExpiration = TimeSpan.FromHours(1);

    private readonly IUserRepository _userRepository;
    private readonly IActionTokenRepository _actionTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly INotificationService _notificationService;

    public PasswordService(
        IUserRepository userRepository,
        IActionTokenRepository actionTokenRepository,
        ITokenService tokenService,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _actionTokenRepository = actionTokenRepository;
        _tokenService = tokenService;
        _notificationService = notificationService;
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User not found with email '{email}'");

        await _actionTokenRepository.DeleteByUserIdAndActionTypeAsync(user.Id, ActionTokenType.PasswordReset, ct);

        var (rawToken, tokenHash) = GenerateActionToken();
        var actionToken = new ActionTokenEntity
        {
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ActionType = ActionTokenType.PasswordReset,
            ExpiresAt = DateTime.UtcNow.Add(TokenExpiration)
        };
        _actionTokenRepository.Add(actionToken);

        await _notificationService.SendPasswordResetAsync(user, rawToken, ct);
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword, CancellationToken ct = default)
    {
        if (newPassword != confirmPassword)
            throw ServiceException.Of(UserErrors.PasswordMismatch, "Password confirmation does not match");

        var tokenHash = ComputeSha256Hash(token);
        var actionToken = await _actionTokenRepository.GetByTokenHashAsync(tokenHash, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidToken, "Invalid or expired reset token");

        if (actionToken.ActionType != ActionTokenType.PasswordReset)
            throw ServiceException.Of(UserErrors.InvalidToken, "Token type mismatch");

        if (actionToken.Used)
            throw ServiceException.Of(UserErrors.TokenConsumed, "Token already used");

        if (actionToken.ExpiresAt < DateTime.UtcNow)
            throw ServiceException.Of(UserErrors.TokenExpired, "Token has expired");

        var user = await _userRepository.GetByEmailAsync(email, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User not found with email '{email}'");

        if (actionToken.UserId != user.Id)
            throw ServiceException.Of(UserErrors.InvalidToken, "Token does not belong to this user");

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor);
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);

        actionToken.Used = true;
        actionToken.UsedAt = DateTime.UtcNow;
        _actionTokenRepository.Update(actionToken);

        await _tokenService.RevokeAllUserTokensAsync(user.Id, ct);
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
