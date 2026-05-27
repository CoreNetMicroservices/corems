namespace CoreMs.Common.Contracts;

/// <summary>
/// Command published to the message bus to request an SMS be sent via Communication MS.
/// </summary>
public record SendSmsCommand(
    string To,
    string Message);
