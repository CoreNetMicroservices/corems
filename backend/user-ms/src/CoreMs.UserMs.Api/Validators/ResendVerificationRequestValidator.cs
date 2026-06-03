using CoreMs.UserMs.Core.Models;
using FluentValidation;

namespace CoreMs.UserMs.Api.Validators;

public class ResendVerificationRequestValidator : AbstractValidator<ResendVerificationRequest>
{
    public ResendVerificationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Verification type is required");
    }
}
