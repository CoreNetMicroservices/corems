using System.Net.Http.Json;

namespace CoreMs.CommunicationMs.Client;

/// <summary>
/// Typed HTTP client for calling communication-ms endpoints.
/// Registered via: builder.Services.AddCommunicationMsClient(baseUrl);
///
/// Automatically forwards JWT and correlation ID via ServiceAuthDelegatingHandler.
/// </summary>
public class CommunicationMsClient
{
    private readonly HttpClient _http;

    public CommunicationMsClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<HttpResponseMessage> SendEmailNotificationAsync(
        string recipient, string subject, string body,
        string emailType = "html", string? sender = null, string? senderName = null,
        CancellationToken ct = default)
    {
        return await _http.PostAsJsonAsync("/api/notifications/email", new
        {
            subject,
            recipient,
            body,
            emailType,
            sender,
            senderName
        }, ct);
    }

    public async Task<HttpResponseMessage> SendSmsNotificationAsync(
        string phoneNumber, string message,
        CancellationToken ct = default)
    {
        return await _http.PostAsJsonAsync("/api/notifications/sms", new
        {
            phoneNumber,
            message
        }, ct);
    }

    public async Task<HttpResponseMessage> SendEmailMessageAsync(
        Guid userId, string recipient, string subject, string body,
        string emailType = "html", string? sender = null, string? senderName = null,
        CancellationToken ct = default)
    {
        return await _http.PostAsJsonAsync("/api/messages/email", new
        {
            userId,
            subject,
            recipient,
            body,
            emailType,
            sender,
            senderName
        }, ct);
    }

    public async Task<HttpResponseMessage> SendSmsMessageAsync(
        Guid userId, string phoneNumber, string message,
        CancellationToken ct = default)
    {
        return await _http.PostAsJsonAsync("/api/messages/sms", new
        {
            userId,
            phoneNumber,
            message
        }, ct);
    }
}
