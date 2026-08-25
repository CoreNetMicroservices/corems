using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Entities;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Exceptions;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Repositories;
using CoreMs.TemplateMs.Client;
using Microsoft.Extensions.Logging;

namespace CoreMs.CommunicationMs.Core.Services;

[Service]
public class SmsService
{
    private readonly MessageRepository _messageRepository;
    private readonly MessageDispatcher _dispatcher;
    private readonly TemplateMsClient _templateClient;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        MessageRepository messageRepository,
        MessageDispatcher dispatcher,
        TemplateMsClient templateClient,
        ILogger<SmsService> logger)
    {
        _messageRepository = messageRepository;
        _dispatcher = dispatcher;
        _templateClient = templateClient;
        _logger = logger;
    }

    public async Task<MessageResponse> SendMessageAsync(SmsMessageRequest request, Guid? senderUserId, CancellationToken ct = default)
    {
        var message = await ResolveMessageAsync(request.Message, request.Template, ct);

        var entity = new SmsMessageEntity
        {
            PhoneNumber = request.PhoneNumber,
            Message = message,
            UserId = request.UserId,
            SentByType = senderUserId != null ? MessageSenderType.User : MessageSenderType.System,
            SentById = senderUserId
        };

        _messageRepository.Add(entity);

        var payload = new SmsPayloadDto { PhoneNumber = request.PhoneNumber, Message = message };
        var status = await _dispatcher.DispatchAsync(MessageType.Sms, entity.Uuid, payload, ct);
        entity.Status = status;
        if (status == MessageStatus.Sent) entity.SentAt = DateTime.UtcNow;

        await _messageRepository.SaveChangesAsync(ct);

        return new MessageResponse
        {
            Uuid = entity.Uuid,
            Type = "sms",
            Status = entity.Status.ToString().ToLowerInvariant(),
            UserId = entity.UserId,
            CreatedAt = entity.CreatedAt,
            SentById = entity.SentById,
            SentByType = entity.SentByType?.ToString().ToLowerInvariant(),
            Payload = payload
        };
    }

    public async Task<NotificationResponse> SendNotificationAsync(SmsNotificationRequest request, CancellationToken ct = default)
    {
        var message = await ResolveMessageAsync(request.Message, request.Template, ct);
        var payload = new SmsPayloadDto { PhoneNumber = request.PhoneNumber, Message = message };
        var status = await _dispatcher.DispatchAsync(MessageType.Sms, Guid.NewGuid(), payload, ct);
        return new NotificationResponse { Status = status.ToString(), SentAt = DateTime.UtcNow };
    }

    private async Task<string> ResolveMessageAsync(string? message, TemplateRequest? template, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(message)) return message;
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
        throw ServiceException.Of(CommunicationErrors.InvalidRequest, "Either message or template must be provided");
    }
}
