namespace CoreMs.Common.Contracts.Messaging;

/// <summary>
/// Command to send an SMS notification via communication-ms.
/// Published by any service, consumed by communication-ms.
/// </summary>
public record SendSmsNotificationCommand
{
    public required string PhoneNumber { get; init; }
    public required string Message { get; init; }
}
