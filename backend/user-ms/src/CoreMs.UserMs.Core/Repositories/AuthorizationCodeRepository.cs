using CoreMs.Common.Repository;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Core.Repositories;

[Repository]
public class AuthorizationCodeRepository(DbContext context) : CrudRepository<AuthorizationCodeEntity>(context)
{
    public virtual async Task<AuthorizationCodeEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await DbSet.Include(c => c.User).ThenInclude(u => u.Roles).FirstOrDefaultAsync(c => c.Code == code, ct);

    public virtual async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        await DbSet
            .Where(c => c.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);
    }
}
