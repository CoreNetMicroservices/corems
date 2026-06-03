using CoreMs.UserMs.Api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CoreMs.UserMs.Api.Controllers;

/// <summary>
/// OpenID Connect Discovery and JWKS endpoints.
/// </summary>
[ApiController]
[AllowAnonymous]
public class DiscoveryController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;

    public DiscoveryController(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>
    /// OpenID Connect Discovery document (RFC 8414).
    /// </summary>
    [HttpGet("~/.well-known/openid-configuration")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfiguration()
    {
        var issuer = _jwtOptions.Issuer.TrimEnd('/');

        var discovery = new
        {
            issuer,
            authorization_endpoint = $"{issuer}/oauth2/authorize",
            token_endpoint = $"{issuer}/oauth2/token",
            revocation_endpoint = $"{issuer}/oauth2/revoke",
            userinfo_endpoint = $"{issuer}/oauth2/userinfo",
            jwks_uri = $"{issuer}/.well-known/jwks.json",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "password", "refresh_token" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { _jwtOptions.Algorithm },
            token_endpoint_auth_methods_supported = new[] { "none", "client_secret_post" },
            scopes_supported = new[] { "openid", "profile", "email" },
            claims_supported = new[] { "sub", "email", "name", "given_name", "family_name", "roles" },
            code_challenge_methods_supported = new[] { "S256", "plain" },
            revocation_endpoint_auth_methods_supported = new[] { "none" }
        };

        return Ok(discovery);
    }

    /// <summary>
    /// JSON Web Key Set (JWKS) endpoint. Returns empty keys for HS256 (symmetric key not exposed).
    /// </summary>
    [HttpGet("~/.well-known/jwks.json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetJwks()
    {
        // HS256 uses a symmetric key which should not be exposed publicly.
        // Return an empty key set — clients validate tokens via the token endpoint or introspection.
        var jwks = new
        {
            keys = Array.Empty<object>()
        };

        return Ok(jwks);
    }
}
