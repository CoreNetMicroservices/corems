using CoreMs.Common.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CoreMs.Common.Middleware;

public static class StatusCodePagesExtensions
{
    /// <summary>
    /// Adds JSON error responses for 404 (no route matched).
    /// Equivalent to Spring's NoHandlerFoundException handling.
    /// </summary>
    public static IApplicationBuilder UseCoreMsStatusCodePages(this IApplicationBuilder app)
    {
        return app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            if (response.ContentType is not null)
                return;

            var errorInfo = response.StatusCode switch
            {
                404 => DefaultErrors.NotFound,
                405 => DefaultErrors.InvalidRequest,
                _ => null
            };

            if (errorInfo is null)
                return;

            response.ContentType = "application/json";
            var error = Error.Of(errorInfo.ErrorCode, errorInfo.Description);
            await response.WriteAsJsonAsync(ErrorResponse.Of(error));
        });
    }
}
