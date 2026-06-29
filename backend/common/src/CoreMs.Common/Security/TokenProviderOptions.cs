namespace CoreMs.Common.Security;

public class TokenProviderOptions
{
    public const string SectionName = "TokenProvider";

    public SigningAlgorithm Algorithm { get; set; } = SigningAlgorithm.HS256;

    // Symmetric key (HS256)
    public string SecretKey { get; set; } = string.Empty;

    // Asymmetric keys (RS256, ES256)
    public string PrivateKeyPath { get; set; } = string.Empty;
    public string PrivateKeyBase64 { get; set; } = string.Empty;
    public string PublicKeyPath { get; set; } = string.Empty;
    public string PublicKeyBase64 { get; set; } = string.Empty;

    // Token metadata
    public string Issuer { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;

    // Per-type expiration (minutes)
    public int AccessTokenExpirationMinutes { get; set; } = 10;
    public int RefreshTokenExpirationMinutes { get; set; } = 1440;
    public int IdTokenExpirationMinutes { get; set; } = 60;
    public int ActionTokenExpirationMinutes { get; set; } = 1440;
}
