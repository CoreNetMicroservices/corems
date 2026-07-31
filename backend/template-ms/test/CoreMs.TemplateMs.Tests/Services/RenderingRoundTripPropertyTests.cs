using CoreMs.TemplateMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 1: Template rendering round-trip
/// For any valid template content and matching parameter set, compiling the template
/// and rendering with those parameters should produce a string that contains no
/// unresolved {{variable}} placeholders for any variable present in the parameter set.
///
/// **Validates: Requirements 5.1**
/// </summary>
public class RenderingRoundTripPropertyTests
{
    private readonly TemplateEngine _engine = new();

    #region Property 1: Template rendering round-trip

    [Property(MaxTest = 50, Arbitrary = [typeof(RenderingRoundTripArbitraries)])]
    public void Render_WithAllParams_NoUnresolvedPlaceholders(RenderableTemplateInput input)
    {
        var compiled = _engine.Compile(input.Content);
        var result = _engine.Render(compiled, input.Parameters);

        // No unresolved {{variable}} placeholders should remain for provided params
        foreach (var paramName in input.Parameters.Keys)
        {
            Assert.DoesNotContain("{{" + paramName + "}}", result);
        }
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(RenderingRoundTripArbitraries)])]
    public void Render_WithAllParams_ContainsParameterValues(RenderableTemplateInput input)
    {
        var compiled = _engine.Compile(input.Content);
        var result = _engine.Render(compiled, input.Parameters);

        // Each parameter value should appear in the rendered output (possibly HTML-escaped)
        foreach (var value in input.Parameters.Values)
        {
            var strValue = value.ToString()!;
            var escaped = System.Net.WebUtility.HtmlEncode(strValue);
            Assert.True(result.Contains(strValue) || result.Contains(escaped),
                $"Expected rendered output to contain '{strValue}' or its escaped form '{escaped}'. Actual: '{result}'");
        }
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(RenderingRoundTripArbitraries)])]
    public void Render_WithAllParams_ProducesNonEmptyOutput(RenderableTemplateInput input)
    {
        var compiled = _engine.Compile(input.Content);
        var result = _engine.Render(compiled, input.Parameters);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_SimpleTemplate_ProducesExpectedOutput()
    {
        var content = "Hello {{name}}, welcome to {{company}}!";
        var compiled = _engine.Compile(content);
        var parameters = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["company"] = "CoreMS"
        };

        var result = _engine.Render(compiled, parameters);

        Assert.Equal("Hello Alice, welcome to CoreMS!", result);
        Assert.DoesNotContain("{{name}}", result);
        Assert.DoesNotContain("{{company}}", result);
    }

    [Fact]
    public void Render_WithBlockHelpers_ResolvesAllPlaceholders()
    {
        var content = "{{#if active}}Welcome {{name}}!{{/if}}";
        var compiled = _engine.Compile(content);
        var parameters = new Dictionary<string, object>
        {
            ["active"] = true,
            ["name"] = "Bob"
        };

        var result = _engine.Render(compiled, parameters);

        Assert.Equal("Welcome Bob!", result);
        Assert.DoesNotContain("{{name}}", result);
    }

    #endregion
}

#region Arbitraries

public record RenderableTemplateInput(string Content, Dictionary<string, object> Parameters)
{
    public override string ToString() => Content;
}

public class RenderingRoundTripArbitraries
{
    private static readonly string[] ParamPool = ["name", "email", "company", "title", "city", "greeting", "message"];
    private static readonly string[] ValuePool = ["Alice", "Bob", "TestCorp", "Engineer", "Seattle", "Hello", "Welcome"];

    public static Arbitrary<RenderableTemplateInput> RenderableTemplateInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(1, int.MaxValue);
        Gen<RenderableTemplateInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var count = rng.Next(1, 4);
            var selectedParams = ParamPool.OrderBy(_ => rng.Next()).Take(count).ToList();

            // Build template with static text interspersed with placeholders
            var parts = selectedParams.Select(p => "{{" + p + "}}");
            var content = "Dear " + string.Join(", ", parts) + ". Thank you!";

            // Build matching parameter dictionary
            var parameters = selectedParams.ToDictionary(
                p => p,
                _ => (object)ValuePool[rng.Next(ValuePool.Length)]);

            return new RenderableTemplateInput(content, parameters);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
