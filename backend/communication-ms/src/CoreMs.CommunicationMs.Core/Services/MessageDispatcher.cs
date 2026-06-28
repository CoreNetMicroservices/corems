using System.Text.Json;
using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Services.Providers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.CommunicationMs.Core.Services;

public class QueueOptions
{
    public const string SectionName = "Queue";
    public bool Enabled { get; set; }
}

/// <summary>
/// Central dispatch logic: sends messages directly or enqueues via RabbitMQ.
/// </summary>
[Service]
public class MessageDispatcher
{
    private readonly IEnumerable<IChannelProvider> _providers;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly QueueOptions _queueOptions;
    private readonly ILogger<MessageDispatcher> _logger;

    public MessageDispatcher(
        IEnumerable<IChannelProvider> providers,
        IPublishEndpoint publishEndpoint,
        IOptions<QueueOptions> queueOptions,
        ILogger<MessageDispatcher> logger)
    {
        _providers = providers;
        _publishEndpoint = publishEndpoint;
        _queueOptions = queueOptions.Value;
        _logger = logger;
    }

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

            await _publishEndpoint.Publish(command, ct);
            _logger.LogInformation("Message enqueued: messageId={MessageId}, type={Type}", messageId, type);
            return MessageStatus.Enqueued;
        }

        // Direct send
        var provider = _providers.FirstOrDefault(p => p.MessageType == type)
            ?? throw new InvalidOperationException($"No provider registered for message type: {type}");

        try
        {
            await provider.SendAsync(payload, ct);
            _logger.LogInformation("Message sent directly: messageId={MessageId}, type={Type}", messageId, type);
            return MessageStatus.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message: messageId={MessageId}, type={Type}", messageId, type);
            return MessageStatus.Failed;
        }
    }
}
