using CoreMs.Common.Middleware;
using CoreMs.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreMs.Common.Http;

/// <summary>
/// DelegatingHandler that resolves auth for outgoing service-to-service HTTP calls.
/// 
/// Priority:
/// 1. If HttpContext has a Bearer token → forward it (normal request flow)
/// 2. If ServiceCallContext has an actor identity → mint a fresh short-lived token (queue/background flow)
/// 3. No auth → proceed without Authorization header
/// </summary>
public class ServiceAuthDelegatingHandler(
    IHttpContextAccessor httpContextAccessor,
    ServiceCallContext serviceCallContext,
    TokenProvider tokenProvider,
    ILogger<ServiceAuthDelegatingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // Normal request flow — forward the incoming JWT
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);

            var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext)
                ?? httpContext.TraceIdentifier;
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
        }
        else if (!string.IsNullOrEmpty(serviceCallContext.ActorUserId))
        {
            // Background/queue flow — mint a fresh service token from actor identity
            var token = MintServiceToken(serviceCallContext.ActorUserId, serviceCallContext.ActorRoles);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        }

        logger.LogDebug("Service call: {Method} {Uri}", request.Method, request.RequestUri);
        return await base.SendAsync(request, cancellationToken);
    }

    private string MintServiceToken(string userId, IReadOnlyList<string>? roles)
    {
        var claims = new Dictionary<string, object>();
        if (roles is { Count: > 0 })
            claims["role"] = roles;

        return tokenProvider.CreateCustomToken("service_call", userId, claims, 5);
    }
}
