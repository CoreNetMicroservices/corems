using System.Net;
using System.Net.Http.Json;
using CoreMs.Common.Repository;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Models;
using CoreMs.UserMs.Infrastructure.Data;
using CoreMs.UserMs.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreMs.UserMs.IntegrationTests;

public class UsersControllerTests : IClassFixture<InMemoryWebApplicationFactory>
{
    private readonly InMemoryWebApplicationFactory _factory;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _anonymousClient;
    private readonly Guid _seededUserUuid;

    public UsersControllerTests(InMemoryWebApplicationFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateAdminClient();
        _anonymousClient = factory.CreateAnonymousClient();
        _seededUserUuid = SeedTestUser();
    }

    private Guid SeedTestUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserMsDbContext>();

        var uuid = Guid.NewGuid();
        var user = new UserEntity
        {
            Uuid = uuid,
            Email = $"admin-test-{uuid:N}@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("TestPass1!"),
            FirstName = "Test",
            LastName = "User",
            Provider = "local",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.Roles.Add(new UserRoleEntity
        {
            Name = "USER_MS_USER",
            UpdatedAt = DateTime.UtcNow
        });

        db.Set<UserEntity>().Add(user);
        db.SaveChanges();

        return uuid;
    }

    #region GET /api/users — Paginated list

