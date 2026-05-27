using System.Text.Json;
using CoreMs.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreMs.Common.Middleware;

/// <summary>
/// Global exception handler that converts all exceptions to a consistent
/// { errors: [{ reasonCode, description, details }] } JSON response format.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, response) = exception switch
        {
            ServiceException serviceEx => HandleServiceException(serviceEx),
            ValidationException validationEx => HandleValidationException(validationEx),
            JsonException jsonEx => HandleJsonException(jsonEx),
            BadHttpRequestException badRequestEx => HandleBadHttpRequestException(badRequestEx),
            OperationCanceledException => HandleCancelledException(),
            UnauthorizedAccessException => HandleUnauthorizedException(),
            _ => HandleUnknownException(exception, httpContext)
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private (int statusCode, ErrorResponse response) HandleServiceException(ServiceException ex)
    {
        if (ex.HttpStatusCode >= 500)
            logger.LogError("Service exception: {Errors}", ex.Errors);
        else
            logger.LogWarning("Service exception: {Errors}", ex.Errors);

        return (ex.HttpStatusCode, ErrorResponse.Of(ex.Errors));
    }

    private static (int statusCode, ErrorResponse response) HandleValidationException(ValidationException ex)
    {
        var errors = ex.Errors
            .Select(e => Error.Of(
                DefaultErrors.ValidationFailed.ErrorCode,
                e.ErrorMessage,
                e.PropertyName))
            .ToList();

        return (DefaultErrors.ValidationFailed.HttpStatusCode, ErrorResponse.Of(errors));
    }

    private static (int statusCode, ErrorResponse response) HandleJsonException(JsonException ex)
    {
        var error = Error.Of(DefaultErrors.FormatInvalid.ErrorCode, DefaultErrors.FormatInvalid.Description, ex.Message);
        return (DefaultErrors.FormatInvalid.HttpStatusCode, ErrorResponse.Of(error));
    }

    private static (int statusCode, ErrorResponse response) HandleBadHttpRequestException(BadHttpRequestException ex)
    {
        var error = Error.Of(DefaultErrors.InvalidRequest.ErrorCode, DefaultErrors.InvalidRequest.Description, ex.Message);
        return (ex.StatusCode, ErrorResponse.Of(error));
    }

    private static (int statusCode, ErrorResponse response) HandleCancelledException()
    {
        var error = Error.Of(DefaultErrors.RequestCancelled.ErrorCode, DefaultErrors.RequestCancelled.Description);
        return (DefaultErrors.RequestCancelled.HttpStatusCode, ErrorResponse.Of(error));
    }

    private static (int statusCode, ErrorResponse response) HandleUnauthorizedException()
    {
        var error = Error.Of(DefaultErrors.Unauthorized.ErrorCode, DefaultErrors.Unauthorized.Description);
        return (DefaultErrors.Unauthorized.HttpStatusCode, ErrorResponse.Of(error));
    }

    private (int statusCode, ErrorResponse response) HandleUnknownException(Exception ex, HttpContext context)
    {
        logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);

        var error = Error.Of(DefaultErrors.ServerError.ErrorCode, DefaultErrors.ServerError.Description);
        return (DefaultErrors.ServerError.HttpStatusCode, ErrorResponse.Of(error));
    }
}
