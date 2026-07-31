using CoreMs.TemplateMs.Core.Entities;
using CoreMs.TemplateMs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.TemplateMs.Api.Services;

public class SeedDataService
{
    private readonly TemplateMsDbContext _context;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(TemplateMsDbContext context, ILogger<SeedDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _context.Set<TemplateEntity>().AnyAsync(t => t.TemplateId == "welcome-email" && t.Language == "en"))
        {
            _logger.LogInformation("Template seed data already exists — skipping");
            return;
        }

        _logger.LogInformation("Seeding template data...");
        _context.Set<TemplateEntity>().AddRange(CreateTemplates());
        await _context.SaveChangesAsync();
        _logger.LogInformation("Template seed data complete — 6 templates created");
    }

    private static List<TemplateEntity> CreateTemplates() =>
    [
        new TemplateEntity
        {
            TemplateId = "welcome-email",
            Language = "en",
            Name = "Welcome Email",
            Description = "Sent to new users after registration",
            Content = "<h1>Welcome, {{firstName}}!</h1><p>Thank you for joining {{appName}}. We're excited to have you on board.</p><p>Best regards,<br/>The {{appName}} Team</p>",
            Category = "EMAIL",
            ParamSchema = new Dictionary<string, object>
            {
                ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["appName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new TemplateEntity
        {
            TemplateId = "email-verification",
            Language = "en",
            Name = "Email Verification",
            Description = "Sent to verify user email address",
            Content = "<h1>Verify Your Email</h1><p>Hi {{firstName}},</p><p>Please click the link below to verify your email address:</p><p><a href=\"{{verificationUrl}}\">Verify Email</a></p><p>This link expires in {{expirationHours}} hours.</p>",
            Category = "EMAIL",
            ParamSchema = new Dictionary<string, object>
            {
                ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["verificationUrl"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["expirationHours"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new TemplateEntity
        {
            TemplateId = "password-reset",
            Language = "en",
            Name = "Password Reset",
            Description = "Sent when user requests password reset",
            Content = "<h1>Reset Your Password</h1><p>Hi {{firstName}},</p><p>We received a request to reset your password. Click the link below:</p><p><a href=\"{{resetUrl}}\">Reset Password</a></p><p>If you did not request this, please ignore this email. This link expires in {{expirationHours}} hours.</p>",
            Category = "EMAIL",
            ParamSchema = new Dictionary<string, object>
            {
                ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["resetUrl"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["expirationHours"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new TemplateEntity
        {
            TemplateId = "sms-welcome",
            Language = "en",
            Name = "SMS Welcome",
            Description = "Welcome SMS sent to new users",
            Content = "Welcome to {{appName}}, {{firstName}}! Your account is ready.",
            Category = "SMS",
            ParamSchema = new Dictionary<string, object>
            {
                ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["appName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new TemplateEntity
        {
            TemplateId = "sms-verification",
            Language = "en",
            Name = "SMS Verification",
            Description = "Phone number verification code",
            Content = "Your {{appName}} verification code is: {{code}}. It expires in {{expirationMinutes}} minutes.",
            Category = "SMS",
            ParamSchema = new Dictionary<string, object>
            {
                ["appName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["code"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["expirationMinutes"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new TemplateEntity
        {
            TemplateId = "invoice-document",
            Language = "en",
            Name = "Invoice Document",
            Description = "Invoice template for billing",
            Content = "<h1>Invoice #{{invoiceNumber}}</h1><p>Date: {{issueDate}}</p><p>Bill To: {{customerName}}</p><p>Amount: {{currency}} {{amount}}</p><p>Due Date: {{dueDate}}</p><p>Thank you for your business.</p>",
            Category = "DOCUMENT",
            ParamSchema = new Dictionary<string, object>
            {
                ["invoiceNumber"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["issueDate"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["customerName"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["currency"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["amount"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true },
                ["dueDate"] = new Dictionary<string, object> { ["type"] = "string", ["required"] = true }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }
    ];
}
