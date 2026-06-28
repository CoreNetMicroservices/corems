using CoreMs.CommunicationMs.Core.Models;
using FluentValidation;

namespace CoreMs.CommunicationMs.Api.Validators;

public class EmailNotificationRequestValidator : AbstractValidator<EmailNotificationRequest>
{
    public EmailNotificationRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Recipient).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Sender).EmailAddress().MaximumLength(254).When(x => x.Sender != null);
        RuleFor(x => x.SenderName).MaximumLength(100).When(x => x.SenderName != null);
        RuleFor(x => x.Body).MaximumLength(10000).When(x => x.Body != null);
        RuleFor(x => x.EmailType).Must(t => t is "txt" or "html").WithMessage("EmailType must be 'txt' or 'html'");
        RuleFor(x => x.Level).Must(l => l is "info" or "warning" or "critical").WithMessage("Level must be 'info', 'warning', or 'critical'");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Body) || x.Template != null)
            .WithMessage("Either body or template must be provided");

        RuleForEach(x => x.Cc).EmailAddress().When(x => x.Cc != null);
        RuleForEach(x => x.Bcc).EmailAddress().When(x => x.Bcc != null);
    }
}
