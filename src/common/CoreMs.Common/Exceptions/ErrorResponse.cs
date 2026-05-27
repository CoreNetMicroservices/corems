namespace CoreMs.Common.Exceptions;

/// <summary>
/// Standard error response returned to clients. Contains a list of errors.
/// </summary>
public record ErrorResponse(IReadOnlyList<Error> Errors)
{
    public static ErrorResponse Of(Error error) => new([error]);
    public static ErrorResponse Of(IReadOnlyList<Error> errors) => new(errors);
}
