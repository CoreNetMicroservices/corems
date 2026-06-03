namespace CoreMs.UserMs.Api.Configuration;

using System.ComponentModel.DataAnnotations;

public class AppOptions
{
    public const string SectionName = "App";

    [Required]
    public string FrontendBaseUrl { get; set; } = "http://localhost:8080";

    [Range(1, int.MaxValue)]
    public int VerificationEmailExpirationMinutes { get; set; } = 1440;

    [Range(1, int.MaxValue)]
    public int PasswordResetExpirationMinutes { get; set; } = 1440;

    [Required, MinLength(1)]
    public List<string> DefaultRoles { get; set; } = ["USER_MS_USER"];
}
