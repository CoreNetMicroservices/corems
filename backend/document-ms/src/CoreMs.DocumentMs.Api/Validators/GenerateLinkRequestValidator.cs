using CoreMs.DocumentMs.Core.Models;
using FluentValidation;

namespace CoreMs.DocumentMs.Api.Validators;

public class GenerateLinkRequestValidator : AbstractValidator<GenerateLinkRequest>
{
    public GenerateLinkRequestValidator()
    {
        RuleFor(x => x.ExpiresInMinutes)
            .InclusiveBetween(1, 43200);
    }
}
