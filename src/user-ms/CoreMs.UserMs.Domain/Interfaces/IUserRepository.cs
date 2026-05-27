using CoreMs.Common.Query;
using CoreMs.UserMs.Domain.Entities;

namespace CoreMs.UserMs.Domain.Interfaces;

public interface IUserRepository : ISearchableRepository<UserEntity>
{
    Task<UserEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default);
    Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<UserEntity?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);
}
