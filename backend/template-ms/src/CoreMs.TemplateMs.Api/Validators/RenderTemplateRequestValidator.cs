using CoreMs.TemplateMs.Core.Models;
using FluentValidation;

namespace CoreMs.TemplateMs.Api.Validators;

public class RenderTemplateRequestValidator : AbstractValidator<RenderTemplateRequest>
{
    public RenderTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("TemplateId is required");
    }
}
