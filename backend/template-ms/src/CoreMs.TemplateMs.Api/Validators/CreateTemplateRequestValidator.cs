using CoreMs.TemplateMs.Core.Enums;
using CoreMs.TemplateMs.Core.Models;
using FluentValidation;

namespace CoreMs.TemplateMs.Api.Validators;

public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("TemplateId is required")
            .MaximumLength(255).WithMessage("TemplateId must not exceed 255 characters");

        RuleFor(x => x.Language)
            .MaximumLength(10).WithMessage("Language must not exceed 10 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .Must(c => TemplateCategory.IsValid(c)).WithMessage("Category must be one of: COMMON, EMAIL, SMS, DOCUMENT");
    }
}
