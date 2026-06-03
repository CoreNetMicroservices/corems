using System.Reflection;
using CoreMs.UserMs.Api.Controllers;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Enums;
using CoreMs.UserMs.Core.Repositories;
using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Core.Models;
using CoreMs.UserMs.Tests.Infrastructure;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CoreMs.UserMs.Tests.Controllers;

/// <summary>
/// Property 15: Admin Authorization Gate
/// For any request to the admin users endpoints, the request SHALL be rejected
/// with HTTP 403 unless the caller has the USER_MS_ADMIN or SUPER_ADMIN role.
///
/// Property 16: Cascade Deletion
/// For any user deletion, all related login tokens, action tokens, authorization codes,
/// and user roles SHALL be removed from the database.
///
/// Property 17: Admin Email Change Resets Verification
/// For any admin email change operation, the user's email SHALL be updated and
/// IsEmailVerified SHALL be reset to false.
///
/// **Validates: Requirements 9.1, 9.5, 9.7**
/// </summary>
public class AdminAuthorizationPropertyTests
{
    #region Property 15: Admin Authorization Gate

    /// <summary>
    /// Property 15: The UsersController class-level [Authorize] attribute requires USER_MS_ADMIN or SUPER_ADMIN.
    /// Validates: Requirement 9.1
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(AdminAuthArbitraries)])]
    public void UsersController_AuthorizeAttribute_RequiresAdminOrSuperAdmin(AdminEndpointInput endpoint)
    {
        var controllerType = typeof(UsersController);
        var authorizeAttrs = controllerType.GetCustomAttributes<AuthorizeAttribute>().ToList();

        Assert.NotEmpty(authorizeAttrs);

        var classAttr = authorizeAttrs.First();
        Assert.NotNull(classAttr.Roles);

        var roles = classAttr.Roles!.Split(',', StringSplitOptions.TrimEntries);
        Assert.Contains("USER_MS_ADMIN", roles);
        Assert.Contains("SUPER_ADMIN", roles);
    }

    /// <summary>
    /// Property 15: For any public method on UsersController, no [AllowAnonymous] bypasses the class-level gate.
    /// Validates: Requirement 9.1
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(AdminAuthArbitraries)])]
    public void UsersController_NoEndpoint_AllowsAnonymous(AdminEndpointInput endpoint)
    {
        var controllerType = typeof(UsersController);
        var method = controllerType.GetMethod(endpoint.MethodName);

        Assert.NotNull(method);
        var allowAnonymous = method!.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.Null(allowAnonymous);
    }

    /// <summary>
    /// Property 15: For any role NOT in {USER_MS_ADMIN, SUPER_ADMIN}, the authorize attribute would reject.
    /// This validates the attribute is configured with exactly the expected roles.
    /// Validates: Requirement 9.1
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(AdminAuthArbitraries)])]
    public void UsersController_UnauthorizedRole_IsNotInAllowedRoles(UnauthorizedRoleInput role)
    {
        var controllerType = typeof(UsersController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttr);
        Assert.NotNull(authorizeAttr!.Roles);

        var allowedRoles = authorizeAttr.Roles!.Split(',', StringSplitOptions.TrimEntries);
        Assert.DoesNotContain(role.Value, allowedRoles);
    }

    #endregion

    #region Property 16: Cascade Deletion

    /// <summary>
    /// Property 16: For any user with related entities, deleting the user cascades to all related data.
    /// Validates: Requirement 9.5
    /// </summary>
    [Property(MaxTest = 30, Arbitrary = [typeof(AdminAuthArbitraries)])]
    public void DeleteUser_CascadesAllRelatedEntities(CascadeTestInput input)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = new TestDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        long userId;

        // Create user with related entities
        using (var context = new TestDbContext(options))
        {
            var user = new UserEntity
            {
                Uuid = Guid.NewGuid(),
                Provider = "local",
                Email = $"cascade_{Guid.NewGuid():N}@test.com",
                FirstName = "Test",
                LastName = "User",
                Password = "hashed",
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Set<UserEntity>().Add(user);
            context.SaveChanges();
            userId = user.Id;

            // Add roles
            for (int i = 0; i < input.RoleCount; i++)
            {
                context.Set<UserRoleEntity>().Add(new UserRoleEntity
                {
                    UserId = userId,
                    Name = $"ROLE_{i}",
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Add login tokens
            for (int i = 0; i < input.TokenCount; i++)
            {
                context.Set<LoginTokenEntity>().Add(new LoginTokenEntity
                {
                    Uuid = Guid.NewGuid(),
                    UserId = userId,
                    Token = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Add action tokens
            for (int i = 0; i < input.ActionTokenCount; i++)
            {
                context.Set<ActionTokenEntity>().Add(new ActionTokenEntity
                {
                    Uuid = Guid.NewGuid(),
                    UserId = userId,
                    TokenHash = Guid.NewGuid().ToString("N"),
                    ActionType = ActionTokenType.EmailVerification,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    Used = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Add authorization codes
            for (int i = 0; i < input.AuthCodeCount; i++)
            {
                context.Set<AuthorizationCodeEntity>().Add(new AuthorizationCodeEntity
                {
                    Code = Guid.NewGuid().ToString("N"),
                    ClientId = "test-client",
                    RedirectUri = "https://example.com/callback",
                    UserId = userId,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            context.SaveChanges();
        }

        // Delete the user
        using (var context = new TestDbContext(options))
        {
            var user = context.Set<UserEntity>().First(u => u.Id == userId);
            context.Set<UserEntity>().Remove(user);
            context.SaveChanges();
        }

        // Verify all related entities are gone
        using (var context = new TestDbContext(options))
        {
            Assert.Empty(context.Set<UserRoleEntity>().Where(r => r.UserId == userId).ToList());
            Assert.Empty(context.Set<LoginTokenEntity>().Where(t => t.UserId == userId).ToList());
            Assert.Empty(context.Set<ActionTokenEntity>().Where(t => t.UserId == userId).ToList());
            Assert.Empty(context.Set<AuthorizationCodeEntity>().Where(c => c.UserId == userId).ToList());
            Assert.Null(context.Set<UserEntity>().FirstOrDefault(u => u.Id == userId));
        }
    }

    #endregion

    #region Property 17: Admin Email Change Resets Verification

    /// <summary>
    /// Property 17: For any admin email change, the email is updated and EmailVerified is reset to false.
    /// Validates: Requirement 9.7
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(AdminAuthArbitraries)])]
    public async Task AdminChangeEmail_AlwaysResetsEmailVerified(AdminEmailChangeInput input)
    {
        var userRepo = Substitute.For<UserRepository>(Substitute.For<DbContext>());
        var user = new UserEntity
        {
            Id = 1,
            Uuid = input.UserUuid,
            Provider = "local",
            Email = input.OldEmail,
            FirstName = "Test",
            LastName = "User",
            Password = "hashed",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        userRepo.GetByUuidAsync(input.UserUuid, Arg.Any<CancellationToken>()).Returns(user);

        var service = new UserService(userRepo);
        await service.AdminChangeEmailAsync(input.UserUuid, input.NewEmail);

        Assert.Equal(input.NewEmail, user.Email);
        Assert.False(user.EmailVerified);
    }

    /// <summary>
    /// Property 17: For any admin email change, UpdatedAt is refreshed.
    /// Validates: Requirement 9.7
    /// </summary>
    [Property(MaxTest = 50, Arbitrary = [typeof(AdminAuthArbitraries)])]
    public async Task AdminChangeEmail_AlwaysUpdatesTimestamp(AdminEmailChangeInput input)
    {
        var userRepo = Substitute.For<UserRepository>(Substitute.For<DbContext>());
        var oldUpdatedAt = DateTime.UtcNow.AddDays(-1);
        var user = new UserEntity
        {
            Id = 1,
            Uuid = input.UserUuid,
            Provider = "local",
            Email = input.OldEmail,
            FirstName = "Test",
            LastName = "User",
            Password = "hashed",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = oldUpdatedAt
        };
        userRepo.GetByUuidAsync(input.UserUuid, Arg.Any<CancellationToken>()).Returns(user);

        var service = new UserService(userRepo);
        await service.AdminChangeEmailAsync(input.UserUuid, input.NewEmail);

        Assert.True(user.UpdatedAt > oldUpdatedAt);
    }

    #endregion
}

#region Custom Types and Arbitraries

public record AdminEndpointInput(string MethodName)
{
    public override string ToString() => MethodName;
}

public record UnauthorizedRoleInput(string Value)
{
    public override string ToString() => Value;
}

public record CascadeTestInput(int RoleCount, int TokenCount, int ActionTokenCount, int AuthCodeCount)
{
    public override string ToString() =>
        $"Roles={RoleCount}, Tokens={TokenCount}, ActionTokens={ActionTokenCount}, AuthCodes={AuthCodeCount}";
}

public record AdminEmailChangeInput(Guid UserUuid, string OldEmail, string NewEmail)
{
    public override string ToString() => $"{OldEmail} -> {NewEmail}";
}

public class AdminAuthArbitraries
{
    private static readonly string[] EndpointMethods =
    [
        "GetAll", "Create", "GetById", "Update", "Delete", "ChangePassword", "ChangeEmail"
    ];

    private static readonly string[] UnauthorizedRoles =
    [
        "USER_MS_USER", "COMMUNICATION_MS_ADMIN", "DOCUMENT_MS_ADMIN",
        "TRANSLATION_MS_ADMIN", "DOCUMENT_MS_USER", "COMMUNICATION_MS_USER",
        "ANONYMOUS", "VIEWER", "EDITOR"
    ];

    public static Arbitrary<AdminEndpointInput> AdminEndpointInputArbitrary()
    {
        Gen<AdminEndpointInput> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements(EndpointMethods),
            name => new AdminEndpointInput(name));
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<UnauthorizedRoleInput> UnauthorizedRoleInputArbitrary()
    {
        Gen<UnauthorizedRoleInput> gen = FsCheck.Fluent.Gen.Select(
            FsCheck.Fluent.Gen.Elements(UnauthorizedRoles),
            role => new UnauthorizedRoleInput(role));
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<CascadeTestInput> CascadeTestInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<CascadeTestInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            return new CascadeTestInput(
                rng.Next(1, 5),
                rng.Next(0, 4),
                rng.Next(0, 3),
                rng.Next(0, 3));
        });
        return FsCheck.Fluent.Arb.From(gen);
    }

    public static Arbitrary<AdminEmailChangeInput> AdminEmailChangeInputArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<AdminEmailChangeInput> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var uuid = new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var oldEmail = $"old_{seed:x8}@test.com";
            var newEmail = $"new_{seed:x8}@test.com";
            return new AdminEmailChangeInput(uuid, oldEmail, newEmail);
        });
        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
