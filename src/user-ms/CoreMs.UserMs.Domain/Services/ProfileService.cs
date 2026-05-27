using CoreMs.Common.Exceptions;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Exceptions;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;

    public ProfileService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserEntity> UpdateProfileAsync(Guid userUuid, ProfileUpdateRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUuidAsync(userUuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {userUuid} not found");

        if (request.PhoneNumber != null && request.PhoneNumber != user.PhoneNumber)
        {
            var existing = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, ct);
            if (existing != null && existing.Uuid != user.Uuid)
                throw ServiceException.Of(UserErrors.UserExists, "Phone number already in use");
        }

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.PhoneNumber != null)
        {
            user.PhoneNumber = request.PhoneNumber;
            user.PhoneVerified = false;
        }
        if (request.ImageUrl != null) user.ImageUrl = request.ImageUrl;

        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);

        return user;
    }

    public async Task ChangePasswordAsync(Guid userUuid, string oldPassword, string newPassword, string confirmPassword, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUuidAsync(userUuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {userUuid} not found");

        if (user.Password != null && !BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            throw ServiceException.Of(UserErrors.PasswordMismatch, "Wrong password");

        if (newPassword != confirmPassword)
            throw ServiceException.Of(UserErrors.PasswordMismatch, "Password confirmation does not match");

        // Social-only users gain local provider on password set
        if (!user.Provider.Contains("local"))
            user.Provider = user.Provider + ",local";

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);
    }
}
