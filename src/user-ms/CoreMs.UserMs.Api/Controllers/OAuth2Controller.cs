using CoreMs.Common.Security;
using CoreMs.UserMs.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

[ApiController]
[Route("oauth2")]
public class OAuth2Controller : ControllerBase
{
    private readonly IOAuth2Service _oauth2Service;
    private readonly ICurrentUserService _currentUserService;

    public OAuth2Controller(IOAuth2Service oauth2Service, ICurrentUserService currentUserService)
    {
        _oauth2Service = oauth2Service;
        _currentUserService = currentUserService;
    }

    [Authorize]
    [HttpGet("authorize")]
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

    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
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

    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Revoke(
        [FromForm(Name = "token")] string token,
        [FromForm(Name = "token_type_hint")] string? tokenTypeHint,
        CancellationToken ct)
    {
        await _oauth2Service.RevokeTokenAsync(token, tokenTypeHint, ct);
        return Ok(new { result = true });
    }
}
