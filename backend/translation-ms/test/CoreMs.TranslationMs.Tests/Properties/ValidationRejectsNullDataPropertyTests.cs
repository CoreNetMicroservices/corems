using CoreMs.TranslationMs.Api.Validators;
using CoreMs.TranslationMs.Core.Models;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.TranslationMs.Tests.Properties;

/// <summary>
/// Property 7: Validation rejects invalid data
/// For any request where Data is null or empty, validation fails;
/// for any non-null, non-empty data with valid keys/values, validation passes.
/// **Validates: Requirements 7.4, 8.3, 11.2**
/// </summary>
public class ValidationRejectsNullDataPropertyTests
{
    private readonly TranslationRequestValidator _validator = new();

    [Fact]
    public void NullData_FailsValidation()
    {
        var request = new TranslationRequest { Data = null! };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Data");
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(TranslationArbitraries)])]
    public void NonNullNonEmptyData_PassesValidation(TranslationInput input)
    {
        var request = new TranslationRequest { Data = input.Data };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyDictionary_FailsValidation()
    {
        var request = new TranslationRequest { Data = new Dictionary<string, string>() };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least one entry"));
    }

    [Fact]
    public void WhitespaceKey_FailsValidation()
    {
        var request = new TranslationRequest { Data = new Dictionary<string, string> { ["  "] = "value" } };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("must not be empty or whitespace"));
    }

    [Fact]
    public void EmptyKey_FailsValidation()
    {
        var request = new TranslationRequest { Data = new Dictionary<string, string> { [""] = "value" } };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("must not be empty or whitespace"));
    }

    [Fact]
    public void KeyExceedsMaxLength_FailsValidation()
    {
        var longKey = new string('a', 256);
        var request = new TranslationRequest { Data = new Dictionary<string, string> { [longKey] = "value" } };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("255"));
    }

    [Fact]
    public void ValueExceedsMaxLength_FailsValidation()
    {
        var longValue = new string('a', 5001);
        var request = new TranslationRequest { Data = new Dictionary<string, string> { ["key"] = longValue } };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("5000"));
    }

    [Fact]
    public void KeyAtMaxLength_PassesValidation()
    {
        var maxKey = new string('a', 255);
        var request = new TranslationRequest { Data = new Dictionary<string, string> { [maxKey] = "value" } };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValueAtMaxLength_PassesValidation()
    {
        var maxValue = new string('a', 5000);
        var request = new TranslationRequest { Data = new Dictionary<string, string> { ["key"] = maxValue } };
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
