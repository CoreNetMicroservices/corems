---
inclusion: fileMatch
fileMatchPattern: "**/*.cs"
---

# Backend Architecture - Core Microservices (.NET)

## Platform
- .NET 10 / ASP.NET Core 10
- C# 13
- PostgreSQL database
- JWT Authentication (OpenIddict)
- RabbitMQ via MassTransit
- Entity Framework Core 10
- .NET Aspire for orchestration

## Solution Structure

```
corems-parent/
├── src/
│   ├── aspire/                    # .NET Aspire orchestration
│   │   ├── CoreMs.AppHost/
│   │   └── CoreMs.ServiceDefaults/
│   ├── common/                    # Shared libraries
│   │   ├── CoreMs.Common/         # Exceptions/, Query/, Data/, Middleware/
│   │   ├── CoreMs.Common.Contracts/ # Shared DTOs, contracts
│   │   └── CoreMs.Common.Security/  # JWT, auth middleware, RBAC
│   └── user-ms/                   # User management service
│       ├── CoreMs.UserMs.Api/
│       ├── CoreMs.UserMs.Domain/
│       └── CoreMs.UserMs.Infrastructure/
├── tests/                         # All tests (top-level)
│   ├── CoreMs.Common.Tests/
│   ├── CoreMs.UserMs.Tests/
│   └── CoreMs.UserMs.IntegrationTests/
├── CoreMs.slnx                    # Solution file
├── Directory.Build.props          # Shared build properties (net10.0)
└── Directory.Packages.props       # Central package management
```

## CoreMs.Common Package

Single shared library containing all common infrastructure:

```
CoreMs.Common/
├── Exceptions/    # ErrorInfo, Error, ErrorResponse, ServiceException, DefaultErrors
├── Query/         # ICrudRepository, ISearchableRepository, QueryParameters, PagedResult, FilterParser
├── Data/          # CoreMsDbContext, CrudRepository, SearchableRepository
└── Middleware/    # GlobalExceptionHandler, AutoSaveChangesMiddleware
```

There is no `CoreMs.Common.Api` project — contracts live in `CoreMs.Common.Contracts`.

## Shared Infrastructure Approach

- Reference `CoreMs.Common` and `CoreMs.Common.Security` as project references
- Service `Program.cs` should be minimal — use extension methods for registration
- DO NOT duplicate middleware or DI registration across services
- Common library wires exception handling, auto-save, and base repository classes

### Dependencies (Directory.Packages.props)
- Central package version management
- All services reference the same package versions
- No package version overrides in individual .csproj files

## Service Folder Layout

```
user-ms/
├── CoreMs.UserMs.Api/              # Host + Controllers
│   ├── Controllers/
│   ├── Program.cs
│   ├── Properties/launchSettings.json
│   ├── appsettings.json
│   └── appsettings.Development.json
├── CoreMs.UserMs.Domain/           # Business logic (no infra deps)
│   ├── Entities/
│   ├── Interfaces/
│   ├── Services/
│   ├── Enums/
│   └── Exceptions/
└── CoreMs.UserMs.Infrastructure/   # Data access
    ├── Data/                       # DbContext + EntityTypeConfigurations
    ├── Repositories/
    └── Migrations/
```

## Code Style

### Namespace Convention
- `CoreMs.<Service>Ms.<Layer>` (e.g., `CoreMs.UserMs.Domain.Entities`)

### Commenting Policy (Strict)
**Goal**: Keep code self-explanatory. Comments are exceptional.

**Allowed comments**:
1. Short rationale for non-obvious decisions (1-2 lines)
2. Links to external issues/specs for workarounds
3. XML docs for public API methods on controllers and service interfaces

**Remove all comments that restate code**:
- ❌ `// set userId`
- ❌ `// populate sender info`

**Formatting**:
- Use XML docs (`///`) for public APIs (required for controller actions and service interfaces)
- Use `//` for short rationale only
- Tag actionable items: `TODO:` / `FIXME:` with owner/ticket

## PR Checklist
- ✅ Avoided editing `Common` projects without discussion
- ✅ Domain layer has zero infrastructure references
- ✅ No package version overrides in individual .csproj files
- ✅ Migration changes sync with entity changes
- ✅ Nullable reference types respected (no warnings)
