using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreMs.Common.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID.
/// If X-Correlation-Id header is present, it's used. Otherwise a new GUID is generated.
/// The correlation ID is stored in HttpContext.Items and added to the response headers.
/// All log scopes within the request include the CorrelationId property.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(HeaderName, correlationId);
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    /// <summary>
    /// Gets the correlation ID from the current HttpContext. Returns null if not available.
    /// </summary>
    public static string? GetCorrelationId(HttpContext? context)
        => context?.Items[ItemKey] as string;
}
