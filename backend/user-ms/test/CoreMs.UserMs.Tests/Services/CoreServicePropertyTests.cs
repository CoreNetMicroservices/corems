using CoreMs.Common.Exceptions;
using CoreMs.Common.Security;
using CoreMs.UserMs.Core.Configuration;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Repositories;
using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Core.Models;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace CoreMs.UserMs.Tests.Services;

/// <summary>
/// Property 1: Password Hashing Round-Trip
/// For any valid password, BCrypt.HashPassword followed by BCrypt.Verify always returns true.
///
/// Property 2: Registration Postconditions
/// For any valid CreateUserRequest, CreateUserAsync produces a user with the given email,
/// EmailVerified = false, and Provider = "local".
///
/// Property 3: Email Uniqueness Enforcement
/// For any email that already exists, CreateUserAsync always throws UserExists.
///
/// Property 18: Credential Validation State Checks
/// For any non-verified user, ValidateCredentialsAsync throws EmailNotVerified.
/// For any invalid password, ValidateCredentialsAsync throws InvalidCredentials.
///
/// Property 19: Successful Authentication Updates LastLoginAt
/// For any valid email+password pair, ValidateCredentialsAsync returns a user with matching email
/// and updates LastLoginAt.
///
/// Property 21: Token Entropy
/// Generated refresh tokens are always unique (generate N, all different) and non-empty.
///
/// Property 24: Timestamp Monotonicity
/// For any user modification, UpdatedAt is always >= CreatedAt.
///
/// Property 25: Social Login Creates or Links User
/// For any supported provider + valid email, HandleSocialLoginAsync creates/returns a user.
/// For any unsupported provider, HandleSocialLoginAsync throws InvalidRequest.
///
/// **Validates: Requirements 1.1–1.7, 3.1–3.4, 7.2–7.5, 8.1–8.3, 10.1–10.5, 11.1–11.2, 14.5, 16.1, 16.2**
/// </summary>
public class CoreServicePropertyTests
{
    #region Property 1: Password Hashing Round-Trip

    /// <summary>
    /// Property 1: For any password, AdminChangePasswordAsync hashes it such that BCrypt.Verify returns true.
    /// Validates: Requirements 16.1, 7.2
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task AdminChangePassword_AlwaysHashesPassword_BCryptVerifyReturnsTrue(ValidPasswordInput password)
    {
        var userRepo = CreateUserRepository();
        var user = CreateTestUser();
        userRepo.GetByUuidAsync(user.Uuid, Arg.Any<CancellationToken>()).Returns(user);

        var service = new UserService(userRepo);
        await service.AdminChangePasswordAsync(user.Uuid, password.Value, password.Value);

        Assert.True(BCrypt.Net.BCrypt.Verify(password.Value, user.Password));
    }

    #endregion

    #region Property 2: Registration Postconditions

    /// <summary>
    /// Property 2: For any valid CreateUserRequest, CreateUserAsync produces a user
    /// with the given email, EmailVerified=false, and Provider="local".
    /// Validates: Requirements 1.1, 1.2, 1.3
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task CreateUser_WithValidRequest_SetsEmailAndLocalProvider(ValidCreateUserInput input)
    {
        var userRepo = CreateUserRepository();
        userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        userRepo.ExistsByPhoneNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var service = new UserService(userRepo);
        var request = new CreateUserRequest(input.Email, input.FirstName, input.LastName, null, null);

        var result = await service.CreateUserAsync(request);

        Assert.Equal(input.Email, result.Email);
        Assert.False(result.EmailVerified);
        Assert.Equal("local", result.Provider);
    }

    #endregion

    #region Property 3: Email Uniqueness Enforcement

    /// <summary>
    /// Property 3: For any email that already exists, CreateUserAsync always throws UserExists.
    /// Validates: Requirement 1.6
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task CreateUser_WithDuplicateEmail_AlwaysThrowsUserExists(ValidCreateUserInput input)
    {
        var userRepo = CreateUserRepository();
        userRepo.ExistsByEmailAsync(input.Email, Arg.Any<CancellationToken>()).Returns(true);

        var service = new UserService(userRepo);
        var request = new CreateUserRequest(input.Email, input.FirstName, input.LastName, null, null);

        var ex = await Assert.ThrowsAsync<ServiceException>(() => service.CreateUserAsync(request));
        Assert.Equal(409, ex.HttpStatusCode);
    }

    #endregion

    #region Property 18: Credential Validation State Checks

