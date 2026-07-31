using CoreMs.TemplateMs.Api.Validators;
using CoreMs.TemplateMs.Core.Models;
using FluentValidation.TestHelper;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Validation;

public class CreateTemplateRequestValidatorTests
{
    private readonly CreateTemplateRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "welcome-email",
            Name = "Welcome Email",
            Content = "Hello {{name}}",
            Category = "EMAIL"
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTemplateId_HasError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "",
            Name = "Test",
            Content = "Hello {{name}}",
            Category = "EMAIL"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TemplateId)
            .WithErrorMessage("TemplateId is required");
    }

    [Fact]
    public void TemplateIdExceeds255_HasError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = new string('a', 256),
            Name = "Test",
            Content = "Hello {{name}}",
            Category = "EMAIL"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TemplateId)
            .WithErrorMessage("TemplateId must not exceed 255 characters");
    }

    [Fact]
    public void EmptyName_HasError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "test",
            Name = "",
            Content = "Hello {{name}}",
            Category = "EMAIL"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public void NameExceeds255_HasError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "test",
            Name = new string('n', 256),
            Content = "Hello {{name}}",
            Category = "EMAIL"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 255 characters");
    }

    [Fact]
    public void EmptyContent_HasError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "test",
            Name = "Test",
            Content = "",
            Category = "EMAIL"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content is required");
    }

    [Fact]
    public void EmptyCategory_HasError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "test",
            Name = "Test",
            Content = "Hello {{name}}",
            Category = ""
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Category)
            .WithErrorMessage("Category is required");
    }

    [Fact]
    public void InvalidCategory_HasError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "test",
            Name = "Test",
            Content = "Hello {{name}}",
            Category = "INVALID"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Category)
            .WithErrorMessage("Category must be one of: COMMON, EMAIL, SMS, DOCUMENT");
    }

    [Theory]
    [InlineData("COMMON")]
    [InlineData("EMAIL")]
    [InlineData("SMS")]
    [InlineData("DOCUMENT")]
    public void ValidCategory_NoError(string category)
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "test",
            Name = "Test",
            Content = "Hello {{name}}",
            Category = category
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void LanguageExceeds10_HasError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "test",
            Name = "Test",
            Content = "Hello {{name}}",
            Category = "EMAIL",
            Language = "toolongvalue"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Language)
            .WithErrorMessage("Language must not exceed 10 characters");
    }

    [Fact]
    public void LanguageWithin10_NoError()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "test",
            Name = "Test",
            Content = "Hello {{name}}",
            Category = "EMAIL",
            Language = "en"
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Language);
    }

    [Fact]
    public void MultipleErrors_AllReported()
    {
        var request = new CreateTemplateRequest
        {
            TemplateId = "",
            Name = "",
            Content = "",
            Category = "INVALID"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TemplateId);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Content);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }
}

public class UpdateTemplateRequestValidatorTests
{
    private readonly UpdateTemplateRequestValidator _validator = new();

    [Fact]
    public void AllNullFields_HasNoErrors()
    {
        var request = new UpdateTemplateRequest();

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void TemplateIdExceeds255_HasError()
    {
        var request = new UpdateTemplateRequest { TemplateId = new string('a', 256) };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TemplateId)
            .WithErrorMessage("TemplateId must not exceed 255 characters");
    }

    [Fact]
    public void TemplateIdWithin255_NoError()
    {
        var request = new UpdateTemplateRequest { TemplateId = "valid-id" };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.TemplateId);
    }

    [Fact]
    public void InvalidCategory_HasError()
    {
        var request = new UpdateTemplateRequest { Category = "INVALID" };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Category)
            .WithErrorMessage("Category must be one of: COMMON, EMAIL, SMS, DOCUMENT");
    }

    [Fact]
    public void ValidCategory_NoError()
    {
        var request = new UpdateTemplateRequest { Category = "SMS" };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void LanguageExceeds10_HasError()
    {
        var request = new UpdateTemplateRequest { Language = "toolongvalue" };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Language)
            .WithErrorMessage("Language must not exceed 10 characters");
    }

    [Fact]
    public void LanguageWithin10_NoError()
    {
        var request = new UpdateTemplateRequest { Language = "fr" };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Language);
    }

    [Fact]
    public void NameExceeds255_HasError()
    {
        var request = new UpdateTemplateRequest { Name = new string('n', 256) };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 255 characters");
    }

    [Fact]
    public void NameWithin255_NoError()
    {
        var request = new UpdateTemplateRequest { Name = "Valid Name" };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}

public class RenderTemplateRequestValidatorTests
{
    private readonly RenderTemplateRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var request = new RenderTemplateRequest { TemplateId = "welcome-email" };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTemplateId_HasError()
    {
        var request = new RenderTemplateRequest { TemplateId = "" };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TemplateId)
            .WithErrorMessage("TemplateId is required");
    }
}
