using CoreMs.UserMs.Core.Models;
using FluentValidation;

namespace CoreMs.UserMs.Api.Validators;

public class VerifyPhoneRequestValidator : AbstractValidator<VerifyPhoneRequest>
{
    public VerifyPhoneRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required");
    }
}
