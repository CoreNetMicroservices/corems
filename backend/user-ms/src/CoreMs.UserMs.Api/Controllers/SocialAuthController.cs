using CoreMs.UserMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

/// <summary>
/// Social OAuth2 login endpoints (Google, GitHub, LinkedIn).
/// </summary>
[ApiController]
[Route("oauth2")]
[AllowAnonymous]
public class SocialAuthController(
    OAuth2ProviderService providerService,
    SocialAuthService socialAuthService,
    TokenService tokenService,
    ILogger<SocialAuthController> logger) : ControllerBase
{
    /// <summary>
    /// Redirect user to the social provider's OAuth2 consent page.
    /// </summary>
    [HttpGet("authorize/{provider}")]
    public IActionResult Authorize(string provider, [FromQuery(Name = "redirect_uri")] string redirectUri)
    {
        if (!providerService.IsSupported(provider))
            return BadRequest(new { error = $"Unsupported provider: {provider}" });

        var callbackUrl = $"{Request.Scheme}://{Request.Host}/oauth2/callback/{provider}";
        var authUrl = providerService.GetAuthorizationUrl(provider, callbackUrl, redirectUri);
        return Redirect(authUrl);
    }

    /// <summary>
    /// Handle OAuth2 callback from the social provider.
    /// </summary>
    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(
        string provider,
        [FromQuery] string code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        var redirectUri = state ?? "http://localhost:8080/welcome";

        try
        {
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/oauth2/callback/{provider}";
            var info = await providerService.ExchangeCodeAsync(provider, code, callbackUrl, ct);

            // HandleSocialLoginAsync persists the user (assigning user.Id);
            // GenerateTokenResponseAsync persists the issued login token.
            var user = await socialAuthService.HandleSocialLoginAsync(provider, info, ct);
            var tokenResponse = await tokenService.GenerateTokenResponseAsync(user, "openid profile email", null, ct);

            var separator = redirectUri.Contains('?') ? "&" : "?";
            return Redirect($"{redirectUri}{separator}refresh_token={tokenResponse.RefreshToken}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Social auth callback failed for provider {Provider}", provider);
            var separator = redirectUri.Contains('?') ? "&" : "?";
            return Redirect($"{redirectUri}{separator}error=social_auth_failed");
        }
    }
}
