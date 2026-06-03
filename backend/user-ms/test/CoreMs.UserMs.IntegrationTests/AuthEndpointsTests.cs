using System.Net;
using System.Net.Http.Json;
using CoreMs.UserMs.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CoreMs.UserMs.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<InMemoryWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(InMemoryWebApplicationFactory factory)
    {
        _client = factory.CreateAnonymousClient();
    }

    #region POST /api/auth/signup — Successful

    [Fact]
    public async Task SignUp_ValidRequest_Returns201WithUserInfo()
    {
        var email = $"signup-valid-{Guid.NewGuid():N}@test.com";
        var request = new
        {
            Email = email,
            Password = "StrongP@ss1",
            ConfirmPassword = "StrongP@ss1",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<UserCreatedResponse>();
        body.Should().NotBeNull();
        body!.UserId.Should().NotBeEmpty();
        body.Email.Should().Be(email);
    }

    #endregion

    #region POST /api/auth/signup — Duplicate email returns 409

    [Fact]
    public async Task SignUp_DuplicateEmail_Returns409()
    {
        var email = $"signup-dup-{Guid.NewGuid():N}@test.com";
        var request = new
        {
            Email = email,
            Password = "StrongP@ss1",
            ConfirmPassword = "StrongP@ss1",
            FirstName = "First",
            LastName = "User"
        };

        var first = await _client.PostAsJsonAsync("/api/auth/signup", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/auth/signup", request);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region POST /api/auth/signup — Invalid input returns 400

    [Theory]
    [InlineData("short1!", "short1!")]
    [InlineData("nouppercase1!", "nouppercase1!")]
    [InlineData("NOLOWERCASE1!", "NOLOWERCASE1!")]
    [InlineData("NoDigitHere!", "NoDigitHere!")]
    [InlineData("NoSpecial1A", "NoSpecial1A")]
    public async Task SignUp_InvalidPassword_Returns400(string password, string confirmPassword)
    {
        var request = new
        {
            Email = $"pw-invalid-{Guid.NewGuid():N}@test.com",
            Password = password,
            ConfirmPassword = confirmPassword,
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignUp_InvalidEmail_Returns400()
    {
        var request = new
        {
            Email = "not-an-email",
            Password = "StrongP@ss1",
            ConfirmPassword = "StrongP@ss1",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignUp_PasswordMismatch_Returns400()
    {
        var request = new
        {
            Email = $"mismatch-{Guid.NewGuid():N}@test.com",
            Password = "StrongP@ss1",
            ConfirmPassword = "DifferentP@ss2",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/auth/verify-email — Invalid token

    [Fact]
    public async Task VerifyEmail_InvalidToken_Returns401()
    {
        var request = new
        {
            Email = "nonexistent@test.com",
            Token = "invalid-token-value-that-wont-match"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/auth/forgot-password

    [Fact(Skip = "ExecuteDeleteAsync not supported by InMemory provider")]
    public async Task ForgotPassword_ExistingUser_Returns200()
    {
        var email = $"forgot-exist-{Guid.NewGuid():N}@test.com";
        await CreateTestUser(email);

        var request = new { Email = email };
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_NonExistentEmail_Returns404()
    {
        var request = new { Email = $"nonexist-{Guid.NewGuid():N}@test.com" };

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/auth/reset-password — Invalid token

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns401()
    {
        var request = new
        {
            Email = "someone@test.com",
            Token = "bogus-reset-token",
            NewPassword = "NewStrong@1",
            ConfirmPassword = "NewStrong@1"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_PasswordMismatch_Returns400()
    {
        var request = new
        {
            Email = "someone@test.com",
            Token = "some-token",
            NewPassword = "NewStrong@1",
            ConfirmPassword = "Different@2"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/auth/resend-verification

    [Fact(Skip = "ExecuteDeleteAsync not supported by InMemory provider")]
    public async Task ResendVerification_ExistingUser_Returns200()
    {
        var email = $"resend-{Guid.NewGuid():N}@test.com";
        await CreateTestUser(email);

        var request = new { Email = email, Type = "EMAIL" };
        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendVerification_NonExistentEmail_Returns404()
    {
        var request = new { Email = $"nonexist-{Guid.NewGuid():N}@test.com", Type = "EMAIL" };

        var response = await _client.PostAsJsonAsync("/api/auth/resend-verification", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helpers

    private async Task CreateTestUser(string email)
    {
        var request = new
        {
            Email = email,
            Password = "StrongP@ss1",
            ConfirmPassword = "StrongP@ss1",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private record UserCreatedResponse(Guid UserId, string Email);

    #endregion
}
