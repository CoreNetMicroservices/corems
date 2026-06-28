using CoreMs.Common.Exceptions;

namespace CoreMs.CommunicationMs.Core.Exceptions;

public static class CommunicationErrors
{
    public static readonly ErrorInfo InvalidRequest = new("communication.invalid_request", 400, "Invalid request");
    public static readonly ErrorInfo MessageNotFound = new("communication.message_not_found", 404, "Message not found");
    public static readonly ErrorInfo SendFailed = new("communication.send_failed", 500, "Failed to send message");
}
