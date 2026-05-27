using CoreMs.Common.Exceptions;
using CoreMs.Common.Query;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Exceptions;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserEntity> GetUserByUuidAsync(Guid uuid, CancellationToken ct = default)
    {
        return await _userRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {uuid} not found");
    }

    public async Task<PagedResult<UserEntity>> GetAllUsersAsync(QueryParameters parameters, CancellationToken ct = default)
    {
        return await _userRepository.GetPagedAsync(parameters, ct);
    }

    public async Task<UserEntity> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, ct))
            throw ServiceException.Of(UserErrors.UserExists, "User with this email already exists");

        if (request.PhoneNumber != null && await _userRepository.ExistsByPhoneNumberAsync(request.PhoneNumber, ct))
            throw ServiceException.Of(UserErrors.UserExists, "User with this phone number already exists");

        var user = new UserEntity
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Provider = "local",
            EmailVerified = false,
            Password = "{noop}temporary"
        };

        if (request.Roles is { Count: > 0 })
        {
            foreach (var role in request.Roles)
                user.Roles.Add(new UserRoleEntity { Name = role });
        }

        _userRepository.Add(user);
        return user;
    }

    public async Task UpdateUserAsync(Guid uuid, UserUpdateRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {uuid} not found");

        if (request.PhoneNumber != null && request.PhoneNumber != user.PhoneNumber)
        {
            var existing = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, ct);
            if (existing != null && existing.Uuid != user.Uuid)
                throw ServiceException.Of(UserErrors.UserExists, "Phone number is already in use");
        }

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Email != null) user.Email = request.Email;
        if (request.PhoneNumber != null)
        {
            user.PhoneNumber = request.PhoneNumber;
            user.PhoneVerified = false;
        }
        if (request.ImageUrl != null) user.ImageUrl = request.ImageUrl;

        if (request.Roles != null)
        {
            user.Roles.Clear();
            foreach (var role in request.Roles)
                user.Roles.Add(new UserRoleEntity { Name = role });
        }

        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);
    }

    public async Task DeleteUserAsync(Guid uuid, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {uuid} not found");

        _userRepository.Remove(user);
    }

    public async Task AdminChangePasswordAsync(Guid uuid, string newPassword, string confirmPassword, CancellationToken ct = default)
    {
        if (newPassword != confirmPassword)
            throw ServiceException.Of(UserErrors.PasswordMismatch, "New password and confirm password do not match");

        var user = await _userRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {uuid} not found");

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);
    }

    public async Task AdminChangeEmailAsync(Guid uuid, string newEmail, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {uuid} not found");

        user.Email = newEmail;
        user.EmailVerified = false;
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);
    }
}
