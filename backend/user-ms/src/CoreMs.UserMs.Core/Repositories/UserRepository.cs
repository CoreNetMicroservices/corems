using CoreMs.Common.Repository;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Core.Repositories;

[Repository]
public class UserRepository(DbContext context) : SearchableRepository<UserEntity>(context)
{
    protected override IReadOnlySet<string> SearchFields => new HashSet<string> { "Email", "FirstName", "LastName" };
    protected override IReadOnlySet<string> SortFields => new HashSet<string> { "CreatedAt", "Email", "FirstName", "LastName" };
    protected override IReadOnlySet<string> FilterFields => new HashSet<string> { "Provider", "EmailVerified", "CreatedAt" };

    protected override IQueryable<UserEntity> BaseQuery() => DbSet.Include(u => u.Roles);

    public virtual async Task<UserEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Uuid == uuid, ct);

    public virtual async Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await DbSet.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == email, ct);

    public virtual async Task<UserEntity?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);

    public virtual async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await DbSet.AnyAsync(u => u.Email == email, ct);

    public virtual async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default)
        => await DbSet.AnyAsync(u => u.PhoneNumber == phoneNumber, ct);
}
