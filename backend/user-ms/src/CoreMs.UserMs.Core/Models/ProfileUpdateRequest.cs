namespace CoreMs.UserMs.Core.Models;

public record ProfileUpdateRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? ImageUrl);
