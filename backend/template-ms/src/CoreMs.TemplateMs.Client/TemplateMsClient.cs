using System.Net.Http.Json;

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
        response.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TemplateMetadataResult>(ct);
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
