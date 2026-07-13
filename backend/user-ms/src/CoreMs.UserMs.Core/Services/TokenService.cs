using System.Security.Cryptography;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.Common.Security;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Models;
using CoreMs.UserMs.Core.Repositories;
using Microsoft.Extensions.Options;

namespace CoreMs.UserMs.Core.Services;

[Service]
public class TokenService
{
    private readonly LoginTokenRepository _loginTokenRepository;
    private readonly ActionTokenRepository _actionTokenRepository;
    private readonly AuthorizationCodeRepository _authorizationCodeRepository;
    private readonly TokenProviderOptions _options;
    private readonly TokenProvider _tokenProvider;

    public TokenService(
        LoginTokenRepository loginTokenRepository,
        ActionTokenRepository actionTokenRepository,
        AuthorizationCodeRepository authorizationCodeRepository,
        IOptions<TokenProviderOptions> options,
        TokenProvider tokenProvider)
    {
        _loginTokenRepository = loginTokenRepository;
        _actionTokenRepository = actionTokenRepository;
        _authorizationCodeRepository = authorizationCodeRepository;
        _options = options.Value;
        _tokenProvider = tokenProvider;
    }

    public Task<OAuth2TokenResponse> GenerateTokenResponseAsync(UserEntity user, string? scope, string? nonce, CancellationToken ct = default)
    {
        var scopes = scope ?? "openid profile email";

        var accessToken = GenerateAccessToken(user, scopes);
        var refreshToken = CreateAndPersistRefreshToken(user);

        string? idToken = null;
        if (scopes.Contains("openid"))
            idToken = GenerateIdToken(user, nonce);

        var response = new OAuth2TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IdToken = idToken,
            TokenType = "Bearer",
            ExpiresIn = _options.AccessTokenExpirationMinutes * 60,
            Scope = scopes
        };

        return Task.FromResult(response);
    }

    public Task<string> CreateRefreshTokenAsync(UserEntity user, CancellationToken ct = default)
    {
        var token = CreateAndPersistRefreshToken(user);
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

    private string GenerateAccessToken(UserEntity user, string scopes)
    {
        var roles = user.Roles.Select(r => r.Name).ToList();
        var claims = new Dictionary<string, object>
        {
            [TokenProvider.ClaimEmail] = user.Email,
            ["scope"] = scopes,
            ["role"] = roles
        };

        return _tokenProvider.CreateAccessToken(user.Uuid.ToString(), claims);
    }

    private string GenerateIdToken(UserEntity user, string? nonce)
    {
        var claims = new Dictionary<string, object>
        {
            [TokenProvider.ClaimEmail] = user.Email,
            ["email_verified"] = user.EmailVerified.ToString().ToLowerInvariant()
        };

        if (user.FirstName is not null)
            claims["given_name"] = user.FirstName;

        if (user.LastName is not null)
            claims["family_name"] = user.LastName;

        if (nonce is not null)
            claims["nonce"] = nonce;

        return _tokenProvider.CreateIdToken(user.Uuid.ToString(), claims);
    }

    private string CreateAndPersistRefreshToken(UserEntity user)
    {
        var roles = user.Roles.Select(r => r.Name).ToList();
        var claims = new Dictionary<string, object>
        {
            [TokenProvider.ClaimEmail] = user.Email,
            [TokenProvider.ClaimFirstName] = user.FirstName ?? "",
            [TokenProvider.ClaimLastName] = user.LastName ?? "",
            [TokenProvider.ClaimRoles] = string.Join(",", roles)
        };

        var token = _tokenProvider.CreateRefreshToken(user.Uuid.ToString(), claims);

        var loginToken = new LoginTokenEntity
        {
            UserId = user.Id,
            Token = token,
            User = user
        };
        _loginTokenRepository.Add(loginToken);

        return token;
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
