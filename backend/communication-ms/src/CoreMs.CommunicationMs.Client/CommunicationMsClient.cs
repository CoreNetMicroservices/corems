using System.Net.Http.Json;

namespace CoreMs.CommunicationMs.Client;

/// <summary>
/// Typed HTTP client for calling communication-ms endpoints.
/// Automatically forwards JWT and correlation ID via ServiceAuthDelegatingHandler.
/// </summary>
public class CommunicationMsClient(HttpClient http)
{
    public async Task<HttpResponseMessage> SendEmailNotificationAsync(
        string recipient, string subject, string? body = null,
        string emailType = "html", string? sender = null, string? senderName = null,
        TemplatePayload? template = null,
        CancellationToken ct = default)
    {
        return await http.PostAsJsonAsync("/api/notifications/email", new
        {
            subject,
            recipient,
            body,
            emailType,
            sender,
            senderName,
            template
        }, ct);
    }

    public async Task<HttpResponseMessage> SendSmsNotificationAsync(
        string phoneNumber, string? message = null,
        TemplatePayload? template = null,
        CancellationToken ct = default)
    {
        return await http.PostAsJsonAsync("/api/notifications/sms", new
        {
            phoneNumber,
            message,
            template
        }, ct);
    }

    public async Task<HttpResponseMessage> SendEmailMessageAsync(
        Guid userId, string recipient, string subject, string? body = null,
        string emailType = "html", string? sender = null, string? senderName = null,
        TemplatePayload? template = null,
        CancellationToken ct = default)
    {
        return await http.PostAsJsonAsync("/api/messages/email", new
        {
            userId,
            subject,
            recipient,
            body,
            emailType,
            sender,
            senderName,
            template
        }, ct);
    }

    public async Task<HttpResponseMessage> SendSmsMessageAsync(
        Guid userId, string phoneNumber, string? message = null,
        TemplatePayload? template = null,
        CancellationToken ct = default)
    {
        return await http.PostAsJsonAsync("/api/messages/sms", new
        {
            userId,
            phoneNumber,
            message,
            template
        }, ct);
    }
}

/// <summary>
/// Template reference payload for communication-ms.
/// </summary>
public record TemplatePayload
{
    public required string TemplateId { get; init; }
    public Dictionary<string, object>? Params { get; init; }
    public string? Language { get; init; }
}
