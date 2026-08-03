namespace CoreMs.Common.Http;

/// <summary>
/// Scoped context holding actor identity for service-to-service calls.
/// In HTTP request flows, identity comes from HttpContext (JWT forwarding via DelegatingHandler).
/// In background flows (queue consumers, background services), set explicitly via SetActor().
/// </summary>
public class ServiceCallContext
{
    /// <summary>
    /// Actor user ID for minting a fresh service token (background/queue flows).
    /// </summary>
    public string? ActorUserId { get; private set; }

    /// <summary>
    /// Actor roles for minting a fresh service token (background/queue flows).
    /// </summary>
    public IReadOnlyList<string>? ActorRoles { get; private set; }

    /// <summary>
    /// Set the actor identity explicitly (for queue consumers, background jobs).
    /// Downstream clients use this to mint a fresh token for service-to-service calls.
    /// </summary>
    public void SetActor(string userId, IReadOnlyList<string>? roles = null)
    {
        ActorUserId = userId;
        ActorRoles = roles;
    }

    public bool HasIdentity => !string.IsNullOrEmpty(ActorUserId);
}
