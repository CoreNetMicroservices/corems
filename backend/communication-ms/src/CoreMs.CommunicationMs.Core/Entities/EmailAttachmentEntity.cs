namespace CoreMs.CommunicationMs.Core.Entities;

public class EmailAttachmentEntity
{
    public long Id { get; set; }
    public long EmailMessageId { get; set; }
    public Guid DocumentUuid { get; set; }
    public string? Checksum { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public EmailMessageEntity EmailMessage { get; set; } = null!;
}
