using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreMs.Common.Http;

/// <summary>
/// DelegatingHandler that forwards JWT token and correlation ID from the incoming request
/// to outgoing service-to-service HTTP calls.
/// </summary>
public class ServiceAuthDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ServiceAuthDelegatingHandler> _logger;

    public ServiceAuthDelegatingHandler(IHttpContextAccessor httpContextAccessor, ILogger<ServiceAuthDelegatingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // Forward JWT token
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            }

            // Forward correlation ID (create one if not present)
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        _logger.LogDebug("Service call: {Method} {Uri}", request.Method, request.RequestUri);
        return await base.SendAsync(request, cancellationToken);
    }
}
