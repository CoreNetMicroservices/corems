using CoreMs.Common.Data;
using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Interfaces;
using CoreMs.UserMs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.UserMs.Infrastructure.Repositories;

public class UserRepository : SearchableRepository<UserEntity>, IUserRepository
{
    public UserRepository(UserMsDbContext context) : base(context) { }

    protected override IReadOnlySet<string> SearchFields => new HashSet<string> { "Email", "FirstName", "LastName" };
    protected override IReadOnlySet<string> SortFields => new HashSet<string> { "CreatedAt", "Email", "FirstName", "LastName" };
    protected override IReadOnlySet<string> FilterFields => new HashSet<string> { "Provider", "EmailVerified", "CreatedAt" };

    protected override IQueryable<UserEntity> BaseQuery() => DbSet.Include(u => u.Roles);

    public async Task<UserEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Uuid == uuid, ct);

    public async Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await DbSet.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<UserEntity?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await DbSet.AnyAsync(u => u.Email == email, ct);

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default)
        => await DbSet.AnyAsync(u => u.PhoneNumber == phoneNumber, ct);
}
