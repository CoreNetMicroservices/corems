---
inclusion: fileMatch
fileMatchPattern: "**/*.cs"
---

# Backend Architecture - Core Microservices (.NET)

## Platform
- .NET 10 / ASP.NET Core 10
- C# 13
- PostgreSQL database
- JWT Authentication (custom implementation)
- RabbitMQ via MassTransit
- Entity Framework Core 10
- .NET Aspire for orchestration
- FluentValidation for request validation

## Solution Structure

```
corems-parent/
├── backend/                       # .NET Backend (solution root)
│   ├── aspire/                    # .NET Aspire orchestration
│   │   ├── CoreMs.AppHost/
│   │   └── CoreMs.ServiceDefaults/
│   ├── common/                    # Shared libraries
│   │   ├── src/
│   │   │   ├── CoreMs.Common/     # Exceptions/, Repository/, Data/, Middleware/, Extensions/
│   │   │   ├── CoreMs.Common.Contracts/
│   │   │   └── CoreMs.Common.Security/
│   │   └── test/
│   │       └── CoreMs.Common.Tests/
│   ├── user-ms/                   # User management service
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
└── docker/                        # Infrastructure
```

## CoreMs.Common Package

Single shared library containing all common infrastructure:

```
CoreMs.Common/
├── Data/          # CoreMsDbContext (abstract base)
├── Exceptions/    # ErrorInfo, Error, ErrorResponse, ServiceException, DefaultExceptionCodes
├── Extensions/    # [Service], [Repository] attributes, ServiceCollectionExtensions (AddCoreMsServices)
├── Middleware/    # GlobalExceptionHandler, AutoSaveChangesMiddleware
└── Repository/    # CrudRepository, SearchableRepository, QueryParameters, PagedResult, FilterParser
```

There is no `CoreMs.Common.Api` project — contracts live in `CoreMs.Common.Contracts`.

## Shared Infrastructure Approach

- Reference `CoreMs.Common` and `CoreMs.Common.Security` as project references
- Use `AddCoreMsServices()` for convention-based DI registration
- DO NOT duplicate middleware or DI registration across services
- Common library wires exception handling, auto-save, and base repository classes

### Dependencies (Directory.Packages.props)
- Central package version management
- All services reference the same package versions
- No package version overrides in individual .csproj files

## Service Folder Layout

```
user-ms/
├── src/
│   ├── CoreMs.UserMs.Api/             # Host + Controllers
│   │   ├── Configuration/
│   │   ├── Controllers/
│   │   ├── Services/                  # Background services (TokenCleanupService)
│   │   ├── Validators/                # FluentValidation validators
│   │   ├── Program.cs
│   │   ├── Properties/launchSettings.json
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   ├── CoreMs.UserMs.Core/            # Business logic + repositories
│   │   ├── Configuration/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Exceptions/
│   │   ├── Models/
│   │   ├── Repositories/
│   │   └── Services/
│   └── CoreMs.UserMs.Infrastructure/  # EF Core configuration only
│       ├── Data/
│       └── Migrations/
└── test/
    ├── CoreMs.UserMs.Tests/
    └── CoreMs.UserMs.IntegrationTests/
```

## Auto-Registration

Services and repositories use attribute-based auto-registration:

```csharp
// Program.cs — one line registers all [Service] and [Repository] classes from the assembly
builder.Services.AddCoreMsServices(typeof(UserService).Assembly);

// Service class
[Service]
public class AuthService(UserRepository userRepository) { }

// Repository class
[Repository]
public class UserRepository(DbContext context) : SearchableRepository<UserEntity>(context) { }
```

No interfaces needed. If a class implements `IClassName`, it registers as the interface. Otherwise it registers as itself.

## Code Style

### Namespace Convention
- `CoreMs.<Service>Ms.<Layer>` (e.g., `CoreMs.UserMs.Core.Services`)

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
- Use XML docs (`///`) for public APIs (required for controller actions)
- Use `//` for short rationale only
- Tag actionable items: `TODO:` / `FIXME:` with owner/ticket

## PR Checklist
- ✅ Avoided editing `Common` projects without discussion
- ✅ Core layer only depends on CoreMs.Common, CoreMs.Common.Contracts, and EF Core
- ✅ Infrastructure layer only has DbContext, Configurations, and Migrations
- ✅ No package version overrides in individual .csproj files
- ✅ Migration changes sync with entity changes
- ✅ Nullable reference types respected (no warnings)

## Aspire Orchestration

### AppHost Configuration
The AppHost (`aspire/CoreMs.AppHost/Program.cs`) is the single orchestrator:

```csharp
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var secrets = builder.Configuration.GetSection("Secrets");

var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 5432)
    .WithDataVolume("corems-postgres-data")
    .WithPgAdmin()
    .AddDatabase("corems");

var userMs = builder.AddProject<Projects.CoreMs_UserMs_Api>("user-ms")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("Jwt__SecretKey", secrets["JwtSecretKey"] ?? "");
```

### Secrets File (gitignored)
`aspire/CoreMs.AppHost/appsettings.Development.json`:
```json
{
  "Parameters": { "postgres-password": "postgres" },
  "Secrets": {
    "JwtSecretKey": "your-secret-key-min-32-chars",
    "RabbitMqPassword": "guest",
    "GoogleClientId": "", "GoogleClientSecret": "",
    "GitHubClientId": "", "GitHubClientSecret": "",
    "LinkedInClientId": "", "LinkedInClientSecret": ""
  }
}
```

### Adding a New Service to Aspire
```csharp
var commMs = builder.AddProject<Projects.CoreMs_CommunicationMs_Api>("communication-ms")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("RabbitMq__Password", secrets["RabbitMqPassword"] ?? "guest");
```

## CORS

Each service configures CORS in Program.cs:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:8080"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// In middleware pipeline (before auth):
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
```

Add `Cors:AllowedOrigins` to service appsettings.json.

## Token Architecture

### Access Token (JWT, short-lived)
- Contains: `sub`, `email`, `role`, `scope`
- Expiration: 10 minutes
- Used for API authorization

### Refresh Token (JWT, long-lived)
- Contains: `sub`, `email`, `first_name`, `last_name`, `roles`
- Expiration: 24 hours
- Stored in DB (`login_token` table) for revocation
- Single-use with rotation (old token deleted, new one issued)
- Frontend must store the new refresh token after each refresh

### Token Claims Convention
- Use short claim names: `"role"`, `"sub"`, `"email"` (not ClaimTypes.*)
- Multiple roles: multiple `"role"` claims in access token
- Refresh token uses `"roles"` (plural) for frontend compatibility

## Auto-Migrate and Seed (Development)

Services auto-migrate and seed in Development mode:
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
    await db.Database.MigrateAsync();
    
    var seeder = new SeedDataService(db, logger);
    await seeder.SeedAsync();  // Idempotent: checks if data exists
}
```
