using System.Text.Json;
using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Configuration;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Services.Providers;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.CommunicationMs.Core.Services;

/// <summary>
/// Central dispatch logic: sends messages directly or enqueues via RabbitMQ.
/// Captures actor identity at enqueue time for downstream service calls.
/// </summary>
[Service]
public class MessageDispatcher(
    IEnumerable<IChannelProvider> providers,
    IPublishEndpoint publishEndpoint,
    IHttpContextAccessor httpContextAccessor,
    IOptions<QueueOptions> queueOptions,
    ILogger<MessageDispatcher> logger)
{
    private readonly QueueOptions _queueOptions = queueOptions.Value;

    public async Task<MessageStatus> DispatchAsync(MessageType type, Guid messageId, object payload, CancellationToken ct = default)
    {
        if (_queueOptions.Enabled)
        {
            var (userId, roles) = CaptureActorIdentity();

            var command = new SendMessageCommand
            {
                MessageId = messageId,
                Type = type,
                PayloadJson = JsonSerializer.Serialize(payload),
                ActorUserId = userId,
                ActorRoles = roles
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

    private (string? UserId, string? Roles) CaptureActorIdentity()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return (null, null);

        var userId = httpContext.User.FindFirst("sub")?.Value;
        var roles = httpContext.User.FindAll("role").Select(c => c.Value);
        return (userId, string.Join(",", roles));
    }
}
