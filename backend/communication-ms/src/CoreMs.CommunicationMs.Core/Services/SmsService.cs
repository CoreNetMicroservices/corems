using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Entities;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Exceptions;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreMs.CommunicationMs.Core.Services;

[Service]
public class SmsService
{
    private readonly MessageRepository _messageRepository;
    private readonly MessageDispatcher _dispatcher;
    private readonly ILogger<SmsService> _logger;

    public SmsService(MessageRepository messageRepository, MessageDispatcher dispatcher, ILogger<SmsService> logger)
    {
        _messageRepository = messageRepository;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<MessageResponse> SendMessageAsync(SmsMessageRequest request, Guid? senderUserId, CancellationToken ct = default)
    {
        var message = ResolveMessage(request.Message, request.Template);

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
        var message = ResolveMessage(request.Message, request.Template);
        var payload = new SmsPayloadDto { PhoneNumber = request.PhoneNumber, Message = message };
        var status = await _dispatcher.DispatchAsync(MessageType.Sms, Guid.NewGuid(), payload, ct);
        return new NotificationResponse { Status = status.ToString(), SentAt = DateTime.UtcNow };
    }

    private static string ResolveMessage(string? message, TemplateRequest? template)
    {
        if (!string.IsNullOrWhiteSpace(message)) return message;
        if (template != null)
        {
            // TODO: call template-ms rendering API
            return $"[Template: {template.TemplateId}]";
        }
        throw ServiceException.Of(CommunicationErrors.InvalidRequest, "Either message or template must be provided");
    }
}
