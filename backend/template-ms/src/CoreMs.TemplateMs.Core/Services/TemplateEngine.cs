using System.Text.RegularExpressions;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.TemplateMs.Core.Exceptions;
using HandlebarsDotNet;

namespace CoreMs.TemplateMs.Core.Services;

[Service]
public class TemplateEngine
{
    private static readonly Regex ParameterRegex = new(
        @"\{\{(?:#(?:if|each|unless|with)\s+)?([a-zA-Z_][a-zA-Z0-9_.]*)\s*\}\}",
        RegexOptions.Compiled);

    private static readonly HashSet<string> BuiltinKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "this", "else", "true", "false", "null", "undefined"
    };

    public HandlebarsTemplate<object, object> Compile(string content)
    {
        return Handlebars.Compile(content);
    }

    public void ValidateSyntax(string content)
    {
        try
        {
            Handlebars.Compile(content);
        }
        catch (HandlebarsParserException ex)
        {
            throw ServiceException.Of(TemplateErrors.InvalidTemplateSyntax, ex.Message);
        }
        catch (HandlebarsCompilerException ex)
        {
            throw ServiceException.Of(TemplateErrors.InvalidTemplateSyntax, ex.Message);
        }
        catch (Exception ex) when (ex is not ServiceException)
        {
            throw ServiceException.Of(TemplateErrors.InvalidTemplateSyntax, ex.Message);
        }
    }

    public IReadOnlyList<string> ExtractParameters(string content)
    {
        var parameters = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in ParameterRegex.Matches(content))
        {
            var name = match.Groups[1].Value;

            if (name.StartsWith('@') || name.StartsWith('/') || name == "." || BuiltinKeywords.Contains(name))
                continue;

            var dotIndex = name.IndexOf('.');
            var rootName = dotIndex > 0 ? name[..dotIndex] : name;

            parameters.Add(rootName);
        }

        return parameters.Order().ToList();
    }

    public string Render(HandlebarsTemplate<object, object> compiledTemplate, Dictionary<string, object> parameters)
    {
        return compiledTemplate(parameters);
    }
}
