using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Services.Providers;
using Microsoft.Extensions.Logging;

namespace CoreMs.CommunicationMs.Core.Services;

/// <summary>
/// Central dispatch logic: sends messages directly via the appropriate channel provider.
/// When RabbitMQ is integrated, this will optionally enqueue instead of sending directly.
/// </summary>
[Service]
public class MessageDispatcher
{
    private readonly IEnumerable<IChannelProvider> _providers;
    private readonly ILogger<MessageDispatcher> _logger;

    public MessageDispatcher(IEnumerable<IChannelProvider> providers, ILogger<MessageDispatcher> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<MessageStatus> DispatchAsync(MessageType type, Guid messageId, object payload, CancellationToken ct = default)
    {
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
