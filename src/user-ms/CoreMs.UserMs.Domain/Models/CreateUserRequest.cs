namespace CoreMs.UserMs.Domain.Models;

public record CreateUserRequest(
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    List<string>? Roles);
