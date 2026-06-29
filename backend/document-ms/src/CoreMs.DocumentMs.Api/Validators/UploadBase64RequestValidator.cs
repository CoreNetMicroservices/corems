using CoreMs.DocumentMs.Core.Models;
using FluentValidation;

namespace CoreMs.DocumentMs.Api.Validators;

public class UploadBase64RequestValidator : AbstractValidator<UploadBase64Request>
{
    public UploadBase64RequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.ContentBase64)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(255)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);
    }
}
