using CoreMs.Common.Repository;
using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

/// <summary>
/// Administrative user management endpoints.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "USER_MS_ADMIN,SUPER_ADMIN")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get a paginated list of users.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserInfoDto>>> GetAll(
        [FromQuery] QueryParameters parameters, CancellationToken ct)
    {
        var result = await _userService.GetAllUsersAsync(parameters, ct);
        var mapped = new PagedResult<UserInfoDto>(
            result.Items.Select(u => u.ToUserInfoDto()).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
        return Ok(mapped);
    }

    /// <summary>
    /// Create a new user with specified roles.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserInfoDto>> Create(
        [FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _userService.CreateUserAsync(request, ct);
        var dto = user.ToUserInfoDto();
        return CreatedAtAction(nameof(GetById), new { userId = dto.Uuid }, dto);
    }

    /// <summary>
    /// Get a user by ID.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserInfoDto>> GetById(Guid userId, CancellationToken ct)
    {
        var user = await _userService.GetUserByUuidAsync(userId, ct);
        return Ok(user.ToUserInfoDto());
    }

    /// <summary>
    /// Update a user's fields including roles and active status.
    /// </summary>
    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<UserInfoDto>> Update(
        Guid userId, [FromBody] UserUpdateRequest request, CancellationToken ct)
    {
        await _userService.UpdateUserAsync(userId, request, ct);
        var user = await _userService.GetUserByUuidAsync(userId, ct);
        return Ok(user.ToUserInfoDto());
    }

    /// <summary>
    /// Delete a user and cascade-delete all related data.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken ct)
    {
        await _userService.DeleteUserAsync(userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Admin change a user's password without requiring the current password.
    /// </summary>
    [HttpPost("{userId:guid}/change-password")]
    public async Task<IActionResult> ChangePassword(
        Guid userId, [FromBody] AdminChangePasswordRequest request, CancellationToken ct)
    {
        await _userService.AdminChangePasswordAsync(userId, request.NewPassword, request.ConfirmPassword, ct);
        return Ok(new { result = true });
    }

    /// <summary>
    /// Admin change a user's email and reset email verification.
    /// </summary>
    [HttpPost("{userId:guid}/change-email")]
    public async Task<IActionResult> ChangeEmail(
        Guid userId, [FromBody] AdminChangeEmailRequest request, CancellationToken ct)
    {
        await _userService.AdminChangeEmailAsync(userId, request.NewEmail, ct);
        return Ok(new { result = true });
    }
}

public record AdminChangePasswordRequest(string NewPassword, string ConfirmPassword);
public record AdminChangeEmailRequest(string NewEmail);
