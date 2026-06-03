using CoreMs.UserMs.Core.Configuration;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Repositories;
using CoreMs.UserMs.Core.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CoreMs.UserMs.Tests.Services;

/// <summary>
/// Property 22: Token Cleanup Removes Only Expired Tokens
/// For any set of tokens in the database, after the cleanup service runs,
/// all tokens with ExpiresAt less than current time SHALL be deleted,
/// and all tokens with ExpiresAt >= current time SHALL remain.
///
/// Properties tested:
/// 1. Cleanup always calls all three repositories (DeleteExpiredAsync on each)
/// 2. Cleanup handles partial failures gracefully (exception propagates)
/// 3. Cleanup respects CancellationToken (token is passed through)
///
/// **Validates: Requirements 12.1, 12.2, 12.3**
/// </summary>
public class TokenCleanupPropertyTests
{
    #region Property 22: Cleanup Always Calls All Three Repositories

    /// <summary>
    /// For any invocation of CleanupExpiredTokensAsync, all three repository
    /// DeleteExpiredAsync methods are called exactly once.
    /// Validates: Requirements 12.1, 12.2, 12.3
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(CleanupArbitraries)])]
    public async Task Cleanup_AlwaysCallsAllThreeRepositories(CleanupInvocationInput input)
    {
        var dbContext = Substitute.For<DbContext>();
        var loginTokenRepo = Substitute.For<LoginTokenRepository>(dbContext);
        var actionTokenRepo = Substitute.For<ActionTokenRepository>(dbContext);
        var authCodeRepo = Substitute.For<AuthorizationCodeRepository>(dbContext);
        var options = Options.Create(CreateTestTokenOptions());

        var service = new TokenService(loginTokenRepo, actionTokenRepo, authCodeRepo, options);

        await service.CleanupExpiredTokensAsync(input.CancellationToken);

        await loginTokenRepo.Received(1).DeleteExpiredAsync(input.CancellationToken);
        await actionTokenRepo.Received(1).DeleteExpiredAsync(input.CancellationToken);
        await authCodeRepo.Received(1).DeleteExpiredAsync(input.CancellationToken);
    }

    #endregion

    #region Property 22: Cleanup Handles Partial Failures Gracefully

    /// <summary>
    /// If the login token repository throws, the exception propagates
    /// and subsequent repositories are not called.
    /// Validates: Requirements 12.1, 12.2, 12.3
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(CleanupArbitraries)])]
    public async Task Cleanup_WhenLoginTokenRepoThrows_ExceptionPropagates(CleanupExceptionInput input)
    {
        var dbContext = Substitute.For<DbContext>();
        var loginTokenRepo = Substitute.For<LoginTokenRepository>(dbContext);
        var actionTokenRepo = Substitute.For<ActionTokenRepository>(dbContext);
        var authCodeRepo = Substitute.For<AuthorizationCodeRepository>(dbContext);
        var options = Options.Create(CreateTestTokenOptions());

        loginTokenRepo.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(input.ErrorMessage));

        var service = new TokenService(loginTokenRepo, actionTokenRepo, authCodeRepo, options);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CleanupExpiredTokensAsync());

        Assert.Equal(input.ErrorMessage, ex.Message);
    }

    /// <summary>
    /// If the action token repository throws, the exception propagates.
    /// Login tokens are still cleaned (called before), but auth codes are not.
    /// Validates: Requirements 12.1, 12.2, 12.3
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(CleanupArbitraries)])]
    public async Task Cleanup_WhenActionTokenRepoThrows_ExceptionPropagates(CleanupExceptionInput input)
    {
        var dbContext = Substitute.For<DbContext>();
        var loginTokenRepo = Substitute.For<LoginTokenRepository>(dbContext);
        var actionTokenRepo = Substitute.For<ActionTokenRepository>(dbContext);
        var authCodeRepo = Substitute.For<AuthorizationCodeRepository>(dbContext);
        var options = Options.Create(CreateTestTokenOptions());

        actionTokenRepo.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(input.ErrorMessage));

        var service = new TokenService(loginTokenRepo, actionTokenRepo, authCodeRepo, options);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CleanupExpiredTokensAsync());

        Assert.Equal(input.ErrorMessage, ex.Message);
        await loginTokenRepo.Received(1).DeleteExpiredAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// If the authorization code repository throws, the exception propagates.
    /// Login tokens and action tokens are still cleaned (called before).
    /// Validates: Requirements 12.1, 12.2, 12.3
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(CleanupArbitraries)])]
    public async Task Cleanup_WhenAuthCodeRepoThrows_ExceptionPropagates(CleanupExceptionInput input)
    {
        var dbContext = Substitute.For<DbContext>();
        var loginTokenRepo = Substitute.For<LoginTokenRepository>(dbContext);
        var actionTokenRepo = Substitute.For<ActionTokenRepository>(dbContext);
        var authCodeRepo = Substitute.For<AuthorizationCodeRepository>(dbContext);
        var options = Options.Create(CreateTestTokenOptions());

        authCodeRepo.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(input.ErrorMessage));

        var service = new TokenService(loginTokenRepo, actionTokenRepo, authCodeRepo, options);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CleanupExpiredTokensAsync());

        Assert.Equal(input.ErrorMessage, ex.Message);
        await loginTokenRepo.Received(1).DeleteExpiredAsync(Arg.Any<CancellationToken>());
        await actionTokenRepo.Received(1).DeleteExpiredAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Property 22: Cleanup Respects CancellationToken

    /// <summary>
    /// For any CancellationToken passed to CleanupExpiredTokensAsync,
    /// the same token is forwarded to all three repository calls.
    /// Validates: Requirements 12.1, 12.2, 12.3
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(CleanupArbitraries)])]
    public async Task Cleanup_AlwaysPassesCancellationTokenToAllRepositories(CleanupInvocationInput input)
    {
        var dbContext = Substitute.For<DbContext>();
        var loginTokenRepo = Substitute.For<LoginTokenRepository>(dbContext);
        var actionTokenRepo = Substitute.For<ActionTokenRepository>(dbContext);
        var authCodeRepo = Substitute.For<AuthorizationCodeRepository>(dbContext);
        var options = Options.Create(CreateTestTokenOptions());

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var service = new TokenService(loginTokenRepo, actionTokenRepo, authCodeRepo, options);

        await service.CleanupExpiredTokensAsync(ct);

        await loginTokenRepo.Received(1).DeleteExpiredAsync(ct);
        await actionTokenRepo.Received(1).DeleteExpiredAsync(ct);
        await authCodeRepo.Received(1).DeleteExpiredAsync(ct);
    }

    #endregion

    #region Helpers

    private static TokenServiceOptions CreateTestTokenOptions() => new()
    {
        Issuer = "http://test-issuer",
        Audience = "test-audience",
        SecretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!",
        AccessTokenExpirationMinutes = 10,
        RefreshTokenExpirationMinutes = 1440,
        IdTokenExpirationMinutes = 60
    };

    #endregion
}

#region Custom Types and Arbitraries for Cleanup Tests

public record CleanupInvocationInput(int Seed)
{
    public CancellationToken CancellationToken => CancellationToken.None;
    public override string ToString() => $"Invocation(seed={Seed})";
}

public record CleanupExceptionInput(string ErrorMessage)
{
    public override string ToString() => $"Exception({ErrorMessage})";
}

public class CleanupArbitraries
{
    public static Arbitrary<CleanupInvocationInput> CleanupInvocationInputArbitrary()
    {
        Gen<CleanupInvocationInput> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Choose(0, int.MaxValue),
            seed => new CleanupInvocationInput(seed));
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<CleanupExceptionInput> CleanupExceptionInputArbitrary()
    {
        var messages = new[]
        {
            "Database connection failed",
            "Timeout expired",
            "Deadlock detected",
            "Connection pool exhausted",
            "Network error",
            "Permission denied",
            "Transaction aborted",
            "Disk full"
        };
        Gen<CleanupExceptionInput> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements(messages),
            msg => new CleanupExceptionInput(msg));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
