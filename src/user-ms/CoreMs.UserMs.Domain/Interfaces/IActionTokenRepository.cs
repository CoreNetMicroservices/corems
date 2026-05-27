using CoreMs.Common.Query;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Enums;

namespace CoreMs.UserMs.Domain.Interfaces;

public interface IActionTokenRepository : ICrudRepository<ActionTokenEntity>
{
    Task<ActionTokenEntity?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<ActionTokenEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default);
    Task DeleteByUserIdAndActionTypeAsync(long userId, ActionTokenType actionType, CancellationToken ct = default);
    Task DeleteExpiredAsync(CancellationToken ct = default);
}
