using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackProvider> _logger;

    public SlackProvider(IOptions<SlackProviderOptions> options, IHttpClientFactory httpClientFactory, ILogger<SlackProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public MessageType MessageType => MessageType.Slack;

    public async Task SendAsync(object payload, CancellationToken ct = default)
    {
        if (payload is not SlackNotificationRequest slack)
            throw new InvalidOperationException("Invalid payload type for Slack provider");

        if (!_options.Enabled)
        {
            _logger.LogInformation("Slack sending disabled. Simulating send to {Channel}: {Message}", slack.Channel, slack.Message);
            return;
        }

        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        var response = await http.PostAsJsonAsync("https://slack.com/api/chat.postMessage", new
        {
            channel = slack.Channel,
            text = slack.Message
        }, ct);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SlackApiResponse>(ct);
        if (result is { Ok: false })
        {
            _logger.LogError("Slack API error: {Error}", result.Error);
            throw new InvalidOperationException($"Slack API error: {result.Error}");
        }

        _logger.LogInformation("Slack message sent to {Channel}", slack.Channel);
    }

    private record SlackApiResponse
    {
        public bool Ok { get; init; }
        public string? Error { get; init; }
    }
}
