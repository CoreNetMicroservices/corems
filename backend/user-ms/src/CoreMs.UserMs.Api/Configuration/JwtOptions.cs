namespace CoreMs.UserMs.Api.Configuration;

using System.ComponentModel.DataAnnotations;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Algorithm { get; set; } = "HS256";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = "corems";

    [Required]
    public string KeyId { get; set; } = "corems-1";

    public string SecretKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AccessTokenExpirationMinutes { get; set; } = 10;

    [Range(1, int.MaxValue)]
    public int RefreshTokenExpirationMinutes { get; set; } = 1440;

    [Range(1, int.MaxValue)]
    public int IdTokenExpirationMinutes { get; set; } = 60;

    [Range(1, int.MaxValue)]
    public int AuthorizationCodeExpirationMinutes { get; set; } = 10;
}
