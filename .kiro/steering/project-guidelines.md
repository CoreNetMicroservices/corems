---
inclusion: always
---

# Core Microservices (.NET) - Project Guidelines

## Project Context

Enterprise-grade microservices toolkit in C# / ASP.NET Core 10. Demonstrates modern .NET architecture patterns for rapid application development.

**Original Java Project**: https://github.com/CoreWebMicroservices/corems-project
**Architecture**: Monorepo with .NET Solution structure and .NET Aspire orchestration

## Repository Structure

```
corems-parent/
├── backend/                          # .NET Backend
│   ├── aspire/                       # .NET Aspire
│   │   ├── CoreMs.AppHost/           # Orchestrator
│   │   └── CoreMs.ServiceDefaults/   # Shared service config
│   ├── common/                       # Shared libraries
│   │   ├── src/
│   │   │   ├── CoreMs.Common/        # Exceptions, Repository, Data, Middleware, Extensions
│   │   │   ├── CoreMs.Common.Contracts/  # Shared DTOs, contracts
│   │   │   └── CoreMs.Common.Security/   # JWT, auth middleware, RBAC
│   │   └── test/
│   │       └── CoreMs.Common.Tests/
│   ├── user-ms/                      # User management service
│   │   ├── src/
│   │   │   ├── CoreMs.UserMs.Api/
│   │   │   ├── CoreMs.UserMs.Core/
│   │   │   └── CoreMs.UserMs.Infrastructure/
│   │   └── test/
│   │       ├── CoreMs.UserMs.Tests/
│   │       └── CoreMs.UserMs.IntegrationTests/
│   ├── local-packages/               # Local NuGet packages
│   ├── CoreMs.slnx                   # Solution file
│   ├── Directory.Build.props         # net10.0, nullable, implicit usings
│   ├── Directory.Packages.props      # Central package management
│   └── nuget.config
├── docker/                           # Infrastructure (PostgreSQL, RabbitMQ, MinIO)
└── README.md
```

## Development Workflow

### Running the Full Stack (Recommended)
```powershell
# Start everything via Aspire (from backend/ directory)
dotnet run --project aspire/CoreMs.AppHost
```

This starts PostgreSQL (Docker), pgAdmin, user-ms, and frontend together. On first run it auto-migrates and seeds test data.

### Secrets Management
All secrets live in a single file: `aspire/CoreMs.AppHost/appsettings.Development.json` (gitignored).
The AppHost distributes secrets to services via `.WithEnvironment()`. Individual services never store secrets in their own appsettings.

### Standalone Commands
```powershell
# Restore and build
dotnet restore
dotnet build

# Run user-ms standalone (requires Postgres on port 5432)
dotnet run --project user-ms/src/CoreMs.UserMs.Api

# Run all tests
dotnet test

# Add migration
dotnet ef migrations add <Name> `
    --project user-ms/src/CoreMs.UserMs.Infrastructure `
    --startup-project user-ms/src/CoreMs.UserMs.Api
```

All commands run from the `backend/` directory.

### Aspire Configuration
- PostgreSQL: fixed port 5432, stable creds (postgres/postgres), persistent Docker volume
- Secrets injected to services via `WithEnvironment("Section__Key", value)`
- Frontend gets backend URL dynamically via `REACT_USER_MS_BASE_URL` env var
- Auto-migrate + auto-seed in Development mode (idempotent)

## Service Structure

Each service uses a three-layer structure: **Api**, **Core**, and **Infrastructure**.

```
<service>-ms/
├── CoreMs.<Service>Ms.Api/              # Host layer
│   ├── Configuration/                   # Options classes (bound to appsettings)
│   ├── Controllers/
│   ├── Filters/                         # Action filters (e.g., ValidationFilter)
│   ├── Services/                        # Background services (IHostedService)
│   ├── Validators/                      # FluentValidation validators
│   ├── Program.cs
│   └── Properties/launchSettings.json
├── CoreMs.<Service>Ms.Core/             # Business logic + data access contracts
│   ├── Configuration/                   # Options classes used by services
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Models/                          # DTOs, request/response models
│   ├── Repositories/                    # Concrete repositories (extend SearchableRepository)
│   └── Services/                        # Business logic services
└── CoreMs.<Service>Ms.Infrastructure/   # EF Core specifics
    ├── Data/                            # DbContext + EntityTypeConfigurations
    └── Migrations/
```

### Layer Responsibilities

- **Api**: HTTP concerns — controllers, validators, options binding, background services, `Program.cs`
- **Core**: Business logic + repositories. Has EF Core dependency (for `DbContext` in constructors). Contains entities, services, repositories, models, exceptions.
- **Infrastructure**: EF Core configuration (Fluent API) and migrations only. No repositories live here.

### Key Differences from Traditional Clean Architecture

- **Repositories live in Core** (not Infrastructure) — they depend on `DbContext` directly
- **No separate interfaces** for services/repositories — concrete classes with `[Service]`/`[Repository]` attributes
- **Core has EF Core dependency** — it's not a "pure domain" layer; it's practical

## Service URLs & Ports

### Development Environment
- **User Service**: http://localhost:5100

### Infrastructure
- **PostgreSQL**: localhost:5432
- **RabbitMQ**: localhost:5672 (Management: 15672)

## Key Shared Libraries

| Package | Purpose |
|---------|---------|
| `CoreMs.Common` | Exceptions, Repository (CrudRepository, SearchableRepository, QueryParameters, PagedResult), Data (CoreMsDbContext), Middleware (GlobalExceptionHandler, AutoSaveChangesMiddleware), Extensions ([Service], [Repository], AddCoreMsServices) |
| `CoreMs.Common.Contracts` | Shared DTOs and API contracts |
| `CoreMs.Common.Security` | JWT validation, ICurrentUserService, RBAC |

## Auto-Registration Convention

Services and repositories are auto-registered via attributes + assembly scanning:

```csharp
// In Program.cs
builder.Services.AddCoreMsServices(typeof(UserService).Assembly);

// In service/repository classes
[Service]   // Registered as scoped by default
public class UserService(UserRepository userRepository) { }

[Repository]  // Registered as scoped by default
public class UserRepository(DbContext context) : SearchableRepository<UserEntity>(context) { }
```

If a class implements `IClassName`, it registers as the interface. Otherwise it registers as itself (concrete type).

## Code Standards
- **Three-layer architecture**: Api → Core → Infrastructure
- **Dependency Injection**: Built-in .NET DI container with auto-registration
- **Minimal comments**: Code should be self-explanatory
- **Nullable reference types**: Enabled globally, no warnings allowed
- **Central package management**: All versions in `Directory.Packages.props`
- **FluentValidation**: Request validation in Api layer

## Database Guidelines
- **Schema per service**: `user_ms`, `communication_ms`, etc.
- **UUID for external IDs**: `Guid` type for public-facing identifiers
- **Long for internal IDs**: `long` for primary keys (BIGSERIAL)
- **Fluent API only**: No data annotations on entities
- **Auto-save middleware**: Repositories never call SaveChanges

## Testing Strategy

### Unit Tests
- xUnit + NSubstitute
- Test business logic in Core layer
- Mock repository dependencies

### Integration Tests
- `WebApplicationFactory<Program>` for real HTTP testing
- Testcontainers for PostgreSQL
- Test full request/response cycle
- Real authentication flow (no mocked tokens)

## Git & Commit Standards
- Conventional commit format: `feat:`, `fix:`, `refactor:`
- Never commit secrets or appsettings with real credentials
- Use `dotnet user-secrets` for local development
