# CoreMS

Enterprise microservices foundation for rapid application development.

Built with C# / ASP.NET Core 10.


## Services

| Service | Port | Description | Status |
|---------|------|-------------|--------|
| **User MS** | 5100 | Authentication, OAuth2/OIDC, user management, RBAC | ✅ Ready |
| **Communication MS** | 5101 | Email, SMS, push notifications | 🔜 Planned |
| **Document MS** | 5102 | File storage, document management | 🔜 Planned |
| **Translation MS** | 5103 | Internationalization, translation bundles | 🔜 Planned |
| **Template MS** | 5104 | Template management and rendering | 🔜 Planned |

## Shared Foundation

All services build on top of `CoreMs.Common` — a shared library providing:

- **Exception Handling** — Structured error responses with `ServiceException` pattern
- **Repositories** — Generic `SearchableRepository<T>` with dynamic search, filter, sort, pagination
- **Data** — Base `CoreMsDbContext`, auto-save middleware (implicit unit of work)
- **Security** — JWT validation, role-based access, `ICurrentUserService`
- **Validation** — `AddCoreMsValidation()` with auto-discovered FluentValidation validators
- **Messaging** — Shared contracts for inter-service communication via RabbitMQ

## Quick Start

```bash
# Prerequisites: .NET 10 SDK, Docker, Aspire CLI
dotnet tool install -g aspire.cli

# Run everything (PostgreSQL + User MS) via Aspire
cd backend
aspire run --project aspire/CoreMs.AppHost
```

Aspire Dashboard: https://localhost:17178
User MS Swagger: http://localhost:5100/swagger

### Without Aspire

```bash
# Start infrastructure manually
docker compose -f docker/docker-compose.infra.yml up -d

# Run User MS
cd backend
dotnet run --project user-ms/src/CoreMs.UserMs.Api
```

### Seed Test Data

```bash
cd backend
dotnet run --project user-ms/src/CoreMs.UserMs.Api -- --seed
```

**Test credentials:** All seed users have password `Password123!`
- `admin@corems.local` — all admin roles
- `alice.johnson@corems.local` — regular user

## Architecture

```
corems-parent/
├── backend/                          # .NET Backend
│   ├── aspire/                       # .NET Aspire orchestration
│   ├── common/                       # Shared libraries
│   │   ├── src/
│   │   │   ├── CoreMs.Common/        # Exceptions, Repository, Data, Middleware, Extensions
│   │   │   ├── CoreMs.Common.Contracts/  # Messaging DTOs
│   │   │   └── CoreMs.Common.Security/   # JWT, RBAC
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
│   ├── local-packages/
│   ├── CoreMs.slnx
│   ├── Directory.Build.props
│   └── Directory.Packages.props
├── docker/                           # Infrastructure (PostgreSQL, RabbitMQ)
└── README.md
```

Each service follows a three-layer structure:
```
<service>-ms/
├── src/
│   ├── CoreMs.<Service>Ms.Api/              # Host, controllers, validators
│   ├── CoreMs.<Service>Ms.Core/             # Entities, services, repositories
│   └── CoreMs.<Service>Ms.Infrastructure/   # EF Core config, migrations
└── test/
    ├── CoreMs.<Service>Ms.Tests/
    └── CoreMs.<Service>Ms.IntegrationTests/
```

## Tech Stack

- .NET 10 / ASP.NET Core 10
- Entity Framework Core 10 + PostgreSQL
- .NET Aspire for local orchestration
- FluentValidation (auto-discovered via `AddCoreMsValidation`)
- BCrypt.Net for password hashing
- MassTransit + RabbitMQ for messaging
- xUnit + FsCheck + FluentAssertions + NSubstitute

## Adding a New Service

1. Create `backend/<service>-ms/src/` with Api, Core, Infrastructure projects
2. Create `backend/<service>-ms/test/` with Tests and IntegrationTests projects
3. Extend `CoreMsDbContext` with your schema name
4. Extend `SearchableRepository<T>` for your entities with `[Repository]` attribute
5. Add services with `[Service]` attribute
6. Add to `CoreMs.slnx` and run `dotnet build`

## CLI

All commands run from the `backend/` directory:

```bash
aspire run --project aspire/CoreMs.AppHost                      # Run all (Aspire)
dotnet run --project user-ms/src/CoreMs.UserMs.Api              # Run standalone
dotnet run --project user-ms/src/CoreMs.UserMs.Api -- --migrate # Migrate DB
dotnet run --project user-ms/src/CoreMs.UserMs.Api -- --seed    # Seed data
dotnet test                                                      # All tests
dotnet build                                                     # Build
```

> **Git Bash on Windows:** `aspire` is a `.cmd` shim. Use `cmd //c "aspire run ..."` or add `alias aspire='cmd //c aspire'` to `~/.bashrc`.

## Related

Also available as a [Java/Spring Boot edition](https://github.com/CoreWebMicroservices/corems-project) with the same API contracts.
