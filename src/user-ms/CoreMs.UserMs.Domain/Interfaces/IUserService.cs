using CoreMs.Common.Query;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Interfaces;

/// <summary>
/// Admin-facing user CRUD operations.
/// </summary>
public interface IUserService
{
    Task<UserEntity> GetUserByUuidAsync(Guid uuid, CancellationToken ct = default);
    Task<PagedResult<UserEntity>> GetAllUsersAsync(QueryParameters parameters, CancellationToken ct = default);
    Task<UserEntity> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task UpdateUserAsync(Guid uuid, UserUpdateRequest request, CancellationToken ct = default);
    Task DeleteUserAsync(Guid uuid, CancellationToken ct = default);
    Task AdminChangePasswordAsync(Guid uuid, string newPassword, string confirmPassword, CancellationToken ct = default);
    Task AdminChangeEmailAsync(Guid uuid, string newEmail, CancellationToken ct = default);
}
