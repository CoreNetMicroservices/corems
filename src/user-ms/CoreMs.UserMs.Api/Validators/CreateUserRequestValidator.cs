using CoreMs.UserMs.Domain.Models;
using FluentValidation;

namespace CoreMs.UserMs.Api.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.FirstName).MaximumLength(50).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(50).When(x => x.LastName != null);
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber != null);
    }
}
