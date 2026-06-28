using CoreMs.CommunicationMs.Core.Models;
using FluentValidation;

namespace CoreMs.CommunicationMs.Api.Validators;

public class SmsMessageRequestValidator : AbstractValidator<SmsMessageRequest>
{
    public SmsMessageRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+\d{1,20}$").WithMessage("Phone number must be in E.164 format (e.g. +15551234567)");
        RuleFor(x => x.Message).MaximumLength(1600).When(x => x.Message != null);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Message) || x.Template != null)
            .WithMessage("Either message or template must be provided");
    }
}
