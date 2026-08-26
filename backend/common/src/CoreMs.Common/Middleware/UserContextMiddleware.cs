using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreMs.Common.Middleware;

/// <summary>
/// Pushes authenticated-user identifiers into the log scope so every log event within a request
/// carries them. Only opaque identifiers are logged (UserId from "sub", TokenId from "jti") —
/// never email, roles, or the raw token. Unauthenticated requests add nothing.
///
/// Must run after UseAuthentication() so HttpContext.User is populated.
/// </summary>
public class UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var properties = new Dictionary<string, object>();

        var userId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
            properties["UserId"] = userId;

        var tokenId = user.FindFirstValue("jti");
        if (!string.IsNullOrEmpty(tokenId))
            properties["TokenId"] = tokenId;

        if (properties.Count == 0)
        {
            await next(context);
            return;
        }

        using (logger.BeginScope(properties))
        {
            await next(context);
        }
    }
}
