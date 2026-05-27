---
inclusion: fileMatch
fileMatchPattern: "**/{Program.cs,appsettings.json,*.csproj}"
---

# Microservice Generation Phases (.NET)

## Phase 1 - Solution Structure & Skeleton (STOP for review)
1. Create service projects:
   - `CoreMs.<Service>Ms.Api` (ASP.NET Core Web API)
   - `CoreMs.<Service>Ms.Domain` (Class Library)
   - `CoreMs.<Service>Ms.Infrastructure` (Class Library)
2. Add project references (Api → Domain + Infrastructure, Infrastructure → Domain)
3. Create `Program.cs` with minimal configuration
4. Add to `CoreMs.slnx`
5. Run: `dotnet build`
6. **STOP for human review**

## Phase 2 - Entities & Repositories (STOP for review)
1. Implement entities in Domain layer (plain C# classes, no attributes)
2. Create `IEntityTypeConfiguration<T>` for each entity in Infrastructure/Data/
3. Create DbContext extending `CoreMsDbContext` (one-liner)
4. Define repository interfaces in Domain layer (extending `ISearchableRepository<T>`)
5. Implement repositories extending `SearchableRepository<T>` in Infrastructure
6. Add initial EF Core migration
7. Run: `dotnet build && dotnet ef migrations add InitialCreate`
8. **STOP for human review**

## Phase 3 - Controllers, Services, Tests
1. Implement controllers with proper routing and authorization
2. Business logic in service layer (Domain)
3. Use `[Authorize(Roles = CoreMsRoles.ServiceAdmin)]` for role-gated operations
4. Implement paginated listing with shared `PagedResult<T>` and `QueryParameters`
5. Add tests in top-level `tests/` folder
6. Run full build + tests

## Project File Guidance

### Api .csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="..\CoreMs.<Service>Ms.Domain\CoreMs.<Service>Ms.Domain.csproj" />
    <ProjectReference Include="..\CoreMs.<Service>Ms.Infrastructure\CoreMs.<Service>Ms.Infrastructure.csproj" />
    <ProjectReference Include="..\..\common\CoreMs.Common.Security\CoreMs.Common.Security.csproj" />
  </ItemGroup>
</Project>
```

Note: `TargetFramework`, `Nullable`, `ImplicitUsings` are inherited from `Directory.Build.props` (net10.0).

### Domain .csproj (NO infrastructure dependencies)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\common\CoreMs.Common\CoreMs.Common.csproj" />
  </ItemGroup>
</Project>
```

### Infrastructure .csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\CoreMs.<Service>Ms.Domain\CoreMs.<Service>Ms.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
  </ItemGroup>
</Project>
```

### Test .csproj (in top-level `tests/` folder)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\src\<service>-ms\CoreMs.<Service>Ms.Domain\CoreMs.<Service>Ms.Domain.csproj" />
    <ProjectReference Include="..\..\src\<service>-ms\CoreMs.<Service>Ms.Infrastructure\CoreMs.<Service>Ms.Infrastructure.csproj" />
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

public class <Service>MsDbContext(DbContextOptions<<Service>MsDbContext> options) : CoreMsDbContext(options)
{
    protected override string SchemaName => "<service>_ms";
}
```

No `DbSet<T>` properties — repositories use `Context.Set<T>()`.

## Program.cs Template

```csharp
using CoreMs.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure
builder.Services.AddDbContext<<Service>MsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories & Services
builder.Services.AddScoped<I<Entity>Repository, <Entity>Repository>();
builder.Services.AddScoped<I<Entity>Service, <Entity>Service>();

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseMiddleware<AutoSaveChangesMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
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
