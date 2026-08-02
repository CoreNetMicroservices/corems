using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Entities;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Exceptions;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Repositories;
using CoreMs.CommunicationMs.Core.Services.Providers;
using CoreMs.TemplateMs.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.CommunicationMs.Core.Services;

[Service]
public class EmailService
{
    private readonly MessageRepository _messageRepository;
    private readonly MessageDispatcher _dispatcher;
    private readonly TemplateMsClient _templateClient;
    private readonly EmailProviderOptions _mailOptions;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        MessageRepository messageRepository,
        MessageDispatcher dispatcher,
        TemplateMsClient templateClient,
        IOptions<EmailProviderOptions> mailOptions,
        ILogger<EmailService> logger)
    {
        _messageRepository = messageRepository;
        _dispatcher = dispatcher;
        _templateClient = templateClient;
        _mailOptions = mailOptions.Value;
        _logger = logger;
    }

    public async Task<MessageResponse> SendMessageAsync(EmailMessageRequest request, Guid? senderUserId, CancellationToken ct = default)
    {
        var body = await ResolveBodyAsync(request.Body, request.Template, ct);

        var entity = new EmailMessageEntity
        {
            EmailType = request.Template != null ? "html" : request.EmailType,
            Subject = request.Subject,
            Sender = request.Sender ?? _mailOptions.DefaultFrom,
            SenderName = request.SenderName,
            Cc = request.Cc != null ? string.Join(",", request.Cc) : null,
            Bcc = request.Bcc != null ? string.Join(",", request.Bcc) : null,
            Recipient = request.Recipient,
            Body = body,
            UserId = request.UserId,
            SentByType = senderUserId != null ? MessageSenderType.User : MessageSenderType.System,
            SentById = senderUserId
        };

        _messageRepository.Add(entity);

        var payload = BuildPayload(request, body);
        var status = await _dispatcher.DispatchAsync(MessageType.Email, entity.Uuid, payload, ct);
        entity.Status = status;
        if (status == MessageStatus.Sent) entity.SentAt = DateTime.UtcNow;

        return MapToResponse(entity, payload);
    }

    public async Task<NotificationResponse> SendNotificationAsync(EmailNotificationRequest request, CancellationToken ct = default)
    {
        var body = await ResolveBodyAsync(request.Body, request.Template, ct);
        var payload = new EmailPayloadDto
        {
            EmailType = request.Template != null ? "html" : request.EmailType,
            Subject = request.Subject,
            Recipient = request.Recipient,
            Sender = request.Sender ?? _mailOptions.DefaultFrom,
            SenderName = request.SenderName,
            Body = body,
            Cc = request.Cc,
            Bcc = request.Bcc,
            DocumentUuids = request.DocumentUuids
        };

        var status = await _dispatcher.DispatchAsync(MessageType.Email, Guid.NewGuid(), payload, ct);
        return new NotificationResponse { Status = status.ToString(), SentAt = DateTime.UtcNow };
    }

    private async Task<string> ResolveBodyAsync(string? body, TemplateRequest? template, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(body)) return body;
        if (template != null)
        {
            var result = await _templateClient.RenderTemplateAsync(
                template.TemplateId,
                template.Params,
                template.Language,
                ct);

            if (result == null)
                throw ServiceException.Of(CommunicationErrors.InvalidRequest,
                    $"Template rendering returned no result for '{template.TemplateId}'");

            return result.RenderedContent;
        }
        throw ServiceException.Of(CommunicationErrors.InvalidRequest, "Either body or template must be provided");
    }

    private EmailPayloadDto BuildPayload(EmailMessageRequest request, string body) => new()
    {
        EmailType = request.Template != null ? "html" : request.EmailType,
        Subject = request.Subject,
        Recipient = request.Recipient,
        Sender = request.Sender ?? _mailOptions.DefaultFrom,
        SenderName = request.SenderName,
        Body = body,
        Cc = request.Cc,
        Bcc = request.Bcc,
        DocumentUuids = request.DocumentUuids
    };

    private static MessageResponse MapToResponse(EmailMessageEntity entity, EmailPayloadDto payload) => new()
    {
        Uuid = entity.Uuid,
        Type = "email",
        Status = entity.Status.ToString().ToLowerInvariant(),
        UserId = entity.UserId,
        CreatedAt = entity.CreatedAt,
        SentById = entity.SentById,
        SentByType = entity.SentByType?.ToString().ToLowerInvariant(),
        Payload = payload
    };
}
