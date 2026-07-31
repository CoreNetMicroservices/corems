using CoreMs.TemplateMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 2: Parameter extraction completeness
/// For any valid Handlebars template content, the set of parameters extracted by ExtractParameters
/// should be a superset of the parameters actually required for rendering (i.e., rendering with all
/// extracted parameters should never fail due to missing parameters).
///
/// **Validates: Requirements 1.5, 7.2, 7.3**
/// </summary>
public class TemplateEnginePropertyTests
{
    private readonly TemplateEngine _engine = new();

    #region Property 2: Parameter extraction completeness

    [Property(MaxTest = 50, Arbitrary = [typeof(TemplateArbitraries)])]
    public void ExtractParameters_AlwaysExtractsAllRequiredParams(ValidTemplateWithParamsInput input)
    {
        var extracted = _engine.ExtractParameters(input.Content);

        foreach (var param in input.ParameterNames)
        {
            Assert.Contains(param, extracted);
        }
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(TemplateArbitraries)])]
    public void ExtractParameters_RenderingWithExtractedParams_NeverThrows(ValidTemplateWithParamsInput input)
    {
        var extracted = _engine.ExtractParameters(input.Content);
        var compiled = _engine.Compile(input.Content);

        var parameters = extracted.ToDictionary(p => p, p => (object)"test_value");

        var result = _engine.Render(compiled, parameters);
        Assert.NotNull(result);
    }

    [Fact]
    public void ExtractParameters_DotNotation_ExtractsRootLevel()
    {
        var content = "Hello {{user.name}}, your role is {{user.role}}";
        var extracted = _engine.ExtractParameters(content);

        Assert.Contains("user", extracted);
        Assert.DoesNotContain("user.name", extracted);
    }

    [Fact]
    public void ExtractParameters_BlockHelpers_ExtractsVariable()
    {
        var content = "{{#if active}}Active{{/if}} {{#each items}}{{this}}{{/each}}";
        var extracted = _engine.ExtractParameters(content);

        Assert.Contains("active", extracted);
        Assert.Contains("items", extracted);
    }

    [Fact]
    public void ExtractParameters_ExcludesBuiltins()
    {
        var content = "{{#each items}}{{@index}}: {{this}}{{/each}}";
        var extracted = _engine.ExtractParameters(content);

        Assert.Contains("items", extracted);
        Assert.DoesNotContain("@index", extracted);
        Assert.DoesNotContain("this", extracted);
    }

    #endregion
}

#region Arbitraries

public record ValidTemplateWithParamsInput(string Content, IReadOnlyList<string> ParameterNames)
{
    public override string ToString() => Content;
}

public class TemplateArbitraries
{
    private static readonly string[] ParamNames =
        ["name", "email", "company", "title", "message", "count", "active", "items", "greeting", "footer"];

    public static Arbitrary<ValidTemplateWithParamsInput> ValidTemplateWithParamsInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(1, int.MaxValue);
        Gen<ValidTemplateWithParamsInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var paramCount = rng.Next(1, 5);
            var selectedParams = ParamNames.OrderBy(_ => rng.Next()).Take(paramCount).ToList();

            var parts = selectedParams.Select(p => "{{" + p + "}}");
            var content = "Hello " + string.Join(" ", parts) + " end.";

            return new ValidTemplateWithParamsInput(content, selectedParams);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
