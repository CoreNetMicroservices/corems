using CoreMs.Common.Security;
using CoreMs.UserMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

/// <summary>
/// OAuth2/OIDC endpoints for authorization, token exchange, and revocation.
/// </summary>
[ApiController]
[Route("oauth2")]
public class OAuth2Controller : ControllerBase
{
    private readonly OAuth2Service _oauth2Service;
    private readonly ICurrentUserService _currentUserService;

    public OAuth2Controller(OAuth2Service oauth2Service, ICurrentUserService currentUserService)
    {
        _oauth2Service = oauth2Service;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Authorization endpoint. Validates client and issues an authorization code.
    /// Requires an authenticated user (JWT bearer).
    /// </summary>
    [Authorize]
    [HttpGet("authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Authorize(
        [FromQuery(Name = "response_type")] string responseType,
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery(Name = "code_challenge")] string? codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod,
        [FromQuery] string? nonce,
        CancellationToken ct)
    {
        var userUuid = _currentUserService.GetCurrentUserUuid();
        var redirectUrl = await _oauth2Service.HandleAuthorizeAsync(
            responseType, clientId, redirectUri, scope, state,
            codeChallenge, codeChallengeMethod, nonce, userUuid, ct);

        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Token endpoint. Supports authorization_code, password, and refresh_token grant types.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Token(
        [FromForm(Name = "grant_type")] string grantType,
        [FromForm(Name = "username")] string? username,
        [FromForm(Name = "password")] string? password,
        [FromForm(Name = "code")] string? code,
        [FromForm(Name = "redirect_uri")] string? redirectUri,
        [FromForm(Name = "code_verifier")] string? codeVerifier,
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "refresh_token")] string? refreshToken,
        [FromForm(Name = "scope")] string? scope,
        CancellationToken ct)
    {
        var response = grantType switch
        {
            "password" => await _oauth2Service.HandlePasswordGrantAsync(username!, password!, scope, ct),
            "authorization_code" => await _oauth2Service.HandleAuthorizationCodeGrantAsync(code!, redirectUri!, codeVerifier, clientId!, ct),
            "refresh_token" => await _oauth2Service.HandleRefreshTokenGrantAsync(refreshToken!, ct),
            _ => throw new ArgumentException($"Unsupported grant_type: {grantType}")
        };

        return Ok(response);
    }

    /// <summary>
    /// Token revocation endpoint (RFC 7009). Always returns 200 OK.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Revoke(
        [FromForm(Name = "token")] string token,
        [FromForm(Name = "token_type_hint")] string? tokenTypeHint,
        CancellationToken ct)
    {
        try
        {
            await _oauth2Service.RevokeTokenAsync(token, tokenTypeHint, ct);
        }
        catch
        {
            // RFC 7009: revocation endpoint always returns 200, even if token is invalid
        }

        return Ok();
    }
}
