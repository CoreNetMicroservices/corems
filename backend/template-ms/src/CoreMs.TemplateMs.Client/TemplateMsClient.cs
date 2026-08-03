using System.Net.Http.Json;
using System.Text.Json;
using CoreMs.Common.Exceptions;

namespace CoreMs.TemplateMs.Client;

/// <summary>
/// Typed HTTP client for calling template-ms endpoints.
/// Automatically forwards JWT and correlation ID via ServiceAuthDelegatingHandler.
/// </summary>
public class TemplateMsClient(HttpClient http)
{
    /// <summary>
    /// Render a template with parameter substitution.
    /// </summary>
    public async Task<TemplateRenderResult?> RenderTemplateAsync(
        string templateId,
        Dictionary<string, object>? parameters = null,
        string? language = null,
        CancellationToken ct = default)
    {
        var request = new
        {
            templateId,
            language = language ?? "en",
            parameters = parameters ?? new Dictionary<string, object>()
        };

        var response = await http.PostAsJsonAsync("/api/templates/render", request, ct);
        await EnsureSuccessOrThrowAsync(response, $"render template '{templateId}'", ct);
        return await response.Content.ReadFromJsonAsync<TemplateRenderResult>(ct);
    }

    /// <summary>
    /// Get template metadata (paramSchema, required params) by templateId and language.
    /// </summary>
    public async Task<TemplateMetadataResult?> GetTemplateMetadataAsync(
        string templateId,
        string? language = null,
        CancellationToken ct = default)
    {
        var lang = language ?? "en";
        var response = await http.GetAsync($"/api/templates/{templateId}/{lang}/metadata", ct);
        await EnsureSuccessOrThrowAsync(response, $"get metadata for template '{templateId}'", ct);
        return await response.Content.ReadFromJsonAsync<TemplateMetadataResult>(ct);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        var details = TryExtractErrorDetails(body) ?? body;
        var statusCode = (int)response.StatusCode;

        throw ServiceException.Of(
            new ErrorInfo("template.client_error", statusCode, $"Failed to {operation}"),
            details);
    }

    private static string? TryExtractErrorDetails(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
            {
                var first = errors[0];
                var desc = first.TryGetProperty("description", out var d) ? d.GetString() : null;
                var det = first.TryGetProperty("details", out var dt) ? dt.GetString() : null;
                return det ?? desc;
            }
        }
        catch { /* not JSON or unexpected format */ }
        return null;
    }
}

public record TemplateRenderResult(string RenderedContent);

public record TemplateMetadataResult(
    string TemplateId,
    string Language,
    string Name,
    string? Description,
    string Category,
    Dictionary<string, object>? ParamSchema,
    IReadOnlyList<string> RequiredParameters);
