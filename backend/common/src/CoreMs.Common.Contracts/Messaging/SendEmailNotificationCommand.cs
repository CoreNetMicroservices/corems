namespace CoreMs.Common.Contracts.Messaging;

/// <summary>
/// Command to send an email notification via communication-ms.
/// Published by any service, consumed by communication-ms.
/// </summary>
public record SendEmailNotificationCommand
{
    public required string Recipient { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public string EmailType { get; init; } = "html";
    public string? Sender { get; init; }
    public string? SenderName { get; init; }
}
