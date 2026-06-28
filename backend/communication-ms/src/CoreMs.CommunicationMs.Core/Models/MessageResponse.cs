namespace CoreMs.CommunicationMs.Core.Models;

public record MessageResponse
{
    public Guid Uuid { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid? SentById { get; init; }
    public string? SentByType { get; init; }
    public object? Payload { get; init; }
}

public record EmailPayloadDto
{
    public string EmailType { get; init; } = "txt";
    public string Subject { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public string? Sender { get; init; }
    public string? SenderName { get; init; }
    public string? Body { get; init; }
    public List<string>? Cc { get; init; }
    public List<string>? Bcc { get; init; }
    public List<Guid>? DocumentUuids { get; init; }
}

public record SmsPayloadDto
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Message { get; init; }
}

public record NotificationResponse
{
    public string Status { get; init; } = string.Empty;
    public DateTime? SentAt { get; init; }
}
