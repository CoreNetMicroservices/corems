namespace CoreMs.UserMs.Api.Configuration;

public class OAuth2ClientOptions
{
    public const string SectionName = "OAuth2";

    public string ClientId { get; set; } = "corems-web";
    public string ClientSecret { get; set; } = string.Empty;
    public List<string> AllowedRedirectUris { get; set; } = new();
}
