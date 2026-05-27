using System.Security.Cryptography;
using CoreMs.Common.Exceptions;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Exceptions;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Services;

public class TokenService : ITokenService
{
    private readonly ILoginTokenRepository _loginTokenRepository;
    private readonly IActionTokenRepository _actionTokenRepository;
    private readonly IAuthorizationCodeRepository _authorizationCodeRepository;

    public TokenService(
        ILoginTokenRepository loginTokenRepository,
        IActionTokenRepository actionTokenRepository,
        IAuthorizationCodeRepository authorizationCodeRepository)
    {
        _loginTokenRepository = loginTokenRepository;
        _actionTokenRepository = actionTokenRepository;
        _authorizationCodeRepository = authorizationCodeRepository;
    }

    public Task<OAuth2TokenResponse> GenerateTokenResponseAsync(UserEntity user, string? scope, string? nonce, CancellationToken ct = default)
    {
        var refreshToken = GenerateSecureToken();

        var loginToken = new LoginTokenEntity
        {
            UserId = user.Id,
            Token = refreshToken,
            User = user
        };
        _loginTokenRepository.Add(loginToken);

        var response = new OAuth2TokenResponse
        {
            AccessToken = "placeholder-access-token",
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = 600,
            Scope = scope ?? "openid profile email"
        };

        return Task.FromResult(response);
    }

    public Task<string> CreateRefreshTokenAsync(UserEntity user, CancellationToken ct = default)
    {
        var token = GenerateSecureToken();

        var loginToken = new LoginTokenEntity
        {
            UserId = user.Id,
            Token = token,
            User = user
        };
        _loginTokenRepository.Add(loginToken);

        return Task.FromResult(token);
    }

    public async Task ValidateRefreshTokenAsync(Guid tokenId, Guid userUuid, CancellationToken ct = default)
    {
        var loginToken = await _loginTokenRepository.GetByUuidAsync(tokenId, ct)
            ?? throw ServiceException.Of(UserErrors.TokenNotFound, $"Token not found with ID: {tokenId}");

        if (loginToken.User.Uuid != userUuid)
            throw ServiceException.Of(UserErrors.TokenNotFound, $"Token not found with ID: {tokenId}");
    }

    public async Task RevokeAllUserTokensAsync(long userId, CancellationToken ct = default)
    {
        await _loginTokenRepository.DeleteAllByUserIdAsync(userId, ct);
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken ct = default)
    {
        await _loginTokenRepository.DeleteExpiredAsync(ct);
        await _actionTokenRepository.DeleteExpiredAsync(ct);
        await _authorizationCodeRepository.DeleteExpiredAsync(ct);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
