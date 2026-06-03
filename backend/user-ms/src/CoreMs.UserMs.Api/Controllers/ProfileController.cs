using CoreMs.Common.Security;
using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

/// <summary>
/// Handles authenticated user profile operations.
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;
    private readonly UserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public ProfileController(
        ProfileService profileService,
        UserService userService,
        ICurrentUserService currentUserService)
    {
        _profileService = profileService;
        _userService = userService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Gets the authenticated user's profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoDto>> GetProfile(CancellationToken ct)
    {
        var userUuid = _currentUserService.GetCurrentUserUuid();
        var user = await _userService.GetUserByUuidAsync(userUuid, ct);
        return Ok(user.ToUserInfoDto());
    }

    /// <summary>
    /// Updates the authenticated user's profile fields (firstName, lastName, phone, avatarUrl).
    /// </summary>
    [HttpPatch]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoDto>> UpdateProfile([FromBody] ProfileUpdateRequest request, CancellationToken ct)
    {
        var userUuid = _currentUserService.GetCurrentUserUuid();
        var user = await _profileService.UpdateProfileAsync(userUuid, request, ct);
        return Ok(user.ToUserInfoDto());
    }

    /// <summary>
    /// Changes the authenticated user's password after verifying the current password.
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userUuid = _currentUserService.GetCurrentUserUuid();
        await _profileService.ChangePasswordAsync(userUuid, request.OldPassword, request.NewPassword, request.ConfirmPassword, ct);
        return Ok(new { result = true });
    }
}
