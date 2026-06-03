namespace CoreMs.Common.Exceptions;

/// <summary>
/// Defines an error reason code with its HTTP status and description.
/// Used as static readonly fields in error definition classes.
/// </summary>
public record ErrorInfo(string ErrorCode, int HttpStatusCode, string Description);
