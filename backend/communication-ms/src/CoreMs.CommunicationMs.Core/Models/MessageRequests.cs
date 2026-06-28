namespace CoreMs.CommunicationMs.Core.Models;

public record EmailMessageRequest
{
    public required Guid UserId { get; init; }
    public string EmailType { get; init; } = "txt";
    public required string Subject { get; init; }
    public required string Recipient { get; init; }
    public string? Sender { get; init; }
    public string? SenderName { get; init; }
    public string? Body { get; init; }
    public List<string>? Cc { get; init; }
    public List<string>? Bcc { get; init; }
    public List<Guid>? DocumentUuids { get; init; }
    public TemplateRequest? Template { get; init; }
}

public record SmsMessageRequest
{
    public required Guid UserId { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Message { get; init; }
    public TemplateRequest? Template { get; init; }
}

public record EmailNotificationRequest
{
    public string Level { get; init; } = "info";
    public string EmailType { get; init; } = "txt";
    public required string Subject { get; init; }
    public required string Recipient { get; init; }
    public string? Sender { get; init; }
    public string? SenderName { get; init; }
    public string? Body { get; init; }
    public List<string>? Cc { get; init; }
    public List<string>? Bcc { get; init; }
    public List<Guid>? DocumentUuids { get; init; }
    public TemplateRequest? Template { get; init; }
}

public record SmsNotificationRequest
{
    public string Level { get; init; } = "info";
    public required string PhoneNumber { get; init; }
    public string? Message { get; init; }
    public TemplateRequest? Template { get; init; }
}

public record SlackNotificationRequest
{
    public string Level { get; init; } = "info";
    public required string Channel { get; init; }
    public required string Message { get; init; }
}

public record TemplateRequest
{
    public required string TemplateId { get; init; }
    public Dictionary<string, object>? Params { get; init; }
    public string? Language { get; init; }
}
