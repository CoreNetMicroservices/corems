using CoreMs.Common.Query;
using CoreMs.UserMs.Domain.Entities;

namespace CoreMs.UserMs.Domain.Interfaces;

public interface ILoginTokenRepository : ICrudRepository<LoginTokenEntity>
{
    Task<LoginTokenEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default);
    Task<LoginTokenEntity?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task DeleteAllByUserIdAsync(long userId, CancellationToken ct = default);
    Task DeleteExpiredAsync(CancellationToken ct = default);
}
