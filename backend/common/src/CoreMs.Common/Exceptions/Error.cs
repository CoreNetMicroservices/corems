namespace CoreMs.Common.Exceptions;

/// <summary>
/// Represents a single error in the error response.
/// </summary>
public record Error(string ReasonCode, string Description, string? Details = null)
{
    public static Error Of(string reasonCode, string description, string? details = null)
        => new(reasonCode, description, details);

    public static Error Of(string reasonCode, string description)
        => new(reasonCode, description);
}
