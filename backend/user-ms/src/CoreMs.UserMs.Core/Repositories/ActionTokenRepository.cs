using CoreMs.Common.Repository;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Core.Repositories;

[Repository]
public class ActionTokenRepository(DbContext context) : CrudRepository<ActionTokenEntity>(context)
{
    public virtual async Task<ActionTokenEntity?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await DbSet.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public virtual async Task<ActionTokenEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.Include(t => t.User).FirstOrDefaultAsync(t => t.Uuid == uuid, ct);

    public virtual async Task DeleteByUserIdAndActionTypeAsync(long userId, ActionTokenType actionType, CancellationToken ct = default)
    {
        await DbSet
            .Where(t => t.UserId == userId && t.ActionType == actionType)
            .ExecuteDeleteAsync(ct);
    }

    public virtual async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        await DbSet
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);
    }
}
