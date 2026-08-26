using System.Net.Http.Json;
using System.Text.Json;
using CoreMs.Common.Exceptions;

namespace CoreMs.TranslationMs.Client;

/// <summary>
/// Typed HTTP client for calling translation-ms endpoints from other services.
/// Automatically forwards JWT and correlation ID via ServiceAuthDelegatingHandler.
/// </summary>
public class TranslationMsClient(HttpClient http)
{
    /// <summary>
    /// Get translation data (key→value map) for a realm and language.
    /// </summary>
    public async Task<Dictionary<string, string>?> GetTranslationsAsync(
        string realm, string language, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/translation/{realm}/{language}", ct);
        await EnsureSuccessOrThrowAsync(response, $"get translations for '{realm}/{language}'", ct);
        return await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(ct);
    }

    /// <summary>
    /// Get available languages for a realm.
    /// </summary>
    public async Task<List<string>?> GetLanguagesAsync(string realm, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/languages/{realm}", ct);
        await EnsureSuccessOrThrowAsync(response, $"get languages for realm '{realm}'", ct);
        return await response.Content.ReadFromJsonAsync<List<string>>(ct);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        var details = TryExtractErrorDetails(body) ?? body;
        var statusCode = (int)response.StatusCode;

        throw ServiceException.Of(
            new ErrorInfo("translation.client_error", statusCode, $"Failed to {operation}"),
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
