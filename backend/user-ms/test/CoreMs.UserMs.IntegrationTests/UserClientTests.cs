using CoreMs.Common.Exceptions;
using CoreMs.UserMs.Client;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Infrastructure.Data;
using CoreMs.UserMs.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreMs.UserMs.IntegrationTests;

/// <summary>
/// Integration tests exercising user-ms through its own typed client (UserMsClient).
/// Proves: client request serialization, controller routing, auth, response deserialization.
/// </summary>
public class UserClientTests : IClassFixture<InMemoryWebApplicationFactory>
{
    private readonly UserMsClient _adminClient;
    private readonly UserMsClient _userClient;
    private readonly Guid _seededUserId;

    public UserClientTests(InMemoryWebApplicationFactory factory)
    {
        _seededUserId = SeedUser(factory);

        _adminClient = new UserMsClient(factory.CreateAdminClient());
        _userClient = new UserMsClient(factory.CreateClientWithRoles(_seededUserId, "USER_MS_USER"));
    }

    private static Guid SeedUser(InMemoryWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserMsDbContext>();

        var uuid = Guid.NewGuid();
        var user = new UserEntity
        {
            Uuid = uuid,
            Email = $"client-test-{uuid:N}@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("TestPass1!"),
            FirstName = "Client",
            LastName = "TestUser",
            Provider = "local",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.Roles.Add(new UserRoleEntity { Name = "USER_MS_USER", UpdatedAt = DateTime.UtcNow });

        db.Set<UserEntity>().Add(user);
        db.SaveChanges();
        return uuid;
    }

    // ---- GetUserAsync (admin endpoint) ----

    [Fact]
    public async Task GetUser_ExistingId_ReturnsUserInfo()
    {
        var result = await _adminClient.GetUserAsync(_seededUserId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(_seededUserId);
        result.FirstName.Should().Be("Client");
        result.LastName.Should().Be("TestUser");
        result.Roles.Should().Contain("USER_MS_USER");
    }

    [Fact]
    public async Task GetUser_UnknownId_ThrowsServiceException()
    {
        var act = async () => await _adminClient.GetUserAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<ServiceException>();
    }

    // ---- GetCurrentProfileAsync (authenticated user's own profile) ----

    [Fact]
    public async Task GetCurrentProfile_Authenticated_ReturnsOwnProfile()
    {
        var result = await _userClient.GetCurrentProfileAsync();

        result.Should().NotBeNull();
        result!.UserId.Should().Be(_seededUserId);
        result.Email.Should().Contain("client-test");
    }

    [Fact]
    public async Task GetCurrentProfile_Anonymous_ThrowsServiceException()
    {
        var factory = new InMemoryWebApplicationFactory();
        var anonClient = new UserMsClient(factory.CreateAnonymousClient());

        var act = async () => await anonClient.GetCurrentProfileAsync();

        await act.Should().ThrowAsync<ServiceException>();
    }
}
