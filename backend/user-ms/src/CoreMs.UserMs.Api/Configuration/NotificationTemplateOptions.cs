namespace CoreMs.UserMs.Api.Configuration;

public class NotificationTemplateOptions
{
    public const string SectionName = "NotificationTemplates";

    public EmailTemplates Email { get; set; } = new();
    public SmsTemplates Sms { get; set; } = new();
}

public class EmailTemplates
{
    public string Welcome { get; set; } = string.Empty;
    public string EmailVerification { get; set; } = string.Empty;
    public string PasswordReset { get; set; } = string.Empty;
}

public class SmsTemplates
{
    public string Welcome { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
}
