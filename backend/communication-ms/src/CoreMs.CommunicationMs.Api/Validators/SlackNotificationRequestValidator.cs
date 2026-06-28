using CoreMs.CommunicationMs.Core.Models;
using FluentValidation;

namespace CoreMs.CommunicationMs.Api.Validators;

public class SlackNotificationRequestValidator : AbstractValidator<SlackNotificationRequest>
{
    public SlackNotificationRequestValidator()
    {
        RuleFor(x => x.Channel).NotEmpty().Matches(@"^(#|@).+").WithMessage("Channel must start with # or @");
        RuleFor(x => x.Message).NotEmpty();
        RuleFor(x => x.Level).Must(l => l is "info" or "warning" or "critical").WithMessage("Level must be 'info', 'warning', or 'critical'");
    }
}
