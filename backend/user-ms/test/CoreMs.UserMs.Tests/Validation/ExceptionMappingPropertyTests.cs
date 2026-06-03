using CoreMs.Common.Exceptions;
using CoreMs.Common.Middleware;
using FluentValidation;
using FluentValidation.Results;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CoreMs.UserMs.Tests.Validation;

/// <summary>
/// Property 20: Exception-to-HTTP Status Code Mapping
/// For any domain exception thrown during request processing, the Exception Handler SHALL map it
/// to the correct HTTP status code and SHALL never expose internal details for unhandled exceptions.
///
/// **Validates: Requirements 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 13.9**
/// </summary>
public class ExceptionMappingPropertyTests
{
    private readonly GlobalExceptionHandler _handler;

    public ExceptionMappingPropertyTests()
    {
        var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(logger);
    }

    /// <summary>
    /// Property 20: For any ServiceException with HTTP status code N, the response status is always N.
    /// Validates: Requirements 13.1–13.7
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ExceptionArbitraries)])]
    public void ServiceException_AlwaysMapsToItsStatusCode(ServiceExceptionInput input)
    {
        var context = CreateFreshContext();
        var exception = ServiceException.Of(
            new ErrorInfo(input.ErrorCode, input.StatusCode, input.Description));

        _handler.TryHandleAsync(context, exception, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        Assert.Equal(input.StatusCode, context.Response.StatusCode);
    }

    /// <summary>
    /// Property 20: ValidationException always maps to 400.
    /// Validates: Requirement 13.8
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(ExceptionArbitraries)])]
    public void ValidationException_AlwaysMapsTo400(ValidationExceptionInput input)
    {
        var context = CreateFreshContext();
        var failures = input.FieldNames
            .Select(f => new ValidationFailure(f, $"'{f}' is invalid"))
            .ToList();
        var exception = new ValidationException(failures);

        _handler.TryHandleAsync(context, exception, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        Assert.Equal(400, context.Response.StatusCode);
    }

    /// <summary>
    /// Property 20: UnauthorizedAccessException always maps to 401.
    /// Validates: Requirement 13.1
    /// </summary>
    [Property(MaxTest = 50)]
    public void UnauthorizedAccessException_AlwaysMapsTo401(NonEmptyString message)
    {
        var context = CreateFreshContext();
        var exception = new UnauthorizedAccessException(message.Get);

        _handler.TryHandleAsync(context, exception, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        Assert.Equal(401, context.Response.StatusCode);
    }

    /// <summary>
    /// Property 20: Unknown exceptions always map to 500.
    /// Validates: Requirement 13.9
    /// </summary>
    [Property(MaxTest = 50)]
    public void UnknownException_AlwaysMapsTo500(NonEmptyString message)
    {
        var context = CreateFreshContext();
        var exception = new InvalidOperationException(message.Get);

        _handler.TryHandleAsync(context, exception, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        Assert.Equal(500, context.Response.StatusCode);
    }

    /// <summary>
    /// Property 20: Unknown exceptions never expose internal message in response body.
    /// Validates: Requirement 13.9
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(ExceptionArbitraries)])]
    public void UnknownException_NeverExposesInternalDetails(SecretMessage secretMessage)
    {
        var context = CreateFreshContext();
        var exception = new InvalidOperationException(secretMessage.Value);

        _handler.TryHandleAsync(context, exception, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = reader.ReadToEnd();

        Assert.DoesNotContain(secretMessage.Value, body);
    }

    /// <summary>
    /// Property 20: DbUpdateException with unique constraint violation message maps to 409.
    /// Validates: Requirement 13.5
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(ExceptionArbitraries)])]
    public void DbUpdateException_WithUniqueViolation_MapsTo409(UniqueConstraintMessage message)
    {
        var context = CreateFreshContext();
        var innerException = new Exception(message.Value);
        var dbException = new DbUpdateException("Error saving", innerException);

        _handler.TryHandleAsync(context, dbException, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        Assert.Equal(409, context.Response.StatusCode);
    }

    /// <summary>
    /// Property 20: OperationCanceledException maps to 499.
    /// </summary>
    [Fact]
    public async Task OperationCanceledException_MapsTo499()
    {
        var context = CreateFreshContext();
        var exception = new OperationCanceledException();

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.Equal(499, context.Response.StatusCode);
    }

    /// <summary>
    /// Verifies specific domain error codes map to expected status codes.
    /// </summary>
    [Theory]
    [InlineData("auth.invalid_credentials", 401)]
    [InlineData("auth.account_disabled", 403)]
    [InlineData("auth.email_not_verified", 403)]
    [InlineData("user.not_found", 404)]
    [InlineData("user.exists", 409)]
    [InlineData("auth.token_expired", 410)]
    [InlineData("auth.token_consumed", 410)]
    public async Task KnownServiceExceptions_MapToExpectedStatusCodes(string errorCode, int expectedStatus)
    {
        var context = CreateFreshContext();
        var exception = ServiceException.Of(new ErrorInfo(errorCode, expectedStatus, "Test error"));

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateFreshContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}

#region Custom Types and Arbitraries

/// <summary>Input for generating ServiceExceptions with various status codes.</summary>
public record ServiceExceptionInput(string ErrorCode, int StatusCode, string Description)
{
    public override string ToString() => $"{ErrorCode} -> {StatusCode}";
}

/// <summary>Input for generating ValidationExceptions with various fields.</summary>
public record ValidationExceptionInput(string[] FieldNames)
{
    public override string ToString() => $"[{string.Join(", ", FieldNames)}]";
}

/// <summary>A string containing a unique constraint violation indicator.</summary>
public record UniqueConstraintMessage(string Value)
{
    public override string ToString() => Value;
}

/// <summary>A unique secret message that won't appear in standard error responses.</summary>
public record SecretMessage(string Value)
{
    public override string ToString() => Value;
}

public class ExceptionArbitraries
{
    private static readonly int[] ValidStatusCodes = [400, 401, 403, 404, 409, 410, 422, 429, 500, 502, 503];
    private static readonly string[] Prefixes = ["user", "auth", "resource", "server"];
    private static readonly string[] Suffixes = ["not_found", "exists", "invalid", "error", "forbidden"];
    private static readonly string[] Descriptions = ["Error occurred", "Not found", "Access denied", "Invalid input"];
    private static readonly string[] Fields = ["Email", "Password", "FirstName", "LastName", "PhoneNumber", "Token", "ConfirmPassword"];
    private static readonly string[] UniqueViolationMessages =
    [
        "duplicate key value violates unique constraint \"ix_app_user_email\"",
        "duplicate key value violates unique constraint \"ix_app_user_uuid\"",
        "unique constraint violation on column 'email'",
        "UNIQUE constraint failed: app_user.email",
        "duplicate key value violates unique constraint \"ix_login_token_token\""
    ];

    public static Arbitrary<ServiceExceptionInput> ServiceExceptionInputArbitrary()
    {
        // Use a seed to pick status code, prefix, suffix, and description
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<ServiceExceptionInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var status = ValidStatusCodes[rng.Next(ValidStatusCodes.Length)];
            var prefix = Prefixes[rng.Next(Prefixes.Length)];
            var suffix = Suffixes[rng.Next(Suffixes.Length)];
            var desc = Descriptions[rng.Next(Descriptions.Length)];
            return new ServiceExceptionInput($"{prefix}.{suffix}", status, desc);
        });

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<ValidationExceptionInput> ValidationExceptionInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<ValidationExceptionInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var count = rng.Next(1, 5);
            var fields = Enumerable.Range(0, count)
                .Select(_ => Fields[rng.Next(Fields.Length)])
                .Distinct()
                .ToArray();
            return new ValidationExceptionInput(fields);
        });

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<UniqueConstraintMessage> UniqueConstraintMessageArbitrary()
    {
        Gen<UniqueConstraintMessage> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements(UniqueViolationMessages),
            msg => new UniqueConstraintMessage(msg));

        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<SecretMessage> SecretMessageArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<SecretMessage> gen = FsCheck.Fluent.Gen.Select(seedGen,
            seed => new SecretMessage($"SECRET_INTERNAL_ERROR_{seed:X8}_DETAILS"));

        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
