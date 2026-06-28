namespace CoreMs.CommunicationMs.Api.Configuration;

public class SlackOptions
{
    public const string SectionName = "Slack";

    public bool Enabled { get; set; }
    public string Token { get; set; } = string.Empty;
    public string SenderApp { get; set; } = "CoreMS";
}
