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

### GitHub Secrets (pipeline credentials only)

| Secret | Used by | Purpose |
|--------|---------|---------|
| `GITHUB_TOKEN` | CI, Deploy | NuGet package publish/restore |
| `AZURE_CREDENTIALS` | Deploy | Azure login (service principal JSON) |
| `AZURE_CLIENT_ID` | Deploy | Service principal for Terraform + az CLI |
| `AZURE_CLIENT_SECRET` | Deploy | Service principal secret |
| `AZURE_TENANT_ID` | Deploy | Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | Deploy | Azure subscription |
| `ACR_LOGIN_SERVER` | Deploy | Azure Container Registry hostname |
| `SWA_DEPLOYMENT_TOKEN` | Deploy | Static Web App deployment token |
| `TERRAFORM_STATE_ACCESS_KEY` | Deploy | Storage account key for TF state |
| `SONAR_TOKEN` | CI | SonarCloud analysis (optional) |

## Custom Domains (Optional)

Custom domains are **not** part of the deploy pipeline. Most deployments run on the default
Azure hostnames. To attach a custom domain (frontend apex + `<service>-api.<domain>`
subdomains with managed TLS certs), run the one-time, idempotent script:

```bash
BASE_DOMAIN=example.com RESOURCE_GROUP=corems-prod-rg SUBSCRIPTION_ID=<sub-id> \
  infra/scripts/setup-custom-domain.sh
```

See `infra/scripts/README.md` for details. This lives outside Terraform because SWA apex
`dns-txt-token` validation and Container Apps managed-cert IDs can't be expressed cleanly in
the `azurerm` provider (provider bug #27362).

### Azure Key Vault (application secrets)

All application secrets live in Key Vault (`corems-prod-rg-kv`). Services read them at startup
via managed identity — no secrets flow through the pipeline.

Naming convention: `Section--Key` (double dash maps to `:` in .NET config).

| Key Vault Secret | Maps to Config | Used by |
|-----------------|----------------|---------|
| `Jwt--SecretKey` | `Jwt:SecretKey` | All services (JWT validation) |
| `Jwt--PrivateKeyBase64` | `Jwt:PrivateKeyBase64` | user-ms (RS256 signing) |
| `Jwt--PublicKeyBase64` | `Jwt:PublicKeyBase64` | user-ms (RS256 verification) |
| `SocialAuth--Google--ClientId` | `SocialAuth:Google:ClientId` | user-ms |
| `SocialAuth--Google--ClientSecret` | `SocialAuth:Google:ClientSecret` | user-ms |
| `SocialAuth--GitHub--ClientId` | `SocialAuth:GitHub:ClientId` | user-ms |
| `SocialAuth--GitHub--ClientSecret` | `SocialAuth:GitHub:ClientSecret` | user-ms |
| `SocialAuth--LinkedIn--ClientId` | `SocialAuth:LinkedIn:ClientId` | user-ms |
| `SocialAuth--LinkedIn--ClientSecret` | `SocialAuth:LinkedIn:ClientSecret` | user-ms |
| `Mail--Host` | `Mail:Host` | communication-ms |
| `Mail--Port` | `Mail:Port` | communication-ms |
| `Mail--Username` | `Mail:Username` | communication-ms |
| `Mail--Password` | `Mail:Password` | communication-ms |
| `Mail--DefaultFrom` | `Mail:DefaultFrom` | communication-ms |
| `DocumentLinkSigningKey` | `DocumentLinkSigningKey` | document-ms |

To add/update secrets:
```bash
az keyvault secret set --vault-name corems-prod-rg-kv --name "Jwt--SecretKey" --value "your-value"
```

### Configuration Split

| What | Source | Mechanism |
|------|--------|-----------|
| DB connection string | Terraform (foundation outputs) | Container App env var |
| Service-to-service URLs | Terraform (Container App FQDNs) | Container App env var |
| Storage account, Key Vault URI | Terraform (foundation outputs) | Container App env var |
| JWT secrets, OAuth creds, SMTP | Azure Key Vault | App reads at startup via managed identity |

## Build Modes

| Context | UsePackageReferences | Shared libs resolved via |
|---------|---------------------|------------------------|
| Local dev (`dotnet build`) | not set (false) | ProjectReference (source) |
| CI (`dotnet build`) | not set (false) | ProjectReference (source) |
| Docker build | true | PackageReference (GitHub Packages) |
