using CoreMs.CommunicationMs.Core.Enums;

namespace CoreMs.CommunicationMs.Core.Entities;

public class MessageEntity
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public MessageType Type { get; set; }
    public Guid UserId { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public MessageSenderType? SentByType { get; set; }
    public Guid? SentById { get; set; }
}
