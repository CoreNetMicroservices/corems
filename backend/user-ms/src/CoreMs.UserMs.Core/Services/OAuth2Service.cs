using System.Security.Cryptography;
using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Models;
using CoreMs.UserMs.Core.Repositories;

namespace CoreMs.UserMs.Core.Services;

[Service]
public class OAuth2Service(
    UserRepository userRepository,
    LoginTokenRepository loginTokenRepository,
    AuthorizationCodeRepository authorizationCodeRepository,
    TokenService tokenService,
    AuthService authService)
{
    private static readonly TimeSpan AuthCodeExpiration = TimeSpan.FromMinutes(10);

    public async Task<string> HandleAuthorizeAsync(
        string responseType,
        string clientId,
        string redirectUri,
        string? scope,
        string? state,
        string? codeChallenge,
        string? codeChallengeMethod,
        string? nonce,
        Guid userUuid,
        CancellationToken ct = default)
    {
        if (responseType != "code")
            throw ServiceException.Of(UserErrors.InvalidRequest, "Only 'code' response_type is supported");

        var user = await userRepository.GetByUuidAsync(userUuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, "User not found");

        var code = Guid.NewGuid().ToString();

        var authCode = new AuthorizationCodeEntity
        {
            Code = code,
            UserId = user.Id,
            User = user,
            ClientId = clientId,
            RedirectUri = redirectUri,
            Scope = scope,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            Nonce = nonce,
            State = state,
            ExpiresAt = DateTime.UtcNow.Add(AuthCodeExpiration),
            IsUsed = false
        };
        authorizationCodeRepository.Add(authCode);

        var redirectUrl = $"{redirectUri}?code={code}";
        if (state is not null)
            redirectUrl += $"&state={state}";

        return redirectUrl;
    }

    public async Task<OAuth2TokenResponse> HandlePasswordGrantAsync(
        string username,
        string password,
        string? scope,
        CancellationToken ct = default)
    {
        var user = await authService.ValidateCredentialsAsync(username, password, ct);
        return await tokenService.GenerateTokenResponseAsync(user, scope, null, ct);
    }

    public async Task<OAuth2TokenResponse> HandleAuthorizationCodeGrantAsync(
        string code,
        string redirectUri,
        string? codeVerifier,
        string clientId,
        CancellationToken ct = default)
    {
        var authCode = await authorizationCodeRepository.GetByCodeAsync(code, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidRequest, "Invalid authorization code");

        if (authCode.IsUsed)
            throw ServiceException.Of(UserErrors.InvalidRequest, "Authorization code already used");

        if (authCode.ExpiresAt < DateTime.UtcNow)
            throw ServiceException.Of(UserErrors.InvalidRequest, "Authorization code expired");

        if (authCode.ClientId != clientId)
            throw ServiceException.Of(UserErrors.InvalidRequest, "client_id mismatch");

        if (authCode.RedirectUri != redirectUri)
            throw ServiceException.Of(UserErrors.InvalidRequest, "redirect_uri mismatch");

        if (authCode.CodeChallenge is not null)
        {
            if (codeVerifier is null)
                throw ServiceException.Of(UserErrors.InvalidRequest, "code_verifier required for PKCE");

            var computedChallenge = ComputeCodeChallenge(codeVerifier, authCode.CodeChallengeMethod);
            if (computedChallenge != authCode.CodeChallenge)
                throw ServiceException.Of(UserErrors.InvalidRequest, "Invalid code_verifier");
        }

        authCode.IsUsed = true;
        authCode.UsedAt = DateTime.UtcNow;
        authorizationCodeRepository.Update(authCode);

        return await tokenService.GenerateTokenResponseAsync(authCode.User, authCode.Scope, authCode.Nonce, ct);
    }

    public async Task<OAuth2TokenResponse> HandleRefreshTokenGrantAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        var loginToken = await loginTokenRepository.GetByTokenAsync(refreshToken, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidToken, "Invalid refresh token");

        var user = loginToken.User;
        loginTokenRepository.Remove(loginToken);

        return await tokenService.GenerateTokenResponseAsync(user, "openid profile email", null, ct);
    }

    public async Task RevokeTokenAsync(string token, string? tokenTypeHint, CancellationToken ct = default)
    {
        var loginToken = await loginTokenRepository.GetByTokenAsync(token, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidToken, "Invalid token");

        loginTokenRepository.Remove(loginToken);
    }

    private static string ComputeCodeChallenge(string codeVerifier, string? method)
    {
        if (method == "plain")
            return codeVerifier;

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public async Task<UserEntity> GetUserForInfoAsync(Guid userUuid, CancellationToken ct = default)
    {
        return await userRepository.GetByUuidAsync(userUuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, "User not found");
    }
}
