namespace CoreMs.Common.Exceptions;

/// <summary>
/// Standard exception for all business logic errors across CoreMS services.
/// Holds a list of errors and the HTTP status code for the response.
/// </summary>
public class ServiceException : Exception
{
    public int HttpStatusCode { get; }
    public IReadOnlyList<Error> Errors { get; }

    private ServiceException(int httpStatusCode, IReadOnlyList<Error> errors)
        : base(errors[0].Description)
    {
        HttpStatusCode = httpStatusCode;
        Errors = errors;
    }

    /// <summary>
    /// Creates a ServiceException from an ErrorInfo with optional detail message.
    /// </summary>
    public static ServiceException Of(ErrorInfo errorInfo, string? details = null)
    {
        var error = new Error(errorInfo.ErrorCode, errorInfo.Description, details);
        return new ServiceException(errorInfo.HttpStatusCode, [error]);
    }

    /// <summary>
    /// Creates a ServiceException with multiple errors.
    /// </summary>
    public static ServiceException Of(IReadOnlyList<Error> errors, int httpStatusCode)
    {
        return new ServiceException(httpStatusCode, errors);
    }
}
