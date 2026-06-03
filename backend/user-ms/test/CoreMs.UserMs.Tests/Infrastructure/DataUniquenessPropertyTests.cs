using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Enums;
using CoreMs.UserMs.Infrastructure.Data;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreMs.UserMs.Tests.Infrastructure;

/// <summary>
/// Property 23: Data Uniqueness Constraints
/// For any two distinct records in the same table, unique-indexed fields
/// (user email, user UUID, user phone where non-null, token values, authorization codes)
/// SHALL have distinct values.
///
/// **Validates: Requirements 14.1, 14.2, 14.3, 14.4**
/// </summary>
public class DataUniquenessPropertyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _options;

    public DataUniquenessPropertyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    private TestDbContext CreateContext() => new(_options);

    public void Dispose()
    {
        _connection.Dispose();
    }

    /// <summary>
    /// Requirement 14.1: Email uniqueness enforced via unique database index.
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(DataUniquenessArbitraries)])]
    public void DuplicateEmail_ShouldBeRejected(ValidEmail email)
    {
        using var context = CreateContext();

        var user1 = CreateValidUser();
        user1.Email = email.Value;
        context.Set<UserEntity>().Add(user1);
        context.SaveChanges();

        var user2 = CreateValidUser();
        user2.Email = email.Value;
        context.Set<UserEntity>().Add(user2);

        Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// Requirement 14.3: Phone uniqueness enforced via unique index.
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(DataUniquenessArbitraries)])]
    public void DuplicatePhoneNumber_ShouldBeRejected(ValidPhone phone)
    {
        using var context = CreateContext();

        var user1 = CreateValidUser();
        user1.PhoneNumber = phone.Value;
        context.Set<UserEntity>().Add(user1);
        context.SaveChanges();

        var user2 = CreateValidUser();
        user2.PhoneNumber = phone.Value;
        context.Set<UserEntity>().Add(user2);

        Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// Requirement 14.2: UUID uniqueness enforced via unique database index.
    /// </summary>
    [Property(MaxTest = 50)]
    public void DuplicateUserUuid_ShouldBeRejected(Guid uuid)
    {
        using var context = CreateContext();

        var user1 = CreateValidUser();
        user1.Uuid = uuid;
        context.Set<UserEntity>().Add(user1);
        context.SaveChanges();

        var user2 = CreateValidUser();
        user2.Uuid = uuid;
        context.Set<UserEntity>().Add(user2);

        Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// Requirement 14.4: Action token hash uniqueness enforced via unique index.
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(DataUniquenessArbitraries)])]
    public void DuplicateActionTokenHash_ShouldBeRejected(NonEmptyToken tokenHash)
    {
        using var context = CreateContext();

        var user = CreateAndSaveUser(context);

        var token1 = CreateValidActionToken(user.Id);
        token1.TokenHash = tokenHash.Value;
        context.Set<ActionTokenEntity>().Add(token1);
        context.SaveChanges();

        var token2 = CreateValidActionToken(user.Id);
        token2.TokenHash = tokenHash.Value;
        context.Set<ActionTokenEntity>().Add(token2);

        Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// Requirement 14.4: Authorization code uniqueness enforced via unique index.
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(DataUniquenessArbitraries)])]
    public void DuplicateAuthorizationCode_ShouldBeRejected(NonEmptyToken code)
    {
        using var context = CreateContext();

        var user = CreateAndSaveUser(context);

        var authCode1 = CreateValidAuthorizationCode(user.Id);
        authCode1.Code = code.Value;
        context.Set<AuthorizationCodeEntity>().Add(authCode1);
        context.SaveChanges();

        var authCode2 = CreateValidAuthorizationCode(user.Id);
        authCode2.Code = code.Value;
        context.Set<AuthorizationCodeEntity>().Add(authCode2);

        Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// Requirement 14.4: Login token value uniqueness enforced via unique index.
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(DataUniquenessArbitraries)])]
    public void DuplicateLoginToken_ShouldBeRejected(NonEmptyToken token)
    {
        using var context = CreateContext();

        var user = CreateAndSaveUser(context);

        var loginToken1 = CreateValidLoginToken(user.Id);
        loginToken1.Token = token.Value;
        context.Set<LoginTokenEntity>().Add(loginToken1);
        context.SaveChanges();

        var loginToken2 = CreateValidLoginToken(user.Id);
        loginToken2.Token = token.Value;
        context.Set<LoginTokenEntity>().Add(loginToken2);

        Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// Requirement 14.4: Login token UUID uniqueness enforced via unique index.
    /// </summary>
    [Property(MaxTest = 50)]
    public void DuplicateLoginTokenUuid_ShouldBeRejected(Guid uuid)
    {
        using var context = CreateContext();

        var user = CreateAndSaveUser(context);

        var loginToken1 = CreateValidLoginToken(user.Id);
        loginToken1.Uuid = uuid;
        context.Set<LoginTokenEntity>().Add(loginToken1);
        context.SaveChanges();

        var loginToken2 = CreateValidLoginToken(user.Id);
        loginToken2.Uuid = uuid;
        context.Set<LoginTokenEntity>().Add(loginToken2);

        Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
    }

    /// <summary>
    /// Requirement 14.4: Action token UUID uniqueness enforced via unique index.
    /// </summary>
    [Property(MaxTest = 50)]
    public void DuplicateActionTokenUuid_ShouldBeRejected(Guid uuid)
    {
        using var context = CreateContext();

        var user = CreateAndSaveUser(context);

        var token1 = CreateValidActionToken(user.Id);
        token1.Uuid = uuid;
        context.Set<ActionTokenEntity>().Add(token1);
        context.SaveChanges();

        var token2 = CreateValidActionToken(user.Id);
        token2.Uuid = uuid;
        context.Set<ActionTokenEntity>().Add(token2);

        Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());
    }

    #region Helpers

    private static UserEntity CreateValidUser() => new()
    {
        Uuid = Guid.NewGuid(),
        Provider = "local",
        Email = $"{Guid.NewGuid():N}@test.com",
        FirstName = "Test",
        LastName = "User",
        Password = "hashed_password",
        EmailVerified = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static UserEntity CreateAndSaveUser(TestDbContext context)
    {
        var user = CreateValidUser();
        context.Set<UserEntity>().Add(user);
        context.SaveChanges();
        return user;
    }

    private static ActionTokenEntity CreateValidActionToken(long userId) => new()
    {
        Uuid = Guid.NewGuid(),
        TokenHash = Guid.NewGuid().ToString("N"),
        ActionType = ActionTokenType.EmailVerification,
        UserId = userId,
        ExpiresAt = DateTime.UtcNow.AddHours(24),
        Used = false,
        CreatedAt = DateTime.UtcNow
    };

    private static AuthorizationCodeEntity CreateValidAuthorizationCode(long userId) => new()
    {
        Code = Guid.NewGuid().ToString("N"),
        ClientId = "test-client",
        RedirectUri = "https://example.com/callback",
        UserId = userId,
        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
        IsUsed = false,
        CreatedAt = DateTime.UtcNow
    };

    private static LoginTokenEntity CreateValidLoginToken(long userId) => new()
    {
        Uuid = Guid.NewGuid(),
        UserId = userId,
        Token = Guid.NewGuid().ToString("N"),
        CreatedAt = DateTime.UtcNow
    };

    #endregion
}

