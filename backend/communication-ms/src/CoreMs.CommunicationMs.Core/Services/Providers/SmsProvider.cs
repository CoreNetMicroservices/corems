using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.CommunicationMs.Core.Services.Providers;

public class SmsProviderOptions
{
    public const string SectionName = "Sms";

    public bool Enabled { get; set; }
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

public class SmsProvider : IChannelProvider
{
    private readonly SmsProviderOptions _options;
    private readonly ILogger<SmsProvider> _logger;

    public SmsProvider(IOptions<SmsProviderOptions> options, ILogger<SmsProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public MessageType MessageType => MessageType.Sms;

    public Task SendAsync(object payload, CancellationToken ct = default)
    {
        if (payload is not SmsPayloadDto sms)
            throw new InvalidOperationException("Invalid payload type for SMS provider");

        if (!_options.Enabled)
        {
            _logger.LogInformation("SMS sending disabled. Simulating send to {PhoneNumber}: {Message}", sms.PhoneNumber, sms.Message);
            return Task.CompletedTask;
        }

        // TODO: integrate with Twilio SDK for real sending
        _logger.LogInformation("Sending SMS to {PhoneNumber}", sms.PhoneNumber);
        return Task.CompletedTask;
    }
}
