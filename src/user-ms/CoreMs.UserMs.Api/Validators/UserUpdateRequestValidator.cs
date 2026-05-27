using CoreMs.UserMs.Domain.Models;
using FluentValidation;

namespace CoreMs.UserMs.Api.Validators;

public class UserUpdateRequestValidator : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(50).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(50).When(x => x.LastName != null);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email != null);
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber != null);
        RuleFor(x => x.ImageUrl).MaximumLength(255).When(x => x.ImageUrl != null);
    }
}
