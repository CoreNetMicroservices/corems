using CoreMs.Common.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreMs.Common.Http;

/// <summary>
/// DelegatingHandler that forwards JWT token and correlation ID from the incoming request
/// to outgoing service-to-service HTTP calls.
/// </summary>
public class ServiceAuthDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<ServiceAuthDelegatingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);

            var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext)
                ?? httpContext.TraceIdentifier;
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        logger.LogDebug("Service call: {Method} {Uri}", request.Method, request.RequestUri);
        return await base.SendAsync(request, cancellationToken);
    }
}
