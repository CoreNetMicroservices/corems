using CoreMs.Common.Data;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Enums;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Infrastructure.Repositories;

public class ActionTokenRepository : CrudRepository<ActionTokenEntity>, IActionTokenRepository
{
    public ActionTokenRepository(UserMsDbContext context) : base(context) { }

    public async Task<ActionTokenEntity?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await DbSet.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<ActionTokenEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.Include(t => t.User).FirstOrDefaultAsync(t => t.Uuid == uuid, ct);

    public async Task DeleteByUserIdAndActionTypeAsync(long userId, ActionTokenType actionType, CancellationToken ct = default)
    {
        await DbSet
            .Where(t => t.UserId == userId && t.ActionType == actionType)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        await DbSet
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);
    }
}
