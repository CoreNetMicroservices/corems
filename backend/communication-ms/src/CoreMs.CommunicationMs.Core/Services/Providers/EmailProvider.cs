using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreMs.CommunicationMs.Core.Services.Providers;

public class EmailProviderOptions
{
    public const string SectionName = "Mail";

    public bool Enabled { get; set; }
    public string DefaultFrom { get; set; } = "noreply@corems.local";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class EmailProvider : IChannelProvider
{
    private readonly EmailProviderOptions _options;
    private readonly ILogger<EmailProvider> _logger;

    public EmailProvider(IOptions<EmailProviderOptions> options, ILogger<EmailProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public MessageType MessageType => MessageType.Email;

    public Task SendAsync(object payload, CancellationToken ct = default)
    {
        if (payload is not EmailPayloadDto email)
            throw new InvalidOperationException("Invalid payload type for email provider");

        if (!_options.Enabled)
        {
            _logger.LogInformation("Email sending disabled. Simulating send to {Recipient}: {Subject}", email.Recipient, email.Subject);
            return Task.CompletedTask;
        }

        // TODO: integrate with MailKit or System.Net.Mail for real sending
        _logger.LogInformation("Sending email to {Recipient}: {Subject}", email.Recipient, email.Subject);
        return Task.CompletedTask;
    }
}
