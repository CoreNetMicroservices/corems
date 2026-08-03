using System.Net.Http.Json;
using CoreMs.Common.Http;
using CoreMs.Common.Security;

namespace CoreMs.DocumentMs.Client;

/// <summary>
/// Typed HTTP client for calling document-ms endpoints.
/// Auth is handled by ServiceAuthDelegatingHandler for normal HTTP flows.
/// For background flows, uses ServiceCallContext + TokenProvider to mint a token per request.
/// </summary>
public class DocumentMsClient(HttpClient http, ServiceCallContext serviceCallContext, TokenProvider tokenProvider)
{
    /// <summary>
    /// Get document metadata by UUID.
    /// </summary>
    public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Guid uuid, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/documents/{uuid}");
        ApplyAuth(request);

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DocumentMetadata>(ct);
    }

    /// <summary>
    /// Download document content as a stream.
    /// </summary>
    public async Task<DocumentDownload?> DownloadDocumentAsync(Guid uuid, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/documents/{uuid}/download");
        ApplyAuth(request);

        var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) return null;

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        var filename = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"{uuid}";

        return new DocumentDownload(stream, contentType, filename);
    }

    /// <summary>
    /// Applies auth to the request. In background flows where ServiceCallContext has an actor,
    /// mints a fresh token directly on the request (bypassing DelegatingHandler scope issues).
    /// </summary>
    private void ApplyAuth(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(serviceCallContext.ActorUserId))
        {
            var claims = new Dictionary<string, object>();
            if (serviceCallContext.ActorRoles is { Count: > 0 })
                claims["role"] = serviceCallContext.ActorRoles;

            var token = tokenProvider.CreateCustomToken("service_call", serviceCallContext.ActorUserId, claims, 5);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        }
    }
}

public record DocumentMetadata
{
    public Guid Uuid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string OriginalFilename { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Extension { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string? Checksum { get; init; }
}

public record DocumentDownload(Stream Stream, string ContentType, string Filename) : IDisposable
{
    public void Dispose() => Stream.Dispose();
}
