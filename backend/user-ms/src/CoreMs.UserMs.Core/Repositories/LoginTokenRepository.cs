using CoreMs.Common.Repository;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Core.Repositories;

[Repository]
public class LoginTokenRepository(DbContext context) : CrudRepository<LoginTokenEntity>(context)
{
    public virtual async Task<LoginTokenEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.Include(t => t.User).ThenInclude(u => u.Roles).FirstOrDefaultAsync(t => t.Uuid == uuid, ct);

    public virtual async Task<LoginTokenEntity?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await DbSet.Include(t => t.User).ThenInclude(u => u.Roles).FirstOrDefaultAsync(t => t.Token == token, ct);

    public virtual async Task DeleteAllByUserIdAsync(long userId, CancellationToken ct = default)
    {
        await DbSet
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }

    public virtual async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        await DbSet
            .Where(t => t.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
