namespace CoreMs.UserMs.Api.Configuration;

public class OAuth2ClientOptions
{
    public const string SectionName = "OAuth2Clients";

    public List<ClientRegistration> Clients { get; set; } = new();
}

public class ClientRegistration
{
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public List<string> RedirectUris { get; set; } = new();
    public List<string> AllowedScopes { get; set; } = new();
    public List<string> AllowedGrantTypes { get; set; } = new();
    public bool RequirePkce { get; set; } = true;
}
