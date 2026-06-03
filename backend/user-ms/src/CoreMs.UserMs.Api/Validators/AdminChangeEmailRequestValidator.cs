using CoreMs.UserMs.Api.Controllers;
using FluentValidation;

namespace CoreMs.UserMs.Api.Validators;

public class AdminChangeEmailRequestValidator : AbstractValidator<AdminChangeEmailRequest>
{
    public AdminChangeEmailRequestValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("New email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
