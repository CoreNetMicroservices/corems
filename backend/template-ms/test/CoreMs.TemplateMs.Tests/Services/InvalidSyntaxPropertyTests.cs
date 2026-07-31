using CoreMs.Common.Exceptions;
using CoreMs.TemplateMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 6: Invalid syntax rejection
/// For any template content string that is not valid Handlebars syntax, the Template_Engine
/// should reject it during validation.
///
/// **Validates: Requirements 1.2, 2.2, 7.1**
/// </summary>
public class InvalidSyntaxPropertyTests
{
    private readonly TemplateEngine _engine = new();

    #region Property 6: Invalid syntax rejection

    [Property(MaxTest = 50, Arbitrary = [typeof(InvalidSyntaxArbitraries)])]
    public void ValidateSyntax_WithInvalidContent_AlwaysThrowsServiceException(InvalidTemplateInput input)
    {
        var ex = Assert.Throws<ServiceException>(() => _engine.ValidateSyntax(input.Content));
        Assert.Equal(400, ex.HttpStatusCode);
    }

    [Fact]
    public void ValidateSyntax_WithValidContent_DoesNotThrow()
    {
        var content = "Hello {{name}}, welcome to {{company}}";
        _engine.ValidateSyntax(content);
    }

    [Fact]
    public void ValidateSyntax_WithUnclosedBlock_Throws()
    {
        var content = "{{#if active}}content without closing";
        var ex = Assert.Throws<ServiceException>(() => _engine.ValidateSyntax(content));
        Assert.Equal(400, ex.HttpStatusCode);
    }

    [Fact]
    public void ValidateSyntax_WithMismatchedBlocks_Throws()
    {
        var content = "{{#if active}}content{{/each}}";
        var ex = Assert.Throws<ServiceException>(() => _engine.ValidateSyntax(content));
        Assert.Equal(400, ex.HttpStatusCode);
    }

    #endregion
}

#region Arbitraries

public record InvalidTemplateInput(string Content)
{
    public override string ToString() => Content;
}

public class InvalidSyntaxArbitraries
{
    public static Arbitrary<InvalidTemplateInput> InvalidTemplateInputArbitrary()
    {
        var invalidTemplates = new[]
        {
            "{{#if active}}no closing",
            "{{#each items}}no closing",
            "{{#if x}}content{{/each}}",
            "{{#unless flag}}content{{/if}}",
            "{{#each list}}item{{/unless}}",
            "{{#with obj}}content{{/each}}",
            "{{#if}}missing expression{{/if}}",
            "{{#each}}missing expression{{/each}}"
        };

        Gen<InvalidTemplateInput> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements(invalidTemplates),
            content => new InvalidTemplateInput(content));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
