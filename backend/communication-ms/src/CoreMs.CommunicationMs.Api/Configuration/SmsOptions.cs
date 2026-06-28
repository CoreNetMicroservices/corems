namespace CoreMs.CommunicationMs.Api.Configuration;

public class SmsOptions
{
    public const string SectionName = "Sms";

    public bool Enabled { get; set; }
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}
