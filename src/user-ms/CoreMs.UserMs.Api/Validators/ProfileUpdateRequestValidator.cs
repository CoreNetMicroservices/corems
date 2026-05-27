using CoreMs.UserMs.Domain.Models;
using FluentValidation;

namespace CoreMs.UserMs.Api.Validators;

public class ProfileUpdateRequestValidator : AbstractValidator<ProfileUpdateRequest>
{
    public ProfileUpdateRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(50).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(50).When(x => x.LastName != null);
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber != null);
        RuleFor(x => x.ImageUrl).MaximumLength(255).When(x => x.ImageUrl != null);
    }
}
