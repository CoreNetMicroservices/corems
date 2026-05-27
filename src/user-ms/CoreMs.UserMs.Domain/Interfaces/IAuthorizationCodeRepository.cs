using CoreMs.Common.Query;
using CoreMs.UserMs.Domain.Entities;

namespace CoreMs.UserMs.Domain.Interfaces;

public interface IAuthorizationCodeRepository : ICrudRepository<AuthorizationCodeEntity>
{
    Task<AuthorizationCodeEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task DeleteExpiredAsync(CancellationToken ct = default);
}
