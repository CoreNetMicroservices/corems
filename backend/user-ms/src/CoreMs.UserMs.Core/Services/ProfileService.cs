using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Models;
using CoreMs.UserMs.Core.Repositories;

namespace CoreMs.UserMs.Core.Services;

[Service]
public class ProfileService(UserRepository userRepository)
{
    public async Task<UserEntity> UpdateProfileAsync(Guid userUuid, ProfileUpdateRequest request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByUuidAsync(userUuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {userUuid} not found");

        if (request.PhoneNumber != null && request.PhoneNumber != user.PhoneNumber)
        {
            var existing = await userRepository.GetByPhoneNumberAsync(request.PhoneNumber, ct);
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
        userRepository.Update(user);

        return user;
    }

    public async Task ChangePasswordAsync(Guid userUuid, string oldPassword, string newPassword, string confirmPassword, CancellationToken ct = default)
    {
        var user = await userRepository.GetByUuidAsync(userUuid, ct)
            ?? throw ServiceException.Of(UserErrors.UserNotFound, $"User with ID {userUuid} not found");

        if (user.Password != null && !BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            throw ServiceException.Of(UserErrors.PasswordMismatch, "Wrong password");

        if (newPassword != confirmPassword)
            throw ServiceException.Of(UserErrors.PasswordMismatch, "Password confirmation does not match");

        if (!user.Provider.Contains("local"))
            user.Provider = user.Provider + ",local";

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
    }
}
