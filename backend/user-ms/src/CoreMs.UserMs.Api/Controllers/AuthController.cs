using CoreMs.Common.Exceptions;
using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

/// <summary>
/// Public authentication endpoints for signup, verification, and password management.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Produces("application/json")]
public class AuthController(
    RegistrationService registrationService,
    PasswordService passwordService) : ControllerBase
{
    /// <summary>
    /// Register a new user account.
    /// </summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(UserCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserCreatedDto>> SignUp([FromBody] SignUpRequest request, CancellationToken ct)
    {
        var user = await registrationService.SignUpAsync(request, ct);
        var result = new UserCreatedDto(user.Uuid, user.Email);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Verify email address with a verification token.
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status410Gone)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        await registrationService.VerifyEmailAsync(request.Email, request.Token, ct);
        return Ok(new { result = true });
    }

    /// <summary>
    /// Verify phone number with a verification code.
    /// </summary>
    [HttpPost("verify-phone")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status410Gone)]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request, CancellationToken ct)
    {
        await registrationService.VerifyPhoneAsync(request.PhoneNumber, request.Code, ct);
        return Ok(new { result = true });
    }

    /// <summary>
    /// Resend email or phone verification. Always returns 200 to prevent enumeration.
    /// </summary>
    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken ct)
    {
        await registrationService.ResendVerificationAsync(request.Email, request.Type, ct);
        return Ok(new { result = true });
    }

    /// <summary>
    /// Request a password reset email. Always returns 200 to prevent email enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await passwordService.ForgotPasswordAsync(request.Email, ct);
        return Ok(new { result = true });
    }

    /// <summary>
    /// Reset password using a valid reset token.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status410Gone)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await passwordService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, request.ConfirmPassword, ct);
        return Ok(new { result = true });
    }
}
