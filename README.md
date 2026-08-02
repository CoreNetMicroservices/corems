# CoreMS (.NET)

Enterprise microservices toolkit for rapid application development.

Built with C# / ASP.NET Core 10, .NET Aspire orchestration, and a custom lightweight framework (`CoreMs.Common`).

## Services

| Service | Port | Description |
|---------|------|-------------|
| **User MS** | 5100 | OAuth2/OIDC, social auth (Google/GitHub/LinkedIn), user management, RBAC |
| **Communication MS** | 5101 | Email (SMTP/MailKit), SMS (Twilio), Slack notifications, RabbitMQ queue |
| **Document MS** | 5102 | File storage (S3/MinIO), visibility-based access, pre-signed links |
| **Translation MS** | 5103 | Internationalization, translation bundles per realm/language |
| **Template MS** | 5104 | Handlebars template management, rendering, and caching |
| **Frontend** | 8080 | React + TypeScript + Vite (all modules integrated) |

## Quick Start

```bash
# Prerequisites: .NET 10 SDK, Docker
cd backend
dotnet run --project aspire/CoreMs.AppHost
```

This starts everything: PostgreSQL, RabbitMQ, MinIO, all 5 backend services, and the frontend.

Aspire Dashboard: https://localhost:17178

### Seed Test Data

```bash
dotnet run --project user-ms/src/CoreMs.UserMs.Api -- --seed
```

Test credentials (password: `Password123!`):
- `admin@corems.local` — all admin roles
- `alice.johnson@corems.local` — regular user

## Framework API

Every service's `Program.cs` uses the CoreMS framework:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCoreMsHost();        // Aspire: OTel, health, service discovery
builder.AddCoreMsApp(o => o     // App: CORS, Swagger, JWT, Serilog, exceptions
    .WithSwagger("My Service", "Description"));
builder.AddCoreMsDatabase<MyDbContext>();   // Aspire Npgsql + DI aliases
builder.AddCoreMsModules(                  // [Service] + [Repository] + validators
    typeof(MyService).Assembly,
    typeof(Program).Assembly);

var app = builder.Build();

if (await app.RunCoreMsDatabaseAsync<MyDbContext>(seed: ...)) return;

app.UseCoreMsApp();             // Middleware pipeline
app.MapCoreMsEndpoints();       // Health endpoints

app.Run();
```

### Available Framework Methods

| Method | Purpose |
|--------|---------|
| `AddCoreMsHost()` | OpenTelemetry, health checks, service discovery |
| `AddCoreMsApp(o => ...)` | CORS, controllers, Swagger, JWT, Serilog, exceptions |
| `AddCoreMsDatabase<T>()` | Aspire Npgsql + CoreMsDbContext/DbContext aliases |
| `AddCoreMsModules(core, api)` | Auto-register [Service]/[Repository] + FluentValidation |
| `AddCoreMsOptions<T>()` | Bind + validate options (DataAnnotations) |
| `AddCoreMsClient<T>(name)` | Service-to-service HTTP client with JWT + correlation ID forwarding |
| `AddCoreMsMessaging(config)` | MassTransit/RabbitMQ with correlation ID propagation |
| `RunCoreMsDatabaseAsync<T>()` | Auto-migrate + seed in dev, --migrate/--seed CLI |
| `UseCoreMsApp()` | Full middleware pipeline (correlation ID, Swagger, auth, auto-save) |
| `MapCoreMsEndpoints()` | /health and /alive endpoints |

## Architecture

```
corems-parent/
├── backend/
│   ├── aspire/
│   │   ├── CoreMs.AppHost/           # Aspire orchestrator
│   │   └── CoreMs.ServiceDefaults/   # CoreMsHost (OTel, health, discovery)
│   ├── common/
│   │   └── src/
│   │       ├── CoreMs.Common/        # Framework library (App/, Security/, Repository/, etc.)
│   │       └── CoreMs.Common.Testing/# CoreMsTestFactory for integration tests
│   ├── user-ms/                      # src/ (Api, Core, Infrastructure) + test/
│   ├── communication-ms/             # src/ (Api, Client, Core, Infrastructure)
│   ├── document-ms/                  # src/ (Api, Core, Infrastructure) + test/
│   ├── translation-ms/              # src/ (Api, Core, Infrastructure) + test/
│   ├── template-ms/                 # src/ (Api, Core, Infrastructure) + test/
│   ├── CoreMs.slnx
│   ├── Directory.Build.props        # net10.0, nullable, implicit usings
│   └── Directory.Packages.props     # Central package management
└── frontend/                         # React + TypeScript + Vite
```

Each service follows three layers:
- **Api** — Controllers, validators, Program.cs (zero direct package refs)
- **Core** — Entities, services ([Service]), repositories ([Repository]), models
- **Infrastructure** — EF Core configurations + migrations only

## Tech Stack

- .NET 10 / C# 13 / ASP.NET Core 10
- Entity Framework Core 10 + PostgreSQL
- .NET Aspire 13.4 for orchestration
- Serilog (structured logging with correlation IDs)
- FluentValidation (auto-discovered)
- MassTransit + RabbitMQ (async messaging)
- MinIO (S3-compatible file storage)
- BCrypt.Net (password hashing)
- Handlebars.NET (template rendering)
- xUnit + FsCheck + FluentAssertions + NSubstitute (testing)
- React 18 + TypeScript + Vite + React Bootstrap + Hookstate (frontend)

## CLI

All commands from `backend/`:

```bash
dotnet run --project aspire/CoreMs.AppHost            # Run everything
dotnet run --project user-ms/src/CoreMs.UserMs.Api    # Run single service
dotnet run --project <service> -- --migrate           # Apply migrations
dotnet run --project <service> -- --seed              # Seed data
dotnet test                                           # All tests
dotnet build                                          # Build solution
```

## Related

Also available as a [Java/Spring Boot edition](https://github.com/CoreWebMicroservices/corems-project).
