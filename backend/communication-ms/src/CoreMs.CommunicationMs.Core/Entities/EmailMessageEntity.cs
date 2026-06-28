using CoreMs.CommunicationMs.Core.Enums;

namespace CoreMs.CommunicationMs.Core.Entities;

public class EmailMessageEntity : MessageEntity
{
    public EmailMessageEntity()
    {
        Type = MessageType.Email;
    }

    public string EmailType { get; set; } = "txt";
    public string Subject { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string? SenderName { get; set; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public ICollection<EmailAttachmentEntity> Attachments { get; set; } = new List<EmailAttachmentEntity>();
}
