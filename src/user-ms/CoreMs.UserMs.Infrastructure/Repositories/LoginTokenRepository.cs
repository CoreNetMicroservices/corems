using CoreMs.Common.Data;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Infrastructure.Repositories;

public class LoginTokenRepository : CrudRepository<LoginTokenEntity>, ILoginTokenRepository
{
    public LoginTokenRepository(UserMsDbContext context) : base(context) { }

    public async Task<LoginTokenEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.Include(t => t.User).FirstOrDefaultAsync(t => t.Uuid == uuid, ct);

    public async Task<LoginTokenEntity?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await DbSet.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == token, ct);

    public async Task DeleteAllByUserIdAsync(long userId, CancellationToken ct = default)
    {
        await DbSet
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        await DbSet
            .Where(t => t.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
