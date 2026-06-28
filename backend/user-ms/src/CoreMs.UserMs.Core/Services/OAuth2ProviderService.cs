using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Models;
using Microsoft.Extensions.Options;

namespace CoreMs.UserMs.Core.Services;

/// <summary>
/// Configuration for OAuth2 providers, injected from appsettings.
/// </summary>
public class OAuth2ProviderOptions
{
    public const string SectionName = "SocialAuth";

    public ProviderConfig? Google { get; set; }
    public ProviderConfig? GitHub { get; set; }
    public ProviderConfig? LinkedIn { get; set; }

    public ProviderConfig GetProvider(string name) => name.ToLowerInvariant() switch
    {
        "google" => Google ?? throw new InvalidOperationException("Google not configured"),
        "github" => GitHub ?? throw new InvalidOperationException("GitHub not configured"),
        "linkedin" => LinkedIn ?? throw new InvalidOperationException("LinkedIn not configured"),
        _ => throw ServiceException.Of(UserErrors.InvalidRequest, $"Unsupported provider: {name}")
    };
}

public class ProviderConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// Handles OAuth2 code exchange and user info retrieval for all social providers.
/// </summary>
[Service]
public class OAuth2ProviderService
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "google", "github", "linkedin"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OAuth2ProviderOptions _options;

    public OAuth2ProviderService(IHttpClientFactory httpClientFactory, IOptions<OAuth2ProviderOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public bool IsSupported(string provider) => SupportedProviders.Contains(provider);

    /// <summary>
    /// Build the redirect URL to the provider's consent page.
    /// </summary>
    public string GetAuthorizationUrl(string provider, string callbackUrl, string state)
    {
        var config = _options.GetProvider(provider);

        return provider.ToLowerInvariant() switch
        {
            "google" => "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={config.ClientId}" +
                $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
                "&response_type=code" +
                "&scope=openid+email+profile" +
                $"&state={Uri.EscapeDataString(state)}",

            "github" => "https://github.com/login/oauth/authorize" +
                $"?client_id={config.ClientId}" +
                $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
                "&scope=read:user+user:email" +
                $"&state={Uri.EscapeDataString(state)}",

            "linkedin" => "https://www.linkedin.com/oauth/v2/authorization" +
                $"?client_id={config.ClientId}" +
                $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
                "&response_type=code" +
                "&scope=openid+profile+email" +
                $"&state={Uri.EscapeDataString(state)}",

            _ => throw ServiceException.Of(UserErrors.InvalidRequest, $"Unsupported provider: {provider}")
        };
    }

    /// <summary>
    /// Exchange an authorization code for user info from the provider.
    /// </summary>
    public async Task<ExternalLoginInfo> ExchangeCodeAsync(string provider, string code, string callbackUrl, CancellationToken ct = default)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => await ExchangeGoogleAsync(code, callbackUrl, ct),
            "github" => await ExchangeGitHubAsync(code, ct),
            "linkedin" => await ExchangeLinkedInAsync(code, callbackUrl, ct),
            _ => throw ServiceException.Of(UserErrors.InvalidRequest, $"Unsupported provider: {provider}")
        };
    }

    private async Task<ExternalLoginInfo> ExchangeGoogleAsync(string code, string callbackUrl, CancellationToken ct)
    {
        var config = _options.Google!;
        var http = _httpClientFactory.CreateClient();

        var tokenResponse = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret,
                ["redirect_uri"] = callbackUrl,
                ["grant_type"] = "authorization_code"
            }), ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokens = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(ct);

        var userInfoResponse = await http.GetAsync(
            $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={tokens!.AccessToken}", ct);
        userInfoResponse.EnsureSuccessStatusCode();
        var user = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUser>(ct);

        return new ExternalLoginInfo(user!.Email, user.GivenName, user.FamilyName, user.Picture, user.Id);
    }

    private async Task<ExternalLoginInfo> ExchangeGitHubAsync(string code, CancellationToken ct)
    {
        var config = _options.GitHub!;
        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var tokenResponse = await http.PostAsync("https://github.com/login/oauth/access_token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret
            }), ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokens = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(ct);

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CoreMS");

        var userResponse = await http.GetAsync("https://api.github.com/user", ct);
        userResponse.EnsureSuccessStatusCode();
        var user = await userResponse.Content.ReadFromJsonAsync<GitHubUser>(ct);

        var email = user!.Email;
        if (string.IsNullOrEmpty(email))
        {
            var emailsResponse = await http.GetAsync("https://api.github.com/user/emails", ct);
            emailsResponse.EnsureSuccessStatusCode();
            var emails = await emailsResponse.Content.ReadFromJsonAsync<List<GitHubEmailEntry>>(ct);
            email = emails?.FirstOrDefault(e => e.Primary)?.Email ?? emails?.FirstOrDefault()?.Email;
        }

        return new ExternalLoginInfo(
            email ?? throw new InvalidOperationException("GitHub did not provide an email"),
            user.Name?.Split(' ').FirstOrDefault(),
            user.Name?.Split(' ').Skip(1).FirstOrDefault(),
            user.AvatarUrl,
            user.Id.ToString());
    }

    private async Task<ExternalLoginInfo> ExchangeLinkedInAsync(string code, string callbackUrl, CancellationToken ct)
    {
        var config = _options.LinkedIn!;
        var http = _httpClientFactory.CreateClient();

        var tokenResponse = await http.PostAsync("https://www.linkedin.com/oauth/v2/accessToken",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret,
                ["redirect_uri"] = callbackUrl,
                ["grant_type"] = "authorization_code"
            }), ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokens = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(ct);

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var userInfoResponse = await http.GetAsync("https://api.linkedin.com/v2/userinfo", ct);
        userInfoResponse.EnsureSuccessStatusCode();
        var user = await userInfoResponse.Content.ReadFromJsonAsync<LinkedInUser>(ct);

        return new ExternalLoginInfo(user!.Email, user.GivenName, user.FamilyName, user.Picture, user.Sub);
    }

    // Shared token response (all providers return access_token)
    private record TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
    }

    private record GoogleUser
    {
        [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
        [JsonPropertyName("email")] public string Email { get; init; } = string.Empty;
        [JsonPropertyName("given_name")] public string? GivenName { get; init; }
        [JsonPropertyName("family_name")] public string? FamilyName { get; init; }
        [JsonPropertyName("picture")] public string? Picture { get; init; }
    }

    private record GitHubUser
    {
        [JsonPropertyName("id")] public long Id { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("email")] public string? Email { get; init; }
        [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; init; }
    }

    private record GitHubEmailEntry
    {
        [JsonPropertyName("email")] public string Email { get; init; } = string.Empty;
        [JsonPropertyName("primary")] public bool Primary { get; init; }
    }

    private record LinkedInUser
    {
        [JsonPropertyName("sub")] public string Sub { get; init; } = string.Empty;
        [JsonPropertyName("email")] public string Email { get; init; } = string.Empty;
        [JsonPropertyName("given_name")] public string? GivenName { get; init; }
        [JsonPropertyName("family_name")] public string? FamilyName { get; init; }
        [JsonPropertyName("picture")] public string? Picture { get; init; }
    }
}
