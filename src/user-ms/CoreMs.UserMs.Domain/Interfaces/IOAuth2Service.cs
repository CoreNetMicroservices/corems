using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Interfaces;

/// <summary>
/// Handles OAuth2 authorization, token exchange (password, auth code, refresh), and revocation.
/// </summary>
public interface IOAuth2Service
{
    Task<string> HandleAuthorizeAsync(
        string responseType,
        string clientId,
        string redirectUri,
        string? scope,
        string? state,
        string? codeChallenge,
        string? codeChallengeMethod,
        string? nonce,
        Guid userUuid,
        CancellationToken ct = default);

    Task<OAuth2TokenResponse> HandlePasswordGrantAsync(
        string username,
        string password,
        string? scope,
        CancellationToken ct = default);

    Task<OAuth2TokenResponse> HandleAuthorizationCodeGrantAsync(
        string code,
        string redirectUri,
        string? codeVerifier,
        string clientId,
        CancellationToken ct = default);

    Task<OAuth2TokenResponse> HandleRefreshTokenGrantAsync(
        string refreshToken,
        CancellationToken ct = default);

    Task RevokeTokenAsync(string token, string? tokenTypeHint, CancellationToken ct = default);
}
