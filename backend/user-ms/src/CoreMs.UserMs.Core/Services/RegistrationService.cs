using System.Security.Cryptography;
using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Enums;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Models;
using CoreMs.UserMs.Core.Repositories;

namespace CoreMs.UserMs.Core.Services;

[Service]
public class RegistrationService(
    UserRepository userRepository,
    ActionTokenRepository actionTokenRepository,
    NotificationService notificationService)
{
    private const int BcryptWorkFactor = 12;
    private static readonly TimeSpan TokenExpiration = TimeSpan.FromHours(24);

    public async Task<UserEntity> SignUpAsync(SignUpRequest request, CancellationToken ct = default)
    {
        if (request.Password != request.ConfirmPassword)
            throw ServiceException.Of(UserErrors.PasswordMismatch, "Password confirmation does not match");

        if (await userRepository.ExistsByEmailAsync(request.Email, ct))
            throw ServiceException.Of(UserErrors.UserExists, "User already exists");

        if (request.PhoneNumber is not null &&
            await userRepository.ExistsByPhoneNumberAsync(request.PhoneNumber, ct))
            throw ServiceException.Of(UserErrors.UserExists, "Phone number already in use");

        var user = new UserEntity
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            ImageUrl = request.ImageUrl,
            Provider = "local",
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password, BcryptWorkFactor),
            EmailVerified = false,
            PhoneVerified = request.PhoneNumber is not null ? false : null
        };

        user.Roles.Add(new UserRoleEntity { Name = "USER_MS_USER" });
        userRepository.Add(user);

        var (rawToken, tokenHash) = GenerateActionToken();
        var actionToken = new ActionTokenEntity
        {
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ActionType = ActionTokenType.EmailVerification,
            ExpiresAt = DateTime.UtcNow.Add(TokenExpiration)
        };
        actionTokenRepository.Add(actionToken);

        await notificationService.SendEmailVerificationAsync(user, rawToken, ct);

        if (user.PhoneNumber is not null)
        {
            var (phoneCode, phoneHash) = GenerateActionToken();
            var phoneToken = new ActionTokenEntity
            {
                UserId = user.Id,
                User = user,
                TokenHash = phoneHash,
                ActionType = ActionTokenType.PhoneVerification,
                ExpiresAt = DateTime.UtcNow.Add(TokenExpiration)
            };
            actionTokenRepository.Add(phoneToken);

            await notificationService.SendPhoneVerificationAsync(user, phoneCode, ct);
        }

        return user;
    }

    public async Task VerifyEmailAsync(string email, string token, CancellationToken ct = default)
    {
        var tokenHash = ComputeSha256Hash(token);
        var actionToken = await actionTokenRepository.GetByTokenHashAsync(tokenHash, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidToken, "Invalid or expired verification token");

        ValidateActionToken(actionToken, ActionTokenType.EmailVerification);

        var user = await userRepository.GetByEmailAsync(email, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User not found with email '{email}'");

        if (actionToken.UserId != user.Id)
            throw ServiceException.Of(UserErrors.InvalidToken, "Token does not belong to this user");

        user.EmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        actionToken.Used = true;
        actionToken.UsedAt = DateTime.UtcNow;
        actionTokenRepository.Update(actionToken);
    }

    public async Task VerifyPhoneAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        var tokenHash = ComputeSha256Hash(code);
        var actionToken = await actionTokenRepository.GetByTokenHashAsync(tokenHash, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidToken, "Invalid or expired verification code");

        ValidateActionToken(actionToken, ActionTokenType.PhoneVerification);

        var user = await userRepository.GetByPhoneNumberAsync(phoneNumber, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User not found with phone '{phoneNumber}'");

        if (actionToken.UserId != user.Id)
            throw ServiceException.Of(UserErrors.InvalidToken, "Token does not belong to this user");

        user.PhoneVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        actionToken.Used = true;
        actionToken.UsedAt = DateTime.UtcNow;
        actionTokenRepository.Update(actionToken);
    }

    public async Task ResendVerificationAsync(string email, string type, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(email, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User not found with email '{email}'");

        var actionType = type.ToUpperInvariant() switch
        {
            "EMAIL" => ActionTokenType.EmailVerification,
            "PHONE" => ActionTokenType.PhoneVerification,
            _ => throw ServiceException.Of(UserErrors.InvalidRequest, $"Invalid verification type: {type}")
        };

        await actionTokenRepository.DeleteByUserIdAndActionTypeAsync(user.Id, actionType, ct);

        var (rawToken, tokenHash) = GenerateActionToken();
        var actionToken = new ActionTokenEntity
        {
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ActionType = actionType,
            ExpiresAt = DateTime.UtcNow.Add(TokenExpiration)
        };
        actionTokenRepository.Add(actionToken);

        if (actionType == ActionTokenType.EmailVerification)
            await notificationService.SendEmailVerificationAsync(user, rawToken, ct);
        else
            await notificationService.SendPhoneVerificationAsync(user, rawToken, ct);
    }

    private static void ValidateActionToken(ActionTokenEntity token, ActionTokenType expectedType)
    {
        if (token.ActionType != expectedType)
            throw ServiceException.Of(UserErrors.InvalidToken, "Token type mismatch");

        if (token.Used)
            throw ServiceException.Of(UserErrors.TokenConsumed, "Token already used");

        if (token.ExpiresAt < DateTime.UtcNow)
            throw ServiceException.Of(UserErrors.TokenExpired, "Token has expired");
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
