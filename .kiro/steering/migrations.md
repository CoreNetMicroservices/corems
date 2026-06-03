---
inclusion: fileMatch
fileMatchPattern: "**/Migrations/**"
---

# Database Migrations Guide (.NET / EF Core)

## Overview

CoreMS uses EF Core Migrations for schema management. Each service has its own DbContext (extending `CoreMsDbContext`) and migration history, isolated by PostgreSQL schema.

## Structure

```
src/<service>-ms/CoreMs.<Service>Ms.Infrastructure/
├── Data/
│   ├── <Service>MsDbContext.cs
│   ├── Configurations/
│   │   └── <Entity>EntityConfiguration.cs
│   ├── DesignTimeDbContextFactory.cs
│   └── SeedData.cs
└── Migrations/
    ├── 20250527112712_InitialCreate.cs
    └── <Service>MsDbContextModelSnapshot.cs
```

Note: Repositories live in `CoreMs.<Service>Ms.Core/Repositories/`, not Infrastructure.

## Schemas

| Schema | Service |
|--------|---------|
| `user_ms` | UserMs |
| `document_ms` | DocumentMs |
| `communication_ms` | CommunicationMs |
| `translation_ms` | TranslationMs |
| `template_ms` | TemplateMs |

## DbContext (One-Liner)

```csharp
public class UserMsDbContext(DbContextOptions<UserMsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "user_ms";
}
```

The base `CoreMsDbContext` handles:
- `HasDefaultSchema(SchemaName)` — all tables go into the service schema
- `ApplyConfigurationsFromAssembly(GetType().Assembly)` — auto-discovers all `IEntityTypeConfiguration<T>` classes

## Migration Commands

```powershell
# Add a new migration
dotnet ef migrations add <MigrationName> `
    --project user-ms/src/CoreMs.UserMs.Infrastructure `
    --startup-project user-ms/src/CoreMs.UserMs.Api

# Apply migrations
dotnet ef database update `
    --project user-ms/src/CoreMs.UserMs.Infrastructure `
    --startup-project user-ms/src/CoreMs.UserMs.Api

# Remove last migration (if not applied)
dotnet ef migrations remove `
    --project user-ms/src/CoreMs.UserMs.Infrastructure `
    --startup-project user-ms/src/CoreMs.UserMs.Api

# Generate SQL script (for production)
dotnet ef migrations script `
    --project user-ms/src/CoreMs.UserMs.Infrastructure `
    --startup-project user-ms/src/CoreMs.UserMs.Api `
    --output migrations/user_ms.sql
```

### For other services, replace paths:
```powershell
dotnet ef migrations add <Name> `
    --project <service>-ms/src/CoreMs.<Service>Ms.Infrastructure `
    --startup-project <service>-ms/src/CoreMs.<Service>Ms.Api
```

All commands run from the `backend/` directory.

## Naming Conventions

### Migration Names
- PascalCase descriptive names
- Examples: `InitialCreate`, `AddUserEmailVerification`, `AddLoginTokenTable`, `AddIndexOnUserEmail`

## Best Practices

### Development
- **Pre-release**: Migrations can be squashed/recreated freely
- **Post-release**: Never modify existing migrations — always add new ones
- **Seed data**: Use `HasData()` in entity configurations for reference data

### Configuration Rules
- Use `UseIdentityAlwaysColumn()` for BIGSERIAL PKs
- Use `HasDefaultValueSql("NOW()")` for timestamp columns
- Always specify explicit table names in `ToTable()`
- Always specify `OnDelete` behavior on relationships

### Indexes
```csharp
builder.HasIndex(e => e.Email).IsUnique();
builder.HasIndex(e => e.CreatedAt);

// Filtered index (PostgreSQL)
builder.HasIndex(e => e.PhoneNumber)
    .IsUnique()
    .HasFilter("phone_number IS NOT NULL");
```

### Foreign Keys
```csharp
builder.HasOne(r => r.User)
    .WithMany(u => u.Roles)
    .HasForeignKey(r => r.UserId)
    .OnDelete(DeleteBehavior.Cascade);
```

## Entity Sync

Always ensure migrations reflect the current entity state. Run `dotnet ef migrations add` after any entity or configuration change.