    /// <summary>
    /// Property 18: For any invalid password, ValidateCredentialsAsync throws InvalidCredentials.
    /// Validates: Requirement 10.3
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task ValidateCredentials_WithWrongPassword_AlwaysThrowsInvalidCredentials(ValidPasswordInput correctPwd, ValidPasswordInput wrongPwd)
    {
        if (correctPwd.Value == wrongPwd.Value) return; // skip trivial case

        var userRepo = CreateUserRepository();
        var user = CreateTestUser();
        user.Password = BCrypt.Net.BCrypt.HashPassword(correctPwd.Value, workFactor: 4);
        user.EmailVerified = true;
        userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var service = new AuthService(userRepo);

        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => service.ValidateCredentialsAsync(user.Email, wrongPwd.Value));
        Assert.Equal(401, ex.HttpStatusCode);
    }

    /// <summary>
    /// Property 18: For any non-verified user, ValidateCredentialsAsync throws EmailNotVerified.
    /// Validates: Requirement 10.5
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task ValidateCredentials_WithUnverifiedEmail_AlwaysThrowsEmailNotVerified(ValidPasswordInput password)
    {
        var userRepo = CreateUserRepository();
        var user = CreateTestUser();
        user.Password = BCrypt.Net.BCrypt.HashPassword(password.Value, workFactor: 4);
        user.EmailVerified = false;
        userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var service = new AuthService(userRepo);

        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => service.ValidateCredentialsAsync(user.Email, password.Value));
        Assert.Equal(403, ex.HttpStatusCode);
    }

    /// <summary>
    /// Property 18: For a non-existent email, ValidateCredentialsAsync throws InvalidCredentials.
    /// Validates: Requirement 10.2
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task ValidateCredentials_WithNonExistentEmail_AlwaysThrowsInvalidCredentials(ValidCreateUserInput input)
    {
        var userRepo = CreateUserRepository();
        userRepo.GetByEmailAsync(input.Email, Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

        var service = new AuthService(userRepo);

        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => service.ValidateCredentialsAsync(input.Email, "AnyP@ss1"));
        Assert.Equal(401, ex.HttpStatusCode);
    }

    #endregion

    #region Property 19: Successful Authentication Updates LastLoginAt

    /// <summary>
    /// Property 19: For valid credentials, ValidateCredentialsAsync returns the user with matching email
    /// and sets LastLoginAt.
    /// Validates: Requirement 10.1
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task ValidateCredentials_WithValidCredentials_UpdatesLastLoginAt(ValidPasswordInput password)
    {
        var userRepo = CreateUserRepository();
        var user = CreateTestUser();
        user.Password = BCrypt.Net.BCrypt.HashPassword(password.Value, workFactor: 4);
        user.EmailVerified = true;
        user.LastLoginAt = null;
        userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var service = new AuthService(userRepo);
        var before = DateTime.UtcNow;

        var result = await service.ValidateCredentialsAsync(user.Email, password.Value);

        Assert.Equal(user.Email, result.Email);
        Assert.NotNull(result.LastLoginAt);
        Assert.True(result.LastLoginAt >= before);
    }

    #endregion

    #region Property 21: Token Entropy

    /// <summary>
    /// Property 21: Generated access tokens are always non-empty strings.
    /// Validates: Requirement 16.2
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task GenerateTokenResponse_AlwaysProducesNonEmptyAccessToken(UserEntityInput userInput)
    {
        var loginTokenRepo = CreateLoginTokenRepository();
        var actionTokenRepo = CreateActionTokenRepository();
        var authCodeRepo = CreateAuthorizationCodeRepository();
        var options = Options.Create(CreateTestTokenOptions());

        var service = new TokenService(loginTokenRepo, actionTokenRepo, authCodeRepo, options, CreateTestTokenProvider());
        var user = userInput.ToEntity();

        var response = await service.GenerateTokenResponseAsync(user, "openid profile email", null);

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
    }

    /// <summary>
    /// Property 21: For any user, GenerateTokenResponseAsync produces a valid JWT with correct sub claim.
    /// Validates: Requirement 16.2
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task GenerateTokenResponse_SubClaimMatchesUserUuid(UserEntityInput userInput)
    {
        var loginTokenRepo = CreateLoginTokenRepository();
        var actionTokenRepo = CreateActionTokenRepository();
        var authCodeRepo = CreateAuthorizationCodeRepository();
        var options = Options.Create(CreateTestTokenOptions());

        var service = new TokenService(loginTokenRepo, actionTokenRepo, authCodeRepo, options, CreateTestTokenProvider());
        var user = userInput.ToEntity();

        var response = await service.GenerateTokenResponseAsync(user, "openid profile email", null);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.AccessToken);
        var sub = jwt.Claims.First(c => c.Type == "sub").Value;

        Assert.Equal(user.Uuid.ToString(), sub);
    }

    /// <summary>
    /// Property 21: Refresh tokens are always unique (generate N, all different).
    /// Validates: Requirement 16.2
    /// </summary>
    [Fact]
    public async Task GenerateMultipleRefreshTokens_AreAllUnique()
    {
        var loginTokenRepo = CreateLoginTokenRepository();
        var actionTokenRepo = CreateActionTokenRepository();
        var authCodeRepo = CreateAuthorizationCodeRepository();
        var options = Options.Create(CreateTestTokenOptions());

        var service = new TokenService(loginTokenRepo, actionTokenRepo, authCodeRepo, options, CreateTestTokenProvider());
        var user = CreateTestUser();

        var tokens = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var token = await service.CreateRefreshTokenAsync(user);
            tokens.Add(token);
        }

        Assert.Equal(100, tokens.Count);
    }

    #endregion

    #region Property 24: Timestamp Monotonicity

    /// <summary>
    /// Property 24: For any AdminChangePassword call, UpdatedAt >= CreatedAt.
    /// Validates: Requirement 14.5
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task AdminChangePassword_UpdatedAtIsAlwaysAfterOrEqualCreatedAt(ValidPasswordInput password)
    {
        var userRepo = CreateUserRepository();
        var user = CreateTestUser();
        user.CreatedAt = DateTime.UtcNow.AddDays(-10);
        userRepo.GetByUuidAsync(user.Uuid, Arg.Any<CancellationToken>()).Returns(user);

        var service = new UserService(userRepo);
        await service.AdminChangePasswordAsync(user.Uuid, password.Value, password.Value);

        Assert.True(user.UpdatedAt >= user.CreatedAt);
    }

    /// <summary>
    /// Property 24: For any AdminChangeEmail call, UpdatedAt >= CreatedAt.
    /// Validates: Requirement 14.5
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task AdminChangeEmail_UpdatedAtIsAlwaysAfterOrEqualCreatedAt(ValidCreateUserInput input)
    {
        var userRepo = CreateUserRepository();
        var user = CreateTestUser();
        user.CreatedAt = DateTime.UtcNow.AddDays(-10);
        userRepo.GetByUuidAsync(user.Uuid, Arg.Any<CancellationToken>()).Returns(user);

        var service = new UserService(userRepo);
        await service.AdminChangeEmailAsync(user.Uuid, input.Email);

        Assert.True(user.UpdatedAt >= user.CreatedAt);
    }

    #endregion

    #region Property 25: Social Login Creates or Links User

    /// <summary>
    /// Property 25: For any supported provider + valid email, HandleSocialLoginAsync creates/returns a user.
    /// Validates: Requirement 11.1, 11.2
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task SocialLogin_WithSupportedProvider_CreatesOrReturnsUser(SocialLoginInput input)
    {
        var userRepo = CreateUserRepository();
        userRepo.GetByEmailAsync(input.Info.Email, Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

        var service = new SocialAuthService(userRepo);
        var result = await service.HandleSocialLoginAsync(input.Provider, input.Info);

        Assert.Equal(input.Info.Email, result.Email);
        Assert.Contains(input.Provider.ToLowerInvariant(), result.Provider);
        Assert.True(result.EmailVerified);
    }

    /// <summary>
    /// Property 25: For a supported provider with an existing user, HandleSocialLoginAsync links the provider.
    /// Validates: Requirement 11.2
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task SocialLogin_WithExistingUser_LinksProvider(SocialLoginInput input)
    {
        var userRepo = CreateUserRepository();
        var existingUser = CreateTestUser();
        existingUser.Email = input.Info.Email;
        existingUser.Provider = "local";
        userRepo.GetByEmailAsync(input.Info.Email, Arg.Any<CancellationToken>()).Returns(existingUser);

        var service = new SocialAuthService(userRepo);
        var result = await service.HandleSocialLoginAsync(input.Provider, input.Info);

        Assert.Equal(input.Info.Email, result.Email);
        Assert.Contains(input.Provider.ToLowerInvariant(), result.Provider);
    }

    /// <summary>
    /// Property 25: For any unsupported provider, HandleSocialLoginAsync throws InvalidRequest.
    /// Validates: Requirement 11.3
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(ServiceArbitraries)])]
    public async Task SocialLogin_WithUnsupportedProvider_AlwaysThrowsInvalidRequest(UnsupportedProviderInput input)
    {
        var userRepo = CreateUserRepository();
        var service = new SocialAuthService(userRepo);

        var info = new ExternalLoginInfo("test@example.com", "Test", "User", null, "id123");

        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => service.HandleSocialLoginAsync(input.Value, info));
        Assert.Equal(400, ex.HttpStatusCode);
    }

    #endregion

    #region Helpers

    private static UserRepository CreateUserRepository()
        => Substitute.For<UserRepository>(Substitute.For<DbContext>());

    private static LoginTokenRepository CreateLoginTokenRepository()
        => Substitute.For<LoginTokenRepository>(Substitute.For<DbContext>());

    private static ActionTokenRepository CreateActionTokenRepository()
        => Substitute.For<ActionTokenRepository>(Substitute.For<DbContext>());

    private static AuthorizationCodeRepository CreateAuthorizationCodeRepository()
        => Substitute.For<AuthorizationCodeRepository>(Substitute.For<DbContext>());

    private static UserEntity CreateTestUser() => new()
    {
        Id = 1,
        Uuid = Guid.NewGuid(),
        Provider = "local",
        Email = $"test_{Guid.NewGuid():N}@example.com",
        FirstName = "Test",
        LastName = "User",
        Password = BCrypt.Net.BCrypt.HashPassword("Test1Pass!", workFactor: 4),
        EmailVerified = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TokenServiceOptions CreateTestTokenOptions() => new()
    {
        Issuer = "http://test-issuer",
        Audience = "test-audience",
        SecretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!",
        AccessTokenExpirationMinutes = 10,
        RefreshTokenExpirationMinutes = 1440,
        IdTokenExpirationMinutes = 60
    };

    private static TokenProvider CreateTestTokenProvider()
    {
        var providerOptions = Options.Create(new TokenProviderOptions
        {
            Algorithm = SigningAlgorithm.HS256,
            SecretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!",
            Issuer = "http://test-issuer",
            AccessTokenExpirationMinutes = 10,
            RefreshTokenExpirationMinutes = 1440,
            IdTokenExpirationMinutes = 60,
            ActionTokenExpirationMinutes = 1440
        });
        return new TokenProvider(providerOptions);
    }

    #endregion
}