#region Custom Types and Arbitraries for FsCheck 3.x

/// <summary>Wrapper for generated valid email addresses.</summary>
public record ValidEmail(string Value)
{
    public override string ToString() => Value;
}

/// <summary>Wrapper for generated valid phone numbers.</summary>
public record ValidPhone(string Value)
{
    public override string ToString() => Value;
}

/// <summary>Wrapper for generated non-empty token strings.</summary>
public record NonEmptyToken(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Custom Arbitrary definitions for FsCheck 3.x property tests.
/// </summary>
public class DataUniquenessArbitraries
{
    public static Arbitrary<ValidEmail> ValidEmailArbitrary()
    {
        Gen<string> prefixGen = FsCheck.Fluent.Gen.Elements(
            new[] { "a", "b", "c", "d", "e", "f", "test", "user", "admin" });
        Gen<ValidEmail> gen = FsCheck.Fluent.Gen.Select(prefixGen,
            prefix => new ValidEmail($"{prefix}_{Guid.NewGuid():N}@example.com"));
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<ValidPhone> ValidPhoneArbitrary()
    {
        Gen<int> numGen = FsCheck.Fluent.Gen.Choose(1000000, 9999999);
        Gen<ValidPhone> gen = FsCheck.Fluent.Gen.Select(numGen,
            num => new ValidPhone($"+1555{num}"));
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<NonEmptyToken> NonEmptyTokenArbitrary()
    {
        Gen<string> prefixGen = FsCheck.Fluent.Gen.Elements(
            new[] { "token", "hash", "code", "secret" });
        Gen<NonEmptyToken> gen = FsCheck.Fluent.Gen.Select(prefixGen,
            prefix => new NonEmptyToken($"{prefix}_{Guid.NewGuid():N}"));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
