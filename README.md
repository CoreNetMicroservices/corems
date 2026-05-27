# CoreMS

Enterprise microservices toolkit for rapid application development. Built with C# / ASP.NET Core 10.

Pick the services you need, drop the ones you don't, and build on top of a solid foundation.

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
- **Messaging** — Shared contracts for inter-service communication via RabbitMQ

## Quick Start

```bash
# Prerequisites: .NET 10 SDK, Docker

# Start infrastructure
docker compose -f docker/docker-compose.infra.yml up -d

# Apply migrations and seed test data
dotnet run --project src/user-ms/CoreMs.UserMs.Api -- --seed

# Run
dotnet run --project src/user-ms/CoreMs.UserMs.Api
```

Open http://localhost:5100/swagger

**Test credentials:** All seed users have password `Password123!`
- `admin@corems.local` — all admin roles
- `alice.johnson@corems.local` — regular user

## Architecture

```
corems-parent/
├── src/
│   ├── common/                       # Shared libraries
│   │   ├── CoreMs.Common/            # Exceptions, Query, Data, Middleware
│   │   ├── CoreMs.Common.Contracts/  # Messaging DTOs
│   │   └── CoreMs.Common.Security/   # JWT, RBAC
│   ├── aspire/                       # .NET Aspire orchestration
│   ├── user-ms/                      # User management service
│   ├── communication-ms/             # (planned)
│   ├── document-ms/                  # (planned)
│   ├── translation-ms/               # (planned)
│   └── template-ms/                  # (planned)
├── tests/                            # All test projects
├── docker/                           # Infrastructure
└── CoreMs.slnx
```

Each service follows Clean Architecture:
```
<service>-ms/
├── CoreMs.<Service>Ms.Api/              # Host, controllers, DI
├── CoreMs.<Service>Ms.Domain/           # Entities, services, interfaces
└── CoreMs.<Service>Ms.Infrastructure/   # EF Core, repositories, migrations
```

## Tech Stack

- .NET 10 / ASP.NET Core 10
- Entity Framework Core 10 + PostgreSQL
- .NET Aspire for local orchestration
- FluentValidation
- BCrypt.Net for password hashing
- MassTransit + RabbitMQ for messaging
- xUnit + FluentAssertions + Testcontainers

## Adding a New Service

1. Create `src/<service>-ms/` with Api, Domain, Infrastructure projects
2. Extend `CoreMsDbContext` with your schema name
3. Extend `SearchableRepository<T>` for your entities
4. Define errors in a static class
5. Add to `CoreMs.slnx` and run `dotnet build`

## CLI

```bash
dotnet run --project src/user-ms/CoreMs.UserMs.Api              # Run
dotnet run --project src/user-ms/CoreMs.UserMs.Api -- --migrate # Migrate DB
dotnet run --project src/user-ms/CoreMs.UserMs.Api -- --seed    # Seed data
dotnet test                                                      # All tests
dotnet build                                                     # Build
```

## Related

Also available as a [Java/Spring Boot edition](https://github.com/CoreWebMicroservices/corems-project) with the same API contracts.
