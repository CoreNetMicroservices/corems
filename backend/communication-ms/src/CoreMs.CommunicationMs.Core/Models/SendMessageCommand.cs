using CoreMs.CommunicationMs.Core.Enums;

namespace CoreMs.CommunicationMs.Core.Models;

/// <summary>
/// MassTransit message contract for dispatching messages via RabbitMQ.
/// </summary>
public record SendMessageCommand
{
    public Guid MessageId { get; init; }
    public MessageType Type { get; init; }
    public string PayloadJson { get; init; } = string.Empty;
    /// <summary>
    /// User ID of the actor who initiated the message (for service-to-service auth when processing from queue).
    /// </summary>
    public string? ActorUserId { get; init; }
    /// <summary>
    /// Roles of the actor (comma-separated) for generating a service token at processing time.
    /// </summary>
    public string? ActorRoles { get; init; }
}
