namespace CoreMs.UserMs.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Algorithm { get; set; } = "RS256";
    public string Issuer { get; set; } = "http://localhost:5100";
    public string Audience { get; set; } = "corems";
    public string SecretKey { get; set; } = string.Empty;
    public string? PublicKeyPath { get; set; }
    public string? PrivateKeyPath { get; set; }
    public int AccessTokenExpirationMinutes { get; set; } = 10;
    public int RefreshTokenExpirationMinutes { get; set; } = 1440;
    public int IdTokenExpirationMinutes { get; set; } = 60;
    public int AuthorizationCodeExpirationMinutes { get; set; } = 10;
}
