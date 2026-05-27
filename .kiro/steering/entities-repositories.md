---
inclusion: fileMatch
fileMatchPattern: "**/{Entities,Repositories,Data}/**"
---

# Entities & Repositories (.NET / EF Core)

## Entity Rules

- Plain C# classes — no EF Core attributes, no base entity class
- Suffix with `Entity` to avoid DTO conflicts: `UserEntity`, `RoleEntity`
- Use `long Id` (BIGSERIAL) for internal PKs, `Guid Uuid` for external identifiers
- Timestamps end with `At`: `CreatedAt`, `UpdatedAt`, `LastLoginAt`
- Booleans start with `Is`: `IsActive`, `IsVerified`, `IsDeleted`
- Initialize collections to empty lists, Guids with `Guid.NewGuid()`

```csharp
public class UserEntity
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserRoleEntity> Roles { get; set; } = new List<UserRoleEntity>();
}
```

## EF Core Configuration (Fluent API Only)

Each entity gets a separate `IEntityTypeConfiguration<T>` class in the Infrastructure layer:

```csharp
public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("app_user");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn();

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.Email).IsRequired().HasMaxLength(255);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");

        builder.HasMany(e => e.Roles)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**Never use** `[Key]`, `[Required]`, `[MaxLength]` or any data annotations on entities.

## DbContext

`CoreMsDbContext` is the abstract base class. Each service creates a one-liner subclass:

```csharp
// Base class in CoreMs.Common.Data
public abstract class CoreMsDbContext : DbContext
{
    protected abstract string SchemaName { get; }
    protected CoreMsDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}

// Service-specific — one liner
public class UserMsDbContext(DbContextOptions<UserMsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "user_ms";
}
```

No `DbSet<T>` properties needed — access entities via `Set<T>()` in repositories.

## Repository Hierarchy

```
ICrudRepository<T>           — basic CRUD (Query layer)
  └─ ISearchableRepository<T>  — adds GetPagedAsync (Query layer)
       └─ IUserRepository       — service-specific methods (Domain layer)
```

### ICrudRepository<T>

```csharp
public interface ICrudRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
```

`Add`, `Update`, `Remove` are **synchronous** — they only track changes in memory. Actual DB write happens via auto-save middleware.

### CrudRepository<T>

```csharp
public abstract class CrudRepository<TEntity>(DbContext context) : ICrudRepository<TEntity>
    where TEntity : class
{
    protected readonly DbContext Context = context;
    protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public virtual void Add(TEntity entity) => DbSet.Add(entity);
    public virtual void Update(TEntity entity) => DbSet.Update(entity);
    public virtual void Remove(TEntity entity) => DbSet.Remove(entity);
}
```

### SearchableRepository<T>

Extends `CrudRepository<T>` with dynamic search, filter, sort, and pagination. Subclasses declare:

```csharp
public class UserRepository : SearchableRepository<UserEntity>, IUserRepository
{
    public UserRepository(UserMsDbContext context) : base(context) { }

    protected override IReadOnlySet<string> SearchFields => new HashSet<string> { "Email", "FirstName", "LastName" };
    protected override IReadOnlySet<string> SortFields => new HashSet<string> { "CreatedAt", "Email", "FirstName", "LastName" };
    protected override IReadOnlySet<string> FilterFields => new HashSet<string> { "Provider", "EmailVerified", "CreatedAt" };

    protected override IQueryable<UserEntity> BaseQuery() => DbSet.Include(u => u.Roles);

    // Service-specific queries
    public async Task<UserEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
        => await DbSet.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Uuid == uuid, ct);
}
```

Property metadata is cached at construction time — no reflection at query time.

## Auto-Save Middleware

`AutoSaveChangesMiddleware` calls `SaveChangesAsync()` at the end of every successful request (status < 400). Repositories never call `SaveChanges` themselves.

```csharp
// Registered in the middleware pipeline
app.UseMiddleware<AutoSaveChangesMiddleware>();
```

This means:
- All changes in a request are flushed together (implicit unit of work)
- If the request throws, nothing is saved (automatic rollback)
- Repository methods just track changes — they don't persist

## Query Parameters

```csharp
public class QueryParameters
{
    public int Page { get; set; }       // default 1, min 1
    public int PageSize { get; set; }   // default 20, max 100
    public string? Sort { get; set; }   // format: "field:asc" or "field:desc"
    public string? Search { get; set; } // free-text across SearchFields
    public List<string>? Filters { get; set; } // format: "field:operation:value"
}
```

### Filter operations
`eq`, `ne`, `like`, `in`, `gt`, `gte`, `lt`, `lte`

Examples:
- `isActive:eq:true`
- `createdAt:gte:2024-01-01`
- `provider:in:google,github`
- `email:like:@example.com`

### Sort format
- `createdAt:desc` — sort by createdAt descending
- `email:asc` — sort by email ascending
- Default (no sort param): first SortField descending

## Service-Specific Repository Interface (Domain Layer)

```csharp
public interface IUserRepository : ISearchableRepository<UserEntity>
{
    Task<UserEntity?> GetByUuidAsync(Guid uuid, CancellationToken ct = default);
    Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
}
```

## Rules

1. **No EF attributes on entities** — Fluent API only
2. **No base entity class** — entities are plain POCOs
3. **No SaveChanges in repositories** — auto-save middleware handles it
4. **Always accept CancellationToken** in async methods
5. **Use `FirstOrDefaultAsync`** (not `SingleOrDefaultAsync`) for lookups
6. **Use `AnyAsync`** for existence checks
7. **One repository per aggregate root**
