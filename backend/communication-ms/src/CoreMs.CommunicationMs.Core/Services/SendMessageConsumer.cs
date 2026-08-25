using System.Text.Json;
using CoreMs.Common.Http;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Repositories;
using CoreMs.CommunicationMs.Core.Services.Providers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreMs.CommunicationMs.Core.Services;

/// <summary>
/// MassTransit consumer that picks messages from the queue and sends via the appropriate provider.
/// Sets ServiceCallContext with the actor identity so downstream HTTP calls are authenticated.
/// </summary>
public class SendMessageConsumer(
    IEnumerable<IChannelProvider> providers,
    MessageRepository messageRepository,
    ServiceCallContext serviceCallContext,
    ILogger<SendMessageConsumer> logger) : IConsumer<SendMessageCommand>
{
    public async Task Consume(ConsumeContext<SendMessageCommand> context)
    {
        var command = context.Message;
        logger.LogInformation("Processing queued message: {MessageId}, type: {Type}", command.MessageId, command.Type);

        if (!string.IsNullOrEmpty(command.ActorUserId))
        {
            var roles = command.ActorRoles?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            serviceCallContext.SetActor(command.ActorUserId, roles);
        }

        var entity = await messageRepository.GetByUuidAsync(command.MessageId, context.CancellationToken);

        var provider = providers.FirstOrDefault(p => p.MessageType == command.Type)
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
                await messageRepository.SaveChangesAsync(context.CancellationToken);
            }

            logger.LogInformation("Message sent successfully: {MessageId}", command.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message: {MessageId}", command.MessageId);

            if (entity != null)
            {
                entity.Status = MessageStatus.Failed;
                entity.SentAt = DateTime.UtcNow;
                await messageRepository.SaveChangesAsync(context.CancellationToken);
            }

            throw;
        }
    }
}
