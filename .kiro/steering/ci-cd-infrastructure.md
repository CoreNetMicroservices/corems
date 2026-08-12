# CI/CD & Infrastructure Guide

## Workflows Overview

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| CI | `ci.yml` | Push to main, PRs, manual | Build, test, lint, publish packages |
| Deploy | `deploy.yml` | Manual only (`workflow_dispatch`) | Build Docker images, deploy to Azure |

## CI Pipeline (`ci.yml`)

### Jobs

1. **build-backend** — restore, build, test (.NET 10)
2. **build-frontend** — install, lint, build (Node 22)
3. **terraform-validate** — `terraform init -backend=false && terraform validate` for all layers
4. **publish-packages** — pack and push changed NuGet packages to GitHub Packages (main only, after backend passes)

### NuGet Package Publishing

- **Auto-discovery**: finds all `.csproj` files with `<IsPackable>true</IsPackable>` — no hardcoded list
- **Change detection**: compares `HEAD~1` vs `HEAD` to identify which packable project directories changed
- **Force publish**: run workflow manually with `force-publish=true` to publish all packages regardless of changes
- **Version format**: `1.0.<github.run_number>` (e.g., `1.0.42`)
- **Feed**: `https://nuget.pkg.github.com/CoreNetMicroservices/index.json`
- **Duplicate handling**: `--skip-duplicate` makes re-runs safe

### Adding a New Shared Library / Client Package

1. Set `<IsPackable>true</IsPackable>` in the `.csproj`
2. Add `PackageId`, `Description`, `Authors`, `RepositoryUrl`, `Version` properties
3. Add a `PackageVersion` entry in `backend/Directory.Packages.props`
4. That's it — CI auto-discovers and publishes it

## Deploy Pipeline (`deploy.yml`)

### Flow

1. **build-and-push** — builds Docker images for all services, pushes to ACR
2. **deploy-infrastructure** — runs `terraform apply` for Container Apps
3. **deploy-frontend** — builds and deploys to Azure Static Web Apps

### Docker Build Convention

- Build context: `backend/`
- Dockerfile path: `backend/<service>-ms/Dockerfile`
- Build arg: `--build-arg GITHUB_TOKEN=${{ secrets.GITHUB_TOKEN }}`
- Image tag: `corems-<service-name>:<git-sha>`

### Adding a New Service to Deploy

1. Add entry to the `matrix.service` list in `deploy.yml`
2. Add the service name to the `image_tags` Terraform variable in the apply step

## Dockerfile Pattern

All service Dockerfiles follow this structure:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE <port>

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG GITHUB_TOKEN
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./

RUN dotnet nuget add source "https://nuget.pkg.github.com/CoreNetMicroservices/index.json" \
    --name github --username "docker" --password "$GITHUB_TOKEN" \
    --store-password-in-clear-text

COPY <service>-ms/src/<Project>/<Project>.csproj <service>-ms/src/<Project>/
# ... copy all project files for restore

RUN dotnet restore <service>-ms/src/<Service>.Api/<Service>.Api.csproj \
    /p:UsePackageReferences=true

COPY <service>-ms/src/ <service>-ms/src/
RUN dotnet publish <service>-ms/src/<Service>.Api/<Service>.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UsePackageReferences=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:<port>
ENTRYPOINT ["dotnet", "<Service>.Api.dll"]
```

Key rules:
- Never COPY shared directories (common/, aspire/, other service clients)
- Always pass `/p:UsePackageReferences=true` to both restore and publish
- GITHUB_TOKEN only exists in the build stage — never in the final image
- Build context is `backend/` — all paths relative to that

## Infrastructure (Terraform)

### Layers

| Layer | Path | Purpose |
|-------|------|---------|
| bootstrap | `infra/bootstrap/` | Azure resource group, storage account for TF state |
| foundation | `infra/foundation/` | ACR, PostgreSQL, DNS, KeyVault, ServiceBus, Storage |
| services | `infra/services/` | Container Apps environment, Container Apps, Static Web App |

### Conventions

- All layers use Azure backend for state storage
- Variables in `variables.tf`, values in `terraform.tfvars` (gitignored)
- Example values in `terraform.tfvars.example` (committed)
- Never commit `.terraform/` directories or state files

## Secrets Reference

| Secret | Used by | Purpose |
|--------|---------|---------|
| `GITHUB_TOKEN` | CI, Deploy | NuGet package publish/restore |
| `AZURE_CREDENTIALS` | Deploy | Azure login (service principal JSON) |
| `ACR_LOGIN_SERVER` | Deploy | Azure Container Registry hostname |
| `SWA_DEPLOYMENT_TOKEN` | Deploy | Static Web App deployment token |
| `SONAR_TOKEN` | CI | SonarCloud analysis (optional) |

## Build Modes

| Context | UsePackageReferences | Shared libs resolved via |
|---------|---------------------|------------------------|
| Local dev (`dotnet build`) | not set (false) | ProjectReference (source) |
| CI (`dotnet build`) | not set (false) | ProjectReference (source) |
| Docker build | true | PackageReference (GitHub Packages) |
