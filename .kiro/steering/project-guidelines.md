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
├── src/
│   ├── aspire/                       # .NET Aspire
│   │   ├── CoreMs.AppHost/           # Orchestrator
│   │   └── CoreMs.ServiceDefaults/   # Shared service config
│   ├── common/                       # Shared libraries
│   │   ├── CoreMs.Common/            # Exceptions, Query, Data, Middleware
│   │   ├── CoreMs.Common.Contracts/  # Shared DTOs, contracts
│   │   └── CoreMs.Common.Security/   # JWT, auth middleware, RBAC
│   └── user-ms/                      # User management service
│       ├── CoreMs.UserMs.Api/
│       ├── CoreMs.UserMs.Domain/
│       └── CoreMs.UserMs.Infrastructure/
├── tests/                            # All test projects (top-level)
│   ├── CoreMs.Common.Tests/
│   ├── CoreMs.UserMs.Tests/
│   └── CoreMs.UserMs.IntegrationTests/
├── local-packages/                   # Local NuGet packages
├── CoreMs.slnx                       # Solution file
├── Directory.Build.props             # net10.0, nullable, implicit usings
├── Directory.Packages.props          # Central package management
└── nuget.config
```

## Development Workflow

```powershell
# Restore and build
dotnet restore
dotnet build

# Run user-ms
dotnet run --project src/user-ms/CoreMs.UserMs.Api

# Run all tests
dotnet test

# Add migration
dotnet ef migrations add <Name> `
    --project src/user-ms/CoreMs.UserMs.Infrastructure `
    --startup-project src/user-ms/CoreMs.UserMs.Api
```

## Service Structure (Clean Architecture)

```
<service>-ms/
├── CoreMs.<Service>Ms.Api/              # Host layer
│   ├── Controllers/
│   ├── Program.cs
│   └── Properties/launchSettings.json
├── CoreMs.<Service>Ms.Domain/           # Core business logic (no infra deps)
│   ├── Entities/
│   ├── Interfaces/
│   ├── Services/
│   └── Exceptions/
└── CoreMs.<Service>Ms.Infrastructure/   # Data access
    ├── Data/                            # DbContext + EntityTypeConfigurations
    ├── Repositories/
    └── Migrations/
```

## Service URLs & Ports

### Development Environment
- **User Service**: http://localhost:5100

### Infrastructure
- **PostgreSQL**: localhost:5432
- **RabbitMQ**: localhost:5672 (Management: 15672)

## Key Shared Libraries

| Package | Purpose |
|---------|---------|
| `CoreMs.Common` | Exceptions, Query (repositories, pagination, filtering), Data (base DbContext, base repositories), Middleware (exception handler, auto-save) |
| `CoreMs.Common.Contracts` | Shared DTOs and API contracts |
| `CoreMs.Common.Security` | JWT validation, auth middleware, RBAC |

## Code Standards
- **Clean Architecture**: Domain layer has zero infrastructure references
- **Dependency Injection**: Built-in .NET DI container
- **Minimal comments**: Code should be self-explanatory
- **Nullable reference types**: Enabled globally, no warnings allowed
- **Central package management**: All versions in `Directory.Packages.props`

## Database Guidelines
- **Schema per service**: `user_ms`, `communication_ms`, etc.
- **UUID for external IDs**: `Guid` type for public-facing identifiers
- **Long for internal IDs**: `long` for primary keys (BIGSERIAL)
- **Fluent API only**: No data annotations on entities
- **Auto-save middleware**: Repositories never call SaveChanges

## Testing Strategy

### Unit Tests
- xUnit + NSubstitute (or Moq)
- Test business logic in Domain layer
- Mock infrastructure dependencies

### Integration Tests
- `WebApplicationFactory<Program>` for real HTTP testing
- Testcontainers for PostgreSQL
- Test full request/response cycle

## Git & Commit Standards
- Conventional commit format: `feat:`, `fix:`, `refactor:`
- Never commit secrets or appsettings with real credentials
- Use `dotnet user-secrets` for local development
