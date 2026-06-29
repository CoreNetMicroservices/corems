using CoreMs.CommunicationMs.Core.Enums;
using CoreMs.CommunicationMs.Core.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

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
    public bool UseSsl { get; set; }
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

    public async Task SendAsync(object payload, CancellationToken ct = default)
    {
        if (payload is not EmailPayloadDto email)
            throw new InvalidOperationException("Invalid payload type for email provider");

        if (!_options.Enabled)
        {
            _logger.LogInformation("Email sending disabled. Simulating send to {Recipient}: {Subject}", email.Recipient, email.Subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(email.SenderName ?? "", email.Sender ?? _options.DefaultFrom));
        message.To.Add(MailboxAddress.Parse(email.Recipient));
        message.Subject = email.Subject;

        if (email.Cc != null)
            foreach (var cc in email.Cc)
                message.Cc.Add(MailboxAddress.Parse(cc));

        if (email.Bcc != null)
            foreach (var bcc in email.Bcc)
                message.Bcc.Add(MailboxAddress.Parse(bcc));

        var isHtml = email.EmailType.Equals("html", StringComparison.OrdinalIgnoreCase);
        message.Body = new TextPart(isHtml ? "html" : "plain") { Text = email.Body ?? "" };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, ct);

        if (!string.IsNullOrEmpty(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Email sent to {Recipient}: {Subject}", email.Recipient, email.Subject);
    }
}
