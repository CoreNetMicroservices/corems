using System.Net.Mime;
using CoreMs.Common.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace CoreMs.Common.Security;

/// <summary>
/// Configures JWT Bearer events to return consistent JSON error responses
/// for 401 (Challenge) and 403 (Forbidden) — equivalent to Spring Security's
/// AuthenticationEntryPoint and AccessDeniedHandler.
/// </summary>
public static class JwtBearerEventsHandler
{
    public static JwtBearerEvents Create() => new()
    {
        OnChallenge = async context =>
        {
            // Suppress the default WWW-Authenticate response
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = MediaTypeNames.Application.Json;

            var error = Error.Of(DefaultErrors.Unauthorized.ErrorCode, DefaultErrors.Unauthorized.Description);
            await context.Response.WriteAsJsonAsync(ErrorResponse.Of(error));
        },

        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = MediaTypeNames.Application.Json;

            var error = Error.Of(DefaultErrors.Forbidden.ErrorCode, DefaultErrors.Forbidden.Description);
            await context.Response.WriteAsJsonAsync(ErrorResponse.Of(error));
        }
    };
}
