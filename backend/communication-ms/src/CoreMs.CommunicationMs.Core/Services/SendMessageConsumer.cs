using System.Text.Json;
using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Entities;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Repositories;
using CoreMs.CommunicationMs.Core.Services.Providers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreMs.CommunicationMs.Core.Services;

/// <summary>
/// MassTransit consumer that picks messages from the queue and sends via the appropriate provider.
/// </summary>
public class SendMessageConsumer : IConsumer<SendMessageCommand>
{
    private readonly IEnumerable<IChannelProvider> _providers;
    private readonly MessageRepository _messageRepository;
    private readonly ILogger<SendMessageConsumer> _logger;

    public SendMessageConsumer(
        IEnumerable<IChannelProvider> providers,
        MessageRepository messageRepository,
        ILogger<SendMessageConsumer> logger)
    {
        _providers = providers;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendMessageCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Processing queued message: {MessageId}, type: {Type}", command.MessageId, command.Type);

        var entity = await _messageRepository.GetByUuidAsync(command.MessageId, context.CancellationToken);

        var provider = _providers.FirstOrDefault(p => p.MessageType == command.Type)
            ?? throw new InvalidOperationException($"No provider for type: {command.Type}");

        try
        {
            object payload = command.Type switch
            {
                MessageType.Email => JsonSerializer.Deserialize<EmailPayloadDto>(command.PayloadJson)!,
                MessageType.Sms => JsonSerializer.Deserialize<SmsPayloadDto>(command.PayloadJson)!,
                MessageType.Slack => JsonSerializer.Deserialize<SlackNotificationRequest>(command.PayloadJson)!,
                _ => throw new InvalidOperationException($"Unknown type: {command.Type}")
            };

            await provider.SendAsync(payload, context.CancellationToken);

            if (entity != null)
            {
                entity.Status = MessageStatus.Sent;
                entity.SentAt = DateTime.UtcNow;
            }

            _logger.LogInformation("Message sent successfully: {MessageId}", command.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message: {MessageId}", command.MessageId);

            if (entity != null)
            {
                entity.Status = MessageStatus.Failed;
                entity.SentAt = DateTime.UtcNow;
            }

            throw; // MassTransit will retry based on retry policy
        }
    }
}
