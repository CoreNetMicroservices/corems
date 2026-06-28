using CoreMs.Common.Contracts.Messaging;
using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using CoreMs.CommunicationMs.Core.Services.Providers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreMs.CommunicationMs.Core.Services;

/// <summary>
/// Consumes notification commands published by other services (e.g., user-ms sends email verification).
/// These are fire-and-forget notifications — not stored in DB.
/// </summary>
public class NotificationCommandConsumer :
    IConsumer<SendEmailNotificationCommand>,
    IConsumer<SendSmsNotificationCommand>
{
    private readonly IEnumerable<IChannelProvider> _providers;
    private readonly ILogger<NotificationCommandConsumer> _logger;

    public NotificationCommandConsumer(
        IEnumerable<IChannelProvider> providers,
        ILogger<NotificationCommandConsumer> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendEmailNotificationCommand> context)
    {
        var cmd = context.Message;
        _logger.LogInformation("Processing email notification to {Recipient}: {Subject}", cmd.Recipient, cmd.Subject);

        var provider = _providers.First(p => p.MessageType == MessageType.Email);
        var payload = new EmailPayloadDto
        {
            Recipient = cmd.Recipient,
            Subject = cmd.Subject,
            Body = cmd.Body,
            EmailType = cmd.EmailType,
            Sender = cmd.Sender,
            SenderName = cmd.SenderName
        };

        await provider.SendAsync(payload, context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<SendSmsNotificationCommand> context)
    {
        var cmd = context.Message;
        _logger.LogInformation("Processing SMS notification to {PhoneNumber}", cmd.PhoneNumber);

        var provider = _providers.First(p => p.MessageType == MessageType.Sms);
        var payload = new SmsPayloadDto
        {
            PhoneNumber = cmd.PhoneNumber,
            Message = cmd.Message
        };

        await provider.SendAsync(payload, context.CancellationToken);
    }
}
