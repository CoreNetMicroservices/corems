using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.UserMs.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IPasswordService _passwordService;

    public AuthController(IRegistrationService registrationService, IPasswordService passwordService)
    {
        _registrationService = registrationService;
        _passwordService = passwordService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request, CancellationToken ct)
    {
        var user = await _registrationService.SignUpAsync(request, ct);
        return Created(string.Empty, new { uuid = user.Uuid });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        await _registrationService.VerifyEmailAsync(request.Email, request.Token, ct);
        return Ok(new { result = true });
    }

    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request, CancellationToken ct)
    {
        await _registrationService.VerifyPhoneAsync(request.PhoneNumber, request.Code, ct);
        return Ok(new { result = true });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken ct)
    {
        await _registrationService.ResendVerificationAsync(request.Email, request.Type, ct);
        return Ok(new { result = true });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _passwordService.ForgotPasswordAsync(request.Email, ct);
        return Ok(new { result = true });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _passwordService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, request.ConfirmPassword, ct);
        return Ok(new { result = true });
    }
}

public record VerifyEmailRequest(string Email, string Token);
public record VerifyPhoneRequest(string PhoneNumber, string Code);
public record ResendVerificationRequest(string Email, string Type);
public record ForgotPasswordRequest(string Email);
