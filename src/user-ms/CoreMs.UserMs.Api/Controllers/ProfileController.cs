using CoreMs.Common.Security;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ICurrentUserService _currentUserService;

    public ProfileController(IProfileService profileService, ICurrentUserService currentUserService)
    {
        _profileService = profileService;
        _currentUserService = currentUserService;
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request, CancellationToken ct)
    {
        var userUuid = _currentUserService.GetCurrentUserUuid();
        var user = await _profileService.UpdateProfileAsync(userUuid, request, ct);
        return Ok(user);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userUuid = _currentUserService.GetCurrentUserUuid();
        await _profileService.ChangePasswordAsync(userUuid, request.OldPassword, request.NewPassword, request.ConfirmPassword, ct);
        return Ok(new { result = true });
    }
}
