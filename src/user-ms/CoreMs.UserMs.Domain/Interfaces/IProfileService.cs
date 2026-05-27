using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Interfaces;

/// <summary>
/// Handles authenticated user profile updates and password changes.
/// </summary>
public interface IProfileService
{
    Task<UserEntity> UpdateProfileAsync(Guid userUuid, ProfileUpdateRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userUuid, string oldPassword, string newPassword, string confirmPassword, CancellationToken ct = default);
}
