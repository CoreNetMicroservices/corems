using CoreMs.Common.Data;
using CoreMs.TemplateMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreMs.TemplateMs.Infrastructure.Data;

public class SeedDataService(TemplateMsDbContext context, ILogger<SeedDataService> logger)
    : CoreMsSeeder<TemplateEntity>(context, logger)
{
    protected override async Task<bool> AlreadySeededAsync(CancellationToken ct) =>
        await Context.Set<TemplateEntity>()
            .AnyAsync(t => t.TemplateId == "corems-styles" && t.Language == "en", ct);

    protected override IEnumerable<TemplateEntity> BuildSeedData() =>
    [
        // ===== SHARED STYLES (COMMON) =====
        CreateStylesTemplate(),

        // ===== AUTH EMAIL TEMPLATES =====
        CreateEmailVerificationTemplate(),
        CreateWelcomeEmailTemplate(),
        CreatePasswordResetTemplate(),
        CreatePasswordChangedTemplate(),
        CreateAccountLockedTemplate(),

        // ===== AUTH SMS TEMPLATES =====
        CreateSmsVerificationTemplate(),
        CreateSmsLoginCodeTemplate(),

        // ===== DOCUMENT TEMPLATES =====
        CreateInvoiceTemplate()
    ];

    private static TemplateEntity CreateStylesTemplate() => new()
    {
        TemplateId = "corems-styles",
        Language = "en",
        Name = "CoreMS Email Styles",
        Description = "Shared CSS styles for all CoreMS emails. Include via {{> corems-styles}}",
        Content = """
            <style>
                body { margin: 0; padding: 20px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f5f5f5; color: #333; }
                .email-container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); overflow: hidden; }
                .email-header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; padding: 32px 24px; text-align: center; }
                .email-header h1 { margin: 0; font-size: 24px; font-weight: 600; letter-spacing: -0.5px; }
                .email-content { padding: 40px 32px; }
                .email-footer { padding: 20px 32px; background-color: #f9fafb; border-top: 1px solid #e5e7eb; text-align: center; }
                .email-footer p { margin: 0; font-size: 12px; color: #9ca3af; }
                p { margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: #4b5563; }
                .greeting { font-size: 16px; color: #1f2937; margin-bottom: 20px; }
                a { color: #667eea; text-decoration: none; }
                a:hover { text-decoration: underline; }
                .btn-primary { display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff !important; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 15px; }
                .btn-primary:hover { opacity: 0.9; text-decoration: none; }
                .box-info { background-color: #eff6ff; border-left: 4px solid #667eea; padding: 16px; margin: 24px 0; border-radius: 4px; font-size: 14px; color: #1e40af; }
                .box-warning { background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 16px; margin: 24px 0; border-radius: 4px; font-size: 14px; color: #92400e; }
                .box-danger { background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 16px; margin: 24px 0; border-radius: 4px; font-size: 14px; color: #991b1b; }
                .code-block { background-color: #f3f4f6; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; text-align: center; margin: 24px 0; }
                .code-block .code { font-size: 32px; font-weight: 700; letter-spacing: 6px; color: #1f2937; font-family: 'Courier New', monospace; }
                .link-text { margin: 16px 0; font-size: 13px; color: #9ca3af; word-break: break-all; }
                .signature { margin-top: 32px; padding-top: 20px; border-top: 1px solid #f3f4f6; font-size: 14px; color: #6b7280; }
            </style>
            """,
        Category = "COMMON",
        ParamSchema = new Dictionary<string, object>(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TemplateEntity CreateEmailVerificationTemplate() => new()
    {
        TemplateId = "email-verification",
        Language = "en",
        Name = "Email Verification",
        Description = "Sent to verify user email address after registration or resend",
        Content = """
            <html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">{{> corems-styles}}</head><body>
            <div class="email-container">
                <div class="email-header"><h1>Verify Your Email</h1></div>
                <div class="email-content">
                    <p class="greeting">Hi {{firstName}},</p>
                    <p>Thanks for signing up! Please verify your email address to activate your account.</p>
                    <div style="text-align: center; margin: 32px 0;">
                        <a href="{{verificationUrl}}" class="btn-primary">Verify Email Address</a>
                    </div>
                    <div class="link-text">
                        Or copy and paste this link into your browser:<br>
                        <a href="{{verificationUrl}}">{{verificationUrl}}</a>
                    </div>
                    <div class="box-warning">
                        <strong>Expires in {{expirationHours}} hours.</strong> If you didn't create an account, you can safely ignore this email.
                    </div>
                    <div class="signature">
                        Cheers,<br><strong>The CoreMS Team</strong>
                    </div>
                </div>
                <div class="email-footer"><p>&copy; {{year}} CoreMS. All rights reserved.</p></div>
            </div></body></html>
            """,
        Category = "EMAIL",
        ParamSchema = new Dictionary<string, object>
        {
            ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["verificationUrl"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["expirationHours"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["year"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = false }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TemplateEntity CreateWelcomeEmailTemplate() => new()
    {
        TemplateId = "welcome-email",
        Language = "en",
        Name = "Welcome Email",
        Description = "Sent to new users after successful email verification",
        Content = """
            <html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">{{> corems-styles}}</head><body>
            <div class="email-container">
                <div class="email-header"><h1>Welcome to {{appName}}</h1></div>
                <div class="email-content">
                    <p class="greeting">Hi {{firstName}},</p>
                    <p>Your email has been verified and your account is fully activated. We're excited to have you on board!</p>
                    <div class="box-info">
                        <strong>Getting Started</strong><br>
                        Explore the platform, set up your profile, and start using all the features available to you.
                    </div>
                    <div style="text-align: center; margin: 32px 0;">
                        <a href="{{appUrl}}" class="btn-primary">Go to {{appName}}</a>
                    </div>
                    <p>If you have any questions, don't hesitate to reach out to our support team.</p>
                    <div class="signature">
                        Cheers,<br><strong>The {{appName}} Team</strong>
                    </div>
                </div>
                <div class="email-footer"><p>&copy; {{year}} {{appName}}. All rights reserved.</p></div>
            </div></body></html>
            """,
        Category = "EMAIL",
        ParamSchema = new Dictionary<string, object>
        {
            ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["appName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["appUrl"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["year"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = false }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TemplateEntity CreatePasswordResetTemplate() => new()
    {
        TemplateId = "password-reset",
        Language = "en",
        Name = "Password Reset",
        Description = "Sent when user requests to reset their password",
        Content = """
            <html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">{{> corems-styles}}</head><body>
            <div class="email-container">
                <div class="email-header"><h1>Reset Your Password</h1></div>
                <div class="email-content">
                    <p class="greeting">Hi {{firstName}},</p>
                    <p>We received a request to reset the password for your account. Click the button below to choose a new password.</p>
                    <div style="text-align: center; margin: 32px 0;">
                        <a href="{{resetUrl}}" class="btn-primary">Reset Password</a>
                    </div>
                    <div class="link-text">
                        Or copy and paste this link into your browser:<br>
                        <a href="{{resetUrl}}">{{resetUrl}}</a>
                    </div>
                    <div class="box-danger">
                        <strong>This link expires in {{expirationHours}} hours.</strong> If you didn't request a password reset, please ignore this email — your password will remain unchanged.
                    </div>
                    <div class="signature">
                        Stay safe,<br><strong>The CoreMS Security Team</strong>
                    </div>
                </div>
                <div class="email-footer"><p>&copy; {{year}} CoreMS. All rights reserved.</p></div>
            </div></body></html>
            """,
        Category = "EMAIL",
        ParamSchema = new Dictionary<string, object>
        {
            ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["resetUrl"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["expirationHours"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["year"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = false }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TemplateEntity CreatePasswordChangedTemplate() => new()
    {
        TemplateId = "password-changed",
        Language = "en",
        Name = "Password Changed Confirmation",
        Description = "Sent after a user successfully changes their password",
        Content = """
            <html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">{{> corems-styles}}</head><body>
            <div class="email-container">
                <div class="email-header"><h1>Password Changed</h1></div>
                <div class="email-content">
                    <p class="greeting">Hi {{firstName}},</p>
                    <p>Your password was successfully changed on {{changedAt}}.</p>
                    <div class="box-danger">
                        <strong>Wasn't you?</strong> If you did not make this change, please reset your password immediately and contact our support team.
                    </div>
                    <div style="text-align: center; margin: 32px 0;">
                        <a href="{{resetUrl}}" class="btn-primary">Reset Password Now</a>
                    </div>
                    <div class="signature">
                        Stay safe,<br><strong>The CoreMS Security Team</strong>
                    </div>
                </div>
                <div class="email-footer"><p>&copy; {{year}} CoreMS. All rights reserved.</p></div>
            </div></body></html>
            """,
        Category = "EMAIL",
        ParamSchema = new Dictionary<string, object>
        {
            ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["changedAt"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["resetUrl"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["year"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = false }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TemplateEntity CreateAccountLockedTemplate() => new()
    {
        TemplateId = "account-locked",
        Language = "en",
        Name = "Account Locked",
        Description = "Sent when account is locked due to suspicious activity or too many failed attempts",
        Content = """
            <html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">{{> corems-styles}}</head><body>
            <div class="email-container">
                <div class="email-header"><h1>Account Locked</h1></div>
                <div class="email-content">
                    <p class="greeting">Hi {{firstName}},</p>
                    <p>We've detected unusual activity on your account and have temporarily locked it to protect your security.</p>
                    <div class="box-danger">
                        <strong>What happened:</strong> {{lockReason}}
                    </div>
                    <p>To regain access, you can reset your password using the button below. If you believe this was a mistake, please contact our support team.</p>
                    <div style="text-align: center; margin: 32px 0;">
                        <a href="{{unlockUrl}}" class="btn-primary">Unlock Account</a>
                    </div>
                    <div class="signature">
                        Stay safe,<br><strong>The CoreMS Security Team</strong>
                    </div>
                </div>
                <div class="email-footer"><p>&copy; {{year}} CoreMS. All rights reserved.</p></div>
            </div></body></html>
            """,
        Category = "EMAIL",
        ParamSchema = new Dictionary<string, object>
        {
            ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["lockReason"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["unlockUrl"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["year"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = false }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TemplateEntity CreateSmsVerificationTemplate() => new()
    {
        TemplateId = "sms-verification",
        Language = "en",
        Name = "SMS Verification Code",
        Description = "Phone number verification code sent during registration or resend",
        Content = "{{code}} is your CoreMS verification code. It expires in {{expirationMinutes}} minutes. Do not share this code with anyone.",
        Category = "SMS",
        ParamSchema = new Dictionary<string, object>
        {
            ["code"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["expirationMinutes"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TemplateEntity CreateSmsLoginCodeTemplate() => new()
    {
        TemplateId = "sms-login-code",
        Language = "en",
        Name = "SMS Login Code (2FA)",
        Description = "Two-factor authentication code sent during login",
        Content = "{{code}} is your CoreMS login code. It expires in {{expirationMinutes}} minutes. If you didn't request this, change your password immediately.",
        Category = "SMS",
        ParamSchema = new Dictionary<string, object>
        {
            ["code"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["expirationMinutes"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TemplateEntity CreateInvoiceTemplate() => new()
    {
        TemplateId = "invoice-document",
        Language = "en",
        Name = "Invoice Document",
        Description = "Invoice template for billing",
        Content = """
            <html><head><meta charset="UTF-8">
            <style>
                body { margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; color: #333; }
                .doc-container { width: 100%; padding: 0; }
                .doc-header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #fff; padding: 40px; text-align: center; }
                .doc-header h1 { margin: 0; font-size: 28px; font-weight: 600; }
                .doc-content { padding: 40px; }
                .doc-footer { padding: 20px 40px; background-color: #f9fafb; border-top: 1px solid #e5e7eb; text-align: center; font-size: 12px; color: #9ca3af; }
                .info-row { display: flex; justify-content: space-between; margin-bottom: 32px; font-size: 15px; }
                .info-block { background-color: #eff6ff; border-left: 4px solid #667eea; padding: 16px; margin-bottom: 32px; border-radius: 4px; }
                table { width: 100%; border-collapse: collapse; margin: 24px 0; }
                th { padding: 14px 16px; text-align: left; font-size: 13px; font-weight: 600; border-bottom: 2px solid #667eea; background-color: #f9fafb; }
                th:last-child { text-align: right; }
                td { padding: 14px 16px; border-bottom: 1px solid #e5e7eb; font-size: 14px; }
                td:last-child { text-align: right; }
                .total-row { background-color: #f3f4f6; font-weight: 700; font-size: 15px; }
                .notes { font-size: 13px; color: #6b7280; margin-top: 24px; padding: 16px; background: #f9fafb; border-radius: 4px; }
            </style></head><body>
            <div class="doc-container">
                <div class="doc-header"><h1>Invoice #{{invoiceNumber}}</h1></div>
                <div class="doc-content">
                    <div class="info-row">
                        <div><strong>Date:</strong> {{issueDate}}</div>
                        <div><strong>Due:</strong> {{dueDate}}</div>
                    </div>
                    <div class="info-block">
                        <strong>Bill To:</strong><br>{{customerName}}<br>{{customerEmail}}
                    </div>
                    <table>
                        <thead>
                            <tr>
                                <th>Description</th>
                                <th>Amount</th>
                            </tr>
                        </thead>
                        <tbody>
                            {{#each items}}
                            <tr>
                                <td>{{this.description}}</td>
                                <td>{{this.amount}}</td>
                            </tr>
                            {{/each}}
                            <tr class="total-row">
                                <td>Total</td>
                                <td>{{currency}} {{totalAmount}}</td>
                            </tr>
                        </tbody>
                    </table>
                    {{#if notes}}<div class="notes">{{notes}}</div>{{/if}}
                </div>
                <div class="doc-footer">Thank you for your business.</div>
            </div></body></html>
            """,
        Category = "DOCUMENT",
        ParamSchema = new Dictionary<string, object>
        {
            ["invoiceNumber"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["issueDate"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["dueDate"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["customerName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["customerEmail"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["items"] = new Dictionary<string, object> { ["type"] = "array", ["required"] = true },
            ["currency"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["totalAmount"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
            ["notes"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = false }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
