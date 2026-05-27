using CoreMs.Common.Data;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Infrastructure.Repositories;

public class AuthorizationCodeRepository : CrudRepository<AuthorizationCodeEntity>, IAuthorizationCodeRepository
{
    public AuthorizationCodeRepository(UserMsDbContext context) : base(context) { }

    public async Task<AuthorizationCodeEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await DbSet.Include(c => c.User).FirstOrDefaultAsync(c => c.Code == code, ct);

    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        await DbSet
            .Where(c => c.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);
    }
}
