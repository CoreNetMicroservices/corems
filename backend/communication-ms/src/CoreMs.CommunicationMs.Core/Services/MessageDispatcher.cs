using System.Text.Json;
using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Configuration;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Services.Providers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.CommunicationMs.Core.Services;

/// <summary>
/// Central dispatch logic: sends messages directly or enqueues via RabbitMQ.
/// </summary>
[Service]
public class MessageDispatcher(
    IEnumerable<IChannelProvider> providers,
    IPublishEndpoint publishEndpoint,
    IOptions<QueueOptions> queueOptions,
    ILogger<MessageDispatcher> logger)
{
    private readonly QueueOptions _queueOptions = queueOptions.Value;

    public async Task<MessageStatus> DispatchAsync(MessageType type, Guid messageId, object payload, CancellationToken ct = default)
    {
        if (_queueOptions.Enabled)
        {
            var command = new SendMessageCommand
            {
                MessageId = messageId,
                Type = type,
                PayloadJson = JsonSerializer.Serialize(payload)
            };

            await publishEndpoint.Publish(command, ct);
            logger.LogInformation("Message enqueued: messageId={MessageId}, type={Type}", messageId, type);
            return MessageStatus.Enqueued;
        }

        var provider = providers.FirstOrDefault(p => p.MessageType == type)
            ?? throw new InvalidOperationException($"No provider registered for message type: {type}");

        try
        {
            await provider.SendAsync(payload, ct);
            logger.LogInformation("Message sent directly: messageId={MessageId}, type={Type}", messageId, type);
            return MessageStatus.Sent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message: messageId={MessageId}, type={Type}", messageId, type);
            return MessageStatus.Failed;
        }
    }
}
