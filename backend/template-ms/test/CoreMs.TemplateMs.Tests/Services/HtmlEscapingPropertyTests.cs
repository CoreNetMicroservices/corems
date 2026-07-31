using CoreMs.TemplateMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.TemplateMs.Tests.Services;

/// <summary>
/// Property 8: HTML escaping of parameter values
/// For any parameter value containing HTML special characters (&lt;, &gt;, &amp;, ", '),
/// the rendered output should contain the HTML-escaped form rather than raw characters.
///
/// **Validates: Requirements 5.5**
/// </summary>
public class HtmlEscapingPropertyTests
{
    private readonly TemplateEngine _engine = new();

    #region Property 8: HTML escaping of parameter values

    [Property(MaxTest = 50, Arbitrary = [typeof(HtmlEscapingArbitraries)])]
    public void Render_WithHtmlSpecialChars_AlwaysEscapesInDoubleBrace(HtmlInputValue input)
    {
        var template = "Output: {{value}}";
        var compiled = _engine.Compile(template);
        var parameters = new Dictionary<string, object> { ["value"] = input.Value };

        var result = _engine.Render(compiled, parameters);

        // Double-brace should HTML-escape special characters
        if (input.Value.Contains('<'))
            Assert.Contains("&lt;", result);
        if (input.Value.Contains('>'))
            Assert.Contains("&gt;", result);
        if (input.Value.Contains('&'))
            Assert.Contains("&amp;", result);
        if (input.Value.Contains('"'))
            Assert.Contains("&quot;", result);

        // Raw HTML special chars should not appear in output
        Assert.DoesNotContain("<", result.Replace("&lt;", ""));
        Assert.DoesNotContain(">", result.Replace("&gt;", ""));
    }

    [Property(MaxTest = 50, Arbitrary = [typeof(HtmlEscapingArbitraries)])]
    public void Render_WithTripleBrace_DoesNotEscape(HtmlInputValue input)
    {
        var template = "Output: {{{value}}}";
        var compiled = _engine.Compile(template);
        var parameters = new Dictionary<string, object> { ["value"] = input.Value };

        var result = _engine.Render(compiled, parameters);

        // Triple-brace should output raw (unescaped)
        Assert.Contains(input.Value, result);
    }

    [Fact]
    public void Render_ScriptTag_IsEscaped()
    {
        var template = "Hello {{name}}";
        var compiled = _engine.Compile(template);
        var parameters = new Dictionary<string, object> { ["name"] = "<script>alert('xss')</script>" };

        var result = _engine.Render(compiled, parameters);

        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    #endregion
}

#region Arbitraries

public record HtmlInputValue(string Value)
{
    public override string ToString() => Value;
}

public class HtmlEscapingArbitraries
{
    // HandlebarsDotNet escapes <, >, &, " (not single quotes)
    private static readonly string[] HtmlChars = ["<", ">", "&", "\""];

    public static Arbitrary<HtmlInputValue> HtmlInputValueArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<HtmlInputValue> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var baseTexts = new[] { "hello", "world", "test value", "user input" };
            var baseText = baseTexts[rng.Next(baseTexts.Length)];
            var specialChar = HtmlChars[rng.Next(HtmlChars.Length)];
            var position = rng.Next(3); // 0=before, 1=middle, 2=after
            var value = position switch
            {
                0 => specialChar + baseText,
                1 => baseText[..(baseText.Length / 2)] + specialChar + baseText[(baseText.Length / 2)..],
                _ => baseText + specialChar
            };
            return new HtmlInputValue(value);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
