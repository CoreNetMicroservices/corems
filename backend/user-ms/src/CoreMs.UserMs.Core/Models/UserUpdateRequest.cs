namespace CoreMs.UserMs.Core.Models;

public record UserUpdateRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? ImageUrl,
    List<string>? Roles);
