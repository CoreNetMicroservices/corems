using CoreMs.Common.Extensions;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using Microsoft.Extensions.Logging;

namespace CoreMs.CommunicationMs.Core.Services;

[Service]
public class SlackService
{
    private readonly MessageDispatcher _dispatcher;
    private readonly ILogger<SlackService> _logger;

    public SlackService(MessageDispatcher dispatcher, ILogger<SlackService> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<NotificationResponse> SendNotificationAsync(SlackNotificationRequest request, CancellationToken ct = default)
    {
        var status = await _dispatcher.DispatchAsync(MessageType.Slack, Guid.NewGuid(), request, ct);
        return new NotificationResponse { Status = status.ToString(), SentAt = DateTime.UtcNow };
    }
}
