---
inclusion: fileMatch
fileMatchPattern: "**/{Program.cs,appsettings.json,*.csproj}"
---

# Microservice Generation Phases (.NET)

## Phase 1 - Solution Structure & Skeleton (STOP for review)
1. Create service projects:
   - `CoreMs.<Service>Ms.Api` (ASP.NET Core Web API)
   - `CoreMs.<Service>Ms.Core` (Class Library)
   - `CoreMs.<Service>Ms.Infrastructure` (Class Library)
2. Add project references (Api → Core + Infrastructure, Infrastructure → Core)
3. Create `Program.cs` with minimal configuration
4. Add to `CoreMs.slnx`
5. Run: `dotnet build`
6. **STOP for human review**

## Phase 2 - Entities & Repositories (STOP for review)
1. Implement entities in Core layer (plain C# classes, no attributes)
2. Create `IEntityTypeConfiguration<T>` for each entity in Infrastructure/Data/Configurations/
3. Create DbContext extending `CoreMsDbContext` (one-liner) in Infrastructure/Data/
4. Implement repositories extending `SearchableRepository<T>` in **Core/Repositories/** with `[Repository]` attribute
5. Add initial EF Core migration
6. Run: `dotnet build && dotnet ef migrations add InitialCreate`
7. **STOP for human review**

## Phase 3 - Controllers, Services, Tests
1. Implement controllers with proper routing and authorization in Api/Controllers/
2. Business logic in service layer (Core/Services/) with `[Service]` attribute
3. Add FluentValidation validators in Api/Validators/
4. Use `[Authorize(Roles = CoreMsRoles.ServiceAdmin)]` for role-gated operations
5. Implement paginated listing with shared `PagedResult<T>` and `QueryParameters`
6. Add tests in top-level `tests/` folder
7. Run full build + tests

## Project File Guidance

### Api .csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <RootNamespace>CoreMs.<Service>Ms.Api</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\CoreMs.<Service>Ms.Core\CoreMs.<Service>Ms.Core.csproj" />
    <ProjectReference Include="..\CoreMs.<Service>Ms.Infrastructure\CoreMs.<Service>Ms.Infrastructure.csproj" />
    <ProjectReference Include="$(SolutionRoot)common\src\CoreMs.Common.Contracts\CoreMs.Common.Contracts.csproj" />
    <ProjectReference Include="$(SolutionRoot)aspire\CoreMs.ServiceDefaults\CoreMs.ServiceDefaults.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
  </ItemGroup>
</Project>
```

Note: `TargetFramework`, `Nullable`, `ImplicitUsings` are inherited from `Directory.Build.props` (net10.0).

### Core .csproj (business logic + repositories)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>CoreMs.<Service>Ms.Core</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$(SolutionRoot)common\src\CoreMs.Common\CoreMs.Common.csproj" />
    <ProjectReference Include="$(SolutionRoot)common\src\CoreMs.Common.Contracts\CoreMs.Common.Contracts.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>
</Project>
```

### Infrastructure .csproj (EF Core config + migrations only)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>CoreMs.<Service>Ms.Infrastructure</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\CoreMs.<Service>Ms.Core\CoreMs.<Service>Ms.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>
</Project>
```

### Test .csproj (in `<service>-ms/test/` folder)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="$(SolutionRoot)<service>-ms\src\CoreMs.<Service>Ms.Core\CoreMs.<Service>Ms.Core.csproj" />
    <ProjectReference Include="$(SolutionRoot)<service>-ms\src\CoreMs.<Service>Ms.Infrastructure\CoreMs.<Service>Ms.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="NSubstitute" />
  </ItemGroup>
</Project>
```

## DbContext (One-Liner)

```csharp
using CoreMs.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMs.<Service>Ms.Infrastructure.Data;

public class <Service>MsDbContext(DbContextOptions<<Service>MsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "<service>_ms";
}
```

No `DbSet<T>` properties — repositories use `Context.Set<T>()`.

## Program.cs Template

```csharp
using CoreMs.Common.App;
using CoreMs.ServiceDefaults;
using CoreMs.<Service>Ms.Core.Services;
using CoreMs.<Service>Ms.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreMsHost();
builder.AddCoreMsApp(o => o.WithSwagger("<Service> Service", "Description here"));
builder.AddCoreMsDatabase<<Service>MsDbContext>();
builder.AddCoreMsModules(typeof(<MainService>).Assembly, typeof(Program).Assembly);

var app = builder.Build();

if (await app.RunCoreMsDatabaseAsync<<Service>MsDbContext>(
    seed: async (db, sp) => await new SeedDataService(db,
        sp.GetRequiredService<ILoggerFactory>().CreateLogger<SeedDataService>()).SeedAsync())) return;

app.UseCoreMsApp();
app.MapCoreMsEndpoints();

app.Run();
```

### Program.cs API Reference

| Method | Purpose |
|--------|---------|
| `builder.AddCoreMsHost()` | Aspire: OpenTelemetry, health checks, service discovery |
| `builder.AddCoreMsApp(o => ...)` | CORS, controllers, Swagger, JWT auth, exception handling |
| `builder.AddCoreMsDatabase<T>()` | Aspire Npgsql DbContext + DI aliases |
| `builder.AddCoreMsModules(core, api)` | Auto-register [Service]/[Repository] + FluentValidation |
| `builder.AddCoreMsOptions<T>()` | Bind options with DataAnnotation validation |
| `builder.AddCoreMsOptionsLite<T>()` | Bind options without DataAnnotation validation |
| `builder.AddCoreMsClient<T>(name)` | Service-to-service typed HttpClient with JWT forwarding |
| `app.RunCoreMsDatabaseAsync<T>()` | Dev auto-migrate + seed, CLI --migrate/--seed support |
| `app.UseCoreMsApp()` | Middleware pipeline (Swagger, exceptions, CORS, auth) |
| `app.MapCoreMsEndpoints()` | Health check endpoints (/health, /alive) |

### CoreMsAppOptions fluent API

```csharp
builder.AddCoreMsApp(o => o
    .WithSwagger("Title", "Description")  // Swagger doc metadata
    .WithEnumsAsStrings()                  // JsonStringEnumConverter
    .WithoutJwtAuth()                      // Skip JWT consumer auth (token issuers only)
    .WithCorsOrigins("http://custom:3000") // Override CORS origins
    .WithoutSwagger());                    // Disable Swagger entirely
```

## Port Allocation

| Port | Service |
|------|---------|
| 5100 | user-ms |
| 5101 | communication-ms |
| 5102 | document-ms |
| 5103 | translation-ms |
| 5104 | template-ms |

Configure in `Properties/launchSettings.json`:
```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:51XX",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Configuration Templates

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=corems;Username=postgres;Password=postgres;Search Path=<service>_ms"
  }
}
```
