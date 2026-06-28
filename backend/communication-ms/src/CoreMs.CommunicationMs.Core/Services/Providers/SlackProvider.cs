using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.CommunicationMs.Core.Services.Providers;

public class SlackProviderOptions
{
    public const string SectionName = "Slack";

    public bool Enabled { get; set; }
    public string Token { get; set; } = string.Empty;
    public string SenderApp { get; set; } = "CoreMS";
}

public class SlackProvider : IChannelProvider
{
    private readonly SlackProviderOptions _options;
    private readonly ILogger<SlackProvider> _logger;

    public SlackProvider(IOptions<SlackProviderOptions> options, ILogger<SlackProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public MessageType MessageType => MessageType.Slack;

    public Task SendAsync(object payload, CancellationToken ct = default)
    {
        if (payload is not SlackNotificationRequest slack)
            throw new InvalidOperationException("Invalid payload type for Slack provider");

        if (!_options.Enabled)
        {
            _logger.LogInformation("Slack sending disabled. Simulating send to {Channel}: {Message}", slack.Channel, slack.Message);
            return Task.CompletedTask;
        }

        // TODO: integrate with Slack SDK for real sending
        _logger.LogInformation("Sending Slack message to {Channel}", slack.Channel);
        return Task.CompletedTask;
    }
}
