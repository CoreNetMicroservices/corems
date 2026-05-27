using System.Security.Cryptography;
using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Exceptions;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Services;

public class OAuth2Service : IOAuth2Service
{
    private static readonly TimeSpan AuthCodeExpiration = TimeSpan.FromMinutes(10);

    private readonly IUserRepository _userRepository;
    private readonly ILoginTokenRepository _loginTokenRepository;
    private readonly IAuthorizationCodeRepository _authorizationCodeRepository;
    private readonly ITokenService _tokenService;

    public OAuth2Service(
        IUserRepository userRepository,
        ILoginTokenRepository loginTokenRepository,
        IAuthorizationCodeRepository authorizationCodeRepository,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _loginTokenRepository = loginTokenRepository;
        _authorizationCodeRepository = authorizationCodeRepository;
        _tokenService = tokenService;
    }

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

        var user = await _userRepository.GetByUuidAsync(userUuid, ct)
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
        _authorizationCodeRepository.Add(authCode);

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
        var user = await _userRepository.GetByEmailAsync(username, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidCredentials, "Invalid credentials");

        if (user.Password is null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            throw ServiceException.Of(UserErrors.InvalidCredentials, "Invalid credentials");

        if (!user.EmailVerified)
            throw ServiceException.Of(UserErrors.EmailNotVerified, "Email not verified");

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);

        return await _tokenService.GenerateTokenResponseAsync(user, scope, null, ct);
    }

    public async Task<OAuth2TokenResponse> HandleAuthorizationCodeGrantAsync(
        string code,
        string redirectUri,
        string? codeVerifier,
        string clientId,
        CancellationToken ct = default)
    {
        var authCode = await _authorizationCodeRepository.GetByCodeAsync(code, ct)
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
        _authorizationCodeRepository.Update(authCode);

        return await _tokenService.GenerateTokenResponseAsync(authCode.User, authCode.Scope, authCode.Nonce, ct);
    }

    public async Task<OAuth2TokenResponse> HandleRefreshTokenGrantAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        var loginToken = await _loginTokenRepository.GetByTokenAsync(refreshToken, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidToken, "Invalid refresh token");

        var user = loginToken.User;

        // Token rotation: delete old token, generate new response
        _loginTokenRepository.Remove(loginToken);

        return await _tokenService.GenerateTokenResponseAsync(user, "openid profile email", null, ct);
    }

    public async Task RevokeTokenAsync(string token, string? tokenTypeHint, CancellationToken ct = default)
    {
        var loginToken = await _loginTokenRepository.GetByTokenAsync(token, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidToken, "Invalid token");

        _loginTokenRepository.Remove(loginToken);
    }

    private static string ComputeCodeChallenge(string codeVerifier, string? method)
    {
        if (method == "plain")
            return codeVerifier;

        // Default to S256
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
