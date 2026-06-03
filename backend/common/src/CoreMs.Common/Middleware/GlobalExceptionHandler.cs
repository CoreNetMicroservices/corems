using System.Text.Json;
using CoreMs.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
            DbUpdateException dbEx => HandleDbUpdateException(dbEx),
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

    private (int statusCode, ErrorResponse response) HandleDbUpdateException(DbUpdateException ex)
    {
        // Unique constraint violations: PostgreSQL SQLSTATE 23505
        if (IsUniqueConstraintViolation(ex))
        {
            logger.LogWarning("Database constraint violation: {Message}", ex.InnerException?.Message ?? ex.Message);
            var error = Error.Of(DefaultErrors.Conflict.ErrorCode, DefaultErrors.Conflict.Description);
            return (DefaultErrors.Conflict.HttpStatusCode, ErrorResponse.Of(error));
        }

        logger.LogError(ex, "Database update exception");
        var serverError = Error.Of(DefaultErrors.ServerError.ErrorCode, DefaultErrors.ServerError.Description);
        return (DefaultErrors.ServerError.HttpStatusCode, ErrorResponse.Of(serverError));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL unique constraint violation — SQLSTATE 23505
        var inner = ex.InnerException;
        if (inner == null) return false;

        var typeName = inner.GetType().Name;
        if (typeName == "PostgresException")
        {
            // Npgsql.PostgresException has a SqlState property
            var sqlStateProp = inner.GetType().GetProperty("SqlState");
            if (sqlStateProp?.GetValue(inner) is string sqlState)
                return sqlState == "23505";
        }

        // Fallback: check message for common constraint violation indicators
        var message = inner.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
               || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
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
