namespace CoreMs.UserMs.Core.Models;

public record CreateUserRequest(
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    List<string>? Roles);
