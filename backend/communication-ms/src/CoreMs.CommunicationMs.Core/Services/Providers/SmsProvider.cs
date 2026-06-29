using System.Net.Http.Headers;
using System.Text;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsProvider> _logger;

    public SmsProvider(IOptions<SmsProviderOptions> options, IHttpClientFactory httpClientFactory, ILogger<SmsProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public MessageType MessageType => MessageType.Sms;

    public async Task SendAsync(object payload, CancellationToken ct = default)
    {
        if (payload is not SmsPayloadDto sms)
            throw new InvalidOperationException("Invalid payload type for SMS provider");

        if (!_options.Enabled)
        {
            _logger.LogInformation("SMS sending disabled. Simulating send to {PhoneNumber}: {Message}", sms.PhoneNumber, sms.Message);
            return;
        }

        var http = _httpClientFactory.CreateClient();
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json";

        var authBytes = Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = sms.PhoneNumber,
            ["From"] = _options.FromNumber,
            ["Body"] = sms.Message ?? ""
        });

        var response = await http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("SMS sent to {PhoneNumber} via Twilio", sms.PhoneNumber);
    }
}
