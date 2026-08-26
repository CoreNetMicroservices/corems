using System.Net.Http.Json;
using System.Text.Json;
using CoreMs.Common.Exceptions;

namespace CoreMs.UserMs.Client;

/// <summary>
/// Typed HTTP client for calling user-ms endpoints from other services.
/// Automatically forwards JWT and correlation ID via ServiceAuthDelegatingHandler.
/// </summary>
public class UserMsClient(HttpClient http)
{
    /// <summary>
    /// Get a user by their UUID (admin endpoint).
    /// </summary>
    public async Task<UserInfoResult?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/users/{userId}", ct);
        await EnsureSuccessOrThrowAsync(response, $"get user '{userId}'", ct);
        return await response.Content.ReadFromJsonAsync<UserInfoResult>(ct);
    }

    /// <summary>
    /// Get the current authenticated user's profile (via forwarded token).
    /// </summary>
    public async Task<UserInfoResult?> GetCurrentProfileAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("/api/profile", ct);
        await EnsureSuccessOrThrowAsync(response, "get current profile", ct);
        return await response.Content.ReadFromJsonAsync<UserInfoResult>(ct);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        var details = TryExtractErrorDetails(body) ?? body;
        var statusCode = (int)response.StatusCode;

        throw ServiceException.Of(
            new ErrorInfo("user.client_error", statusCode, $"Failed to {operation}"),
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

/// <summary>User info returned by user-ms.</summary>
public record UserInfoResult
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}
