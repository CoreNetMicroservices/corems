namespace CoreMs.UserMs.Core.Configuration;

public class NotificationTemplateOptions
{
    public const string SectionName = "NotificationTemplates";

    public EmailTemplates Email { get; set; } = new();
    public SmsTemplates Sms { get; set; } = new();
}

public class EmailTemplates
{
    public string EmailVerification { get; set; } = "email-verification";
    public string Welcome { get; set; } = "welcome-email";
    public string PasswordReset { get; set; } = "password-reset";
    public string PasswordChanged { get; set; } = "password-changed";
}

public class SmsTemplates
{
    public string VerificationCode { get; set; } = "sms-verification";
    public string LoginCode { get; set; } = "sms-login-code";
}
