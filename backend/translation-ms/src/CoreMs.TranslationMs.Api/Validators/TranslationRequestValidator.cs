using CoreMs.TranslationMs.Core.Models;
using FluentValidation;

namespace CoreMs.TranslationMs.Api.Validators;

public class TranslationRequestValidator : AbstractValidator<TranslationRequest>
{
    private const int MaxKeyLength = 255;
    private const int MaxValueLength = 5000;

    public TranslationRequestValidator()
    {
        RuleFor(x => x.Translations)
            .NotNull().WithMessage("Translations field is required")
            .Must(d => d != null && d.Count > 0).WithMessage("Translations must contain at least one entry")
            .Must(d => d == null || d.Keys.All(k => !string.IsNullOrWhiteSpace(k)))
                .WithMessage("Translation keys must not be empty or whitespace")
            .Must(d => d == null || d.Keys.All(k => k.Length <= MaxKeyLength))
                .WithMessage($"Translation keys must not exceed {MaxKeyLength} characters")
            .Must(d => d == null || d.Values.All(v => v == null || v.Length <= MaxValueLength))
                .WithMessage($"Translation values must not exceed {MaxValueLength} characters");
    }
}
