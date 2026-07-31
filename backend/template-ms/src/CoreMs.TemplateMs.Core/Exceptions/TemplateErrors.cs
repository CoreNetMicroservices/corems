using CoreMs.Common.Exceptions;

namespace CoreMs.TemplateMs.Core.Exceptions;

public static class TemplateErrors
{
    public static readonly ErrorInfo TemplateNotFound = new("template.not_found", 404, "Template not found");
    public static readonly ErrorInfo TemplateAlreadyExists = new("template.already_exists", 409, "Template with this ID and language already exists");
    public static readonly ErrorInfo InvalidTemplateSyntax = new("template.invalid_syntax", 400, "Template contains invalid Handlebars syntax");
    public static readonly ErrorInfo MissingRequiredParameters = new("template.missing_parameters", 400, "Required template parameters are missing");
    public static readonly ErrorInfo RenderingFailed = new("template.rendering_failed", 500, "Template rendering failed");
}
