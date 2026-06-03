namespace CoreMs.UserMs.Api.Configuration;

public class SocialAuthOptions
{
    public const string SectionName = "SocialAuth";

    public GoogleOptions? Google { get; set; }
    public GitHubOptions? GitHub { get; set; }
    public LinkedInOptions? LinkedIn { get; set; }
}

public class GoogleOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class GitHubOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class LinkedInOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