#region Custom Types and Arbitraries

public record ValidPasswordInput(string Value)
{
    public override string ToString() => Value;
}

public record ValidCreateUserInput(string Email, string? FirstName, string? LastName)
{
    public override string ToString() => Email;
}

public record UserEntityInput(Guid Uuid, string Email, string? FirstName, string? LastName)
{
    public UserEntity ToEntity() => new()
    {
        Id = 1,
        Uuid = Uuid,
        Email = Email,
        FirstName = FirstName,
        LastName = LastName,
        Provider = "local",
        EmailVerified = true,
        Password = BCrypt.Net.BCrypt.HashPassword("Dummy1!", workFactor: 4),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Roles = { new UserRoleEntity { Name = "USER_MS_USER" } }
    };

    public override string ToString() => $"{Email} ({Uuid})";
}

public record SocialLoginInput(string Provider, ExternalLoginInfo Info)
{
    public override string ToString() => $"{Provider}: {Info.Email}";
}

public record UnsupportedProviderInput(string Value)
{
    public override string ToString() => Value;
}

public class ServiceArbitraries
{
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string DigitChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*";

    public static Arbitrary<ValidPasswordInput> ValidPasswordInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<ValidPasswordInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var chars = new char[12];
            chars[0] = UpperChars[rng.Next(UpperChars.Length)];
            chars[1] = LowerChars[rng.Next(LowerChars.Length)];
            chars[2] = DigitChars[rng.Next(DigitChars.Length)];
            chars[3] = SpecialChars[rng.Next(SpecialChars.Length)];
            var all = UpperChars + LowerChars + DigitChars + SpecialChars;
            for (int i = 4; i < 12; i++)
                chars[i] = all[rng.Next(all.Length)];
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new ValidPasswordInput(new string(chars));
        });
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<ValidCreateUserInput> ValidCreateUserInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<ValidCreateUserInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var names = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank" };
            var lastNames = new[] { "Smith", "Johnson", "Brown", "Taylor", "Wilson" };
            var email = $"user_{seed:x8}@test.com";
            var firstName = names[rng.Next(names.Length)];
            var lastName = lastNames[rng.Next(lastNames.Length)];
            return new ValidCreateUserInput(email, firstName, lastName);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<UserEntityInput> UserEntityInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<UserEntityInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var names = new[] { "Alice", "Bob", "Charlie", "Diana" };
            var lastNames = new[] { "Smith", "Johnson", "Brown" };
            var uuid = new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var email = $"user_{seed:x8}@test.com";
            return new UserEntityInput(uuid, email, names[rng.Next(names.Length)], lastNames[rng.Next(lastNames.Length)]);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<SocialLoginInput> SocialLoginInputArbitrary()
    {
        var providers = new[] { "google", "github", "linkedin" };
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<SocialLoginInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var provider = providers[rng.Next(providers.Length)];
            var email = $"social_{seed:x8}@test.com";
            var info = new ExternalLoginInfo(email, "Social", "User", null, $"provider_id_{seed}");
            return new SocialLoginInput(provider, info);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<UnsupportedProviderInput> UnsupportedProviderInputArbitrary()
    {
        var unsupported = new[] { "facebook", "twitter", "apple", "microsoft", "yahoo", "dropbox", "slack" };
        Gen<UnsupportedProviderInput> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements(unsupported),
            p => new UnsupportedProviderInput(p));
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
