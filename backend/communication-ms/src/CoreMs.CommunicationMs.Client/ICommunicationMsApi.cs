using System.Net.Http.Json;
using CoreMs.Common.Contracts.Messaging;

namespace CoreMs.CommunicationMs.Client;

/// <summary>
/// Typed HTTP client for calling communication-ms endpoints.
/// Registered via: builder.Services.AddCoreMsHttpClient&lt;ICommunicationMsApi&gt;(baseUrl);
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

    public async Task<HttpResponseMessage> SendEmailNotificationAsync(SendEmailNotificationCommand command, CancellationToken ct = default)
    {
        return await _http.PostAsJsonAsync("/api/notifications/email", new
        {
            subject = command.Subject,
            recipient = command.Recipient,
            body = command.Body,
            emailType = command.EmailType,
            sender = command.Sender,
            senderName = command.SenderName
        }, ct);
    }

    public async Task<HttpResponseMessage> SendSmsNotificationAsync(SendSmsNotificationCommand command, CancellationToken ct = default)
    {
        return await _http.PostAsJsonAsync("/api/notifications/sms", new
        {
            phoneNumber = command.PhoneNumber,
            message = command.Message
        }, ct);
    }
}
