namespace CoreMs.UserMs.Core.Models;

public record SignUpRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? ImageUrl);
