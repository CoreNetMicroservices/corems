namespace CoreMs.UserMs.Domain.Models;

public record ProfileUpdateRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? ImageUrl);
