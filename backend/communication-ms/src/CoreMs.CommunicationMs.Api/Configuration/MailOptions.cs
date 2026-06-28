namespace CoreMs.CommunicationMs.Api.Configuration;

public class MailOptions
{
    public const string SectionName = "Mail";

    public bool Enabled { get; set; }
    public string DefaultFrom { get; set; } = "noreply@corems.local";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