    [Fact]
    public async Task GetUsers_WithAdminToken_ReturnsPaginatedUsers()
    {
        var response = await _adminClient.GetAsync("/api/users?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserInfoDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalElements.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetUsers_WithoutToken_Returns401()
    {
        var response = await _anonymousClient.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/users — Create user

    [Fact]
    public async Task CreateUser_WithAdminToken_ReturnsCreated()
    {
        var request = new CreateUserRequest(
            Email: $"newuser-{Guid.NewGuid():N}@example.com",
            FirstName: "New",
            LastName: "User",
            PhoneNumber: null,
            Roles: ["USER_MS_USER", "USER_MS_ADMIN"]);

        var response = await _adminClient.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await response.Content.ReadFromJsonAsync<UserInfoDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(request.Email);
        user.FirstName.Should().Be("New");
        user.LastName.Should().Be("User");
        user.Roles.Should().Contain("USER_MS_USER");
        user.Roles.Should().Contain("USER_MS_ADMIN");
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_Returns400()
    {
        var request = new CreateUserRequest(
            Email: "not-an-email",
            FirstName: "Bad",
            LastName: "Email",
            PhoneNumber: null,
            Roles: ["USER_MS_USER"]);

        var response = await _adminClient.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_WithEmptyEmail_Returns400()
    {
        var request = new CreateUserRequest(
            Email: "",
            FirstName: "No",
            LastName: "Email",
            PhoneNumber: null,
            Roles: ["USER_MS_USER"]);

        var response = await _adminClient.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/users/{id} — Get by ID

    [Fact]
    public async Task GetUserById_WithValidId_ReturnsUser()
    {
        var response = await _adminClient.GetAsync($"/api/users/{_seededUserUuid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserInfoDto>();
        user.Should().NotBeNull();
        user!.UserId.Should().Be(_seededUserUuid);
        user.FirstName.Should().Be("Test");
        user.LastName.Should().Be("User");
        user.Roles.Should().Contain("USER_MS_USER");
    }

    [Fact]
    public async Task GetUserById_WithNonExistentId_Returns404()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await _adminClient.GetAsync($"/api/users/{nonExistentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /api/users/{id} — Update user

    [Fact]
    public async Task UpdateUser_WithAdminToken_ReturnsUpdatedUser()
    {
        // Create a user to update
        var createRequest = new CreateUserRequest(
            Email: $"updateuser-{Guid.NewGuid():N}@example.com",
            FirstName: "Before",
            LastName: "Update",
            PhoneNumber: null,
            Roles: ["USER_MS_USER"]);
        var createResponse = await _adminClient.PostAsJsonAsync("/api/users", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<UserInfoDto>();

        var updateRequest = new UserUpdateRequest(
            FirstName: "After",
            LastName: "Updated",
            Email: null,
            PhoneNumber: "+1234567890",
            ImageUrl: null,
            Roles: null);

        var response = await _adminClient.PutAsJsonAsync($"/api/users/{created!.UserId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserInfoDto>();
        user.Should().NotBeNull();
        user!.FirstName.Should().Be("After");
        user.LastName.Should().Be("Updated");
        user.PhoneNumber.Should().Be("+1234567890");
    }

    #endregion

    #region DELETE /api/users/{id} — Delete user

    [Fact]
    public async Task DeleteUser_WithAdminToken_ReturnsNoContent()
    {
        // Create a user to delete
        var createRequest = new CreateUserRequest(
            Email: $"deleteuser-{Guid.NewGuid():N}@example.com",
            FirstName: "Delete",
            LastName: "Me",
            PhoneNumber: null,
            Roles: ["USER_MS_USER"]);
        var createResponse = await _adminClient.PostAsJsonAsync("/api/users", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<UserInfoDto>();

        // Delete the user
        var response = await _adminClient.DeleteAsync($"/api/users/{created!.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getResponse = await _adminClient.GetAsync($"/api/users/{created.UserId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_WithNonExistentId_Returns404()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await _adminClient.DeleteAsync($"/api/users/{nonExistentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/users/{id}/change-password — Admin change password

    [Fact]
    public async Task AdminChangePassword_WithAdminToken_ReturnsOk()
    {
        var request = new { NewPassword = "NewSecureP@ss1", ConfirmPassword = "NewSecureP@ss1" };

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/users/{_seededUserUuid}/change-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/users/{id}/change-email — Admin change email

    [Fact]
    public async Task AdminChangeEmail_WithAdminToken_ReturnsOk()
    {
        // Create a dedicated user for this test to avoid conflicts
        var createRequest = new CreateUserRequest(
            Email: $"emailchange-{Guid.NewGuid():N}@example.com",
            FirstName: "Email",
            LastName: "Change",
            PhoneNumber: null,
            Roles: ["USER_MS_USER"]);
        var createResponse = await _adminClient.PostAsJsonAsync("/api/users", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<UserInfoDto>();

        var request = new { NewEmail = $"newemail-{Guid.NewGuid():N}@example.com" };

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/users/{created!.UserId}/change-email", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Authorization — non-admin users get 403

    [Fact]
    public async Task GetUsers_WithUserRoleOnly_Returns403()
    {
        var userClient = _factory.CreateClientWithRoles(Guid.NewGuid(), "USER_MS_USER");

        var response = await userClient.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_WithUserRoleOnly_Returns403()
    {
        var userClient = _factory.CreateClientWithRoles(Guid.NewGuid(), "USER_MS_USER");

        var request = new CreateUserRequest(
            Email: "forbidden@example.com",
            FirstName: "No",
            LastName: "Access",
            PhoneNumber: null,
            Roles: ["USER_MS_USER"]);

        var response = await userClient.PostAsJsonAsync("/api/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_WithUserRoleOnly_Returns403()
    {
        var userClient = _factory.CreateClientWithRoles(Guid.NewGuid(), "USER_MS_USER");

        var response = await userClient.DeleteAsync($"/api/users/{_seededUserUuid}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WithSuperAdminRole_ReturnsOk()
    {
        var superAdminClient = _factory.CreateClientWithRoles(Guid.NewGuid(), "SUPER_ADMIN");

        var response = await superAdminClient.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_WithUserRoleOnly_Returns403()
    {
        var userClient = _factory.CreateClientWithRoles(Guid.NewGuid(), "USER_MS_USER");

        var updateRequest = new UserUpdateRequest(
            FirstName: "Hacker",
            LastName: "Attempt",
            Email: null,
            PhoneNumber: null,
            ImageUrl: null,
            Roles: null);

        var response = await userClient.PutAsJsonAsync($"/api/users/{_seededUserUuid}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminChangePassword_WithUserRoleOnly_Returns403()
    {
        var userClient = _factory.CreateClientWithRoles(Guid.NewGuid(), "USER_MS_USER");

        var request = new { NewPassword = "Hack3rP@ss!", ConfirmPassword = "Hack3rP@ss!" };

        var response = await userClient.PostAsJsonAsync(
            $"/api/users/{_seededUserUuid}/change-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminChangeEmail_WithUserRoleOnly_Returns403()
    {
        var userClient = _factory.CreateClientWithRoles(Guid.NewGuid(), "USER_MS_USER");

        var request = new { NewEmail = "hacker@example.com" };

        var response = await userClient.PostAsJsonAsync(
            $"/api/users/{_seededUserUuid}/change-email", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
