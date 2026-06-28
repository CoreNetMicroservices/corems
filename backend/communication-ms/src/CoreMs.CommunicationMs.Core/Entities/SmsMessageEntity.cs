using CoreMs.CommunicationMs.Core.Enums;

namespace CoreMs.CommunicationMs.Core.Entities;

public class SmsMessageEntity : MessageEntity
{
    public SmsMessageEntity()
    {
        Type = MessageType.Sms;
    }

    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Sid { get; set; }
}
