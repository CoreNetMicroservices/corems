namespace CoreMs.Common.Exceptions;

/// <summary>
/// Common error definitions shared across all CoreMS services.
/// </summary>
public static class DefaultErrors
{
    public static readonly ErrorInfo ServerError = new("server.error", 500, "Unexpected error. Try again later.");
    public static readonly ErrorInfo NotImplemented = new("server.not_implemented", 501, "This functionality is not implemented yet.");

    public static readonly ErrorInfo Unauthorized = new("user.unauthorized", 401, "User is unauthorized");
    public static readonly ErrorInfo AccessDenied = new("user.access_denied", 401, "Access is denied");
    public static readonly ErrorInfo Forbidden = new("user.forbidden", 403, "Access to this resource is forbidden");
    public static readonly ErrorInfo NotFound = new("resource.not.found", 404, "Resource is not found");

    public static readonly ErrorInfo InvalidRequest = new("invalid.request", 400, "Invalid request");
    public static readonly ErrorInfo InvalidInput = new("invalid.data", 400, "Invalid input data");
    public static readonly ErrorInfo ValidationFailed = new("validation.failed", 400, "Validation failed");
    public static readonly ErrorInfo FormatInvalid = new("format.invalid", 400, "Invalid format");
    public static readonly ErrorInfo ParameterMissing = new("request.parameter.missing", 400, "Request parameter is missing");
    public static readonly ErrorInfo Conflict = new("resource.conflict", 409, "Resource conflict detected");

    public static readonly ErrorInfo RequestCancelled = new("request.cancelled", 499, "Request was cancelled");
}
