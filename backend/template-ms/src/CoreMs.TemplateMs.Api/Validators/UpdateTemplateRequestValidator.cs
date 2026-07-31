using CoreMs.TemplateMs.Core.Enums;
using CoreMs.TemplateMs.Core.Models;
using FluentValidation;

namespace CoreMs.TemplateMs.Api.Validators;

public class UpdateTemplateRequestValidator : AbstractValidator<UpdateTemplateRequest>
{
    public UpdateTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateId)
            .MaximumLength(255).WithMessage("TemplateId must not exceed 255 characters")
            .When(x => x.TemplateId != null);

        RuleFor(x => x.Language)
            .MaximumLength(10).WithMessage("Language must not exceed 10 characters")
            .When(x => x.Language != null);

        RuleFor(x => x.Name)
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.Category)
            .Must(c => TemplateCategory.IsValid(c!)).WithMessage("Category must be one of: COMMON, EMAIL, SMS, DOCUMENT")
            .When(x => x.Category != null);
    }
}
