using CoreMs.Common.Query;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "USER_MS_ADMIN,SUPER_ADMIN")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters parameters, CancellationToken ct)
    {
        var result = await _userService.GetAllUsersAsync(parameters, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _userService.CreateUserAsync(request, ct);
        return Created(string.Empty, new { uuid = user.Uuid });
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken ct)
    {
        var user = await _userService.GetUserByUuidAsync(userId, ct);
        return Ok(user);
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(Guid userId, [FromBody] UserUpdateRequest request, CancellationToken ct)
    {
        await _userService.UpdateUserAsync(userId, request, ct);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken ct)
    {
        await _userService.DeleteUserAsync(userId, ct);
        return NoContent();
    }

    [HttpPost("{userId:guid}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid userId, [FromBody] AdminChangePasswordRequest request, CancellationToken ct)
    {
        await _userService.AdminChangePasswordAsync(userId, request.NewPassword, request.ConfirmPassword, ct);
        return Ok(new { result = true });
    }

    [HttpPost("{userId:guid}/change-email")]
    public async Task<IActionResult> ChangeEmail(Guid userId, [FromBody] AdminChangeEmailRequest request, CancellationToken ct)
    {
        await _userService.AdminChangeEmailAsync(userId, request.NewEmail, ct);
        return Ok(new { result = true });
    }
}

public record AdminChangePasswordRequest(string NewPassword, string ConfirmPassword);
public record AdminChangeEmailRequest(string NewEmail);
