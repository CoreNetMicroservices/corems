namespace CoreMs.UserMs.Core.Configuration;

/// <summary>
/// Configuration for JWT token generation in TokenService.
/// Bound from the "Jwt" section in appsettings.json.
/// </summary>
public class TokenServiceOptions
{
    public const string SectionName = "Jwt";

    public string Algorithm { get; set; } = "HS256";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = "corems";
    public string KeyId { get; set; } = "corems-1";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 10;
    public int RefreshTokenExpirationMinutes { get; set; } = 1440;
    public int IdTokenExpirationMinutes { get; set; } = 60;
}
