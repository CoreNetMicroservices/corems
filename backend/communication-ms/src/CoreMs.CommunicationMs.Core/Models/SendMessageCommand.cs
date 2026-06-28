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
}
