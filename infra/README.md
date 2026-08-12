# CoreMS Azure Infrastructure

Terraform configuration for deploying the CoreMS platform to Azure. Organized into three layers with separate state files for independent lifecycle management.

## Architecture

```
infra/
├── bootstrap/       # One-time: remote state storage (local state)
├── foundation/      # Shared resources: database, registry, storage, messaging
└── services/        # Application workloads: Container Apps, Static Web App, DNS
```

### Layer Dependency Flow

```
bootstrap → foundation → services
```

- **bootstrap** creates the Azure Storage Account that holds Terraform state for the other layers.
- **foundation** provisions shared infrastructure (Postgres, ACR, Service Bus, Blob Storage, Key Vault, DNS zone).
- **services** reads foundation outputs via remote state and deploys Container Apps, Static Web App, and DNS records.

## Resources

### Foundation Layer

| Resource | Type | SKU/Tier | Purpose |
|----------|------|----------|---------|
| Resource Group | `azurerm_resource_group` | — | Container for all resources |
| PostgreSQL Flexible Server | `azurerm_postgresql_flexible_server` | B_Standard_B1ms | Shared database (one DB, schema-per-service) |
| Container Registry | `azurerm_container_registry` | Basic | Docker image storage |
| Service Bus Namespace | `azurerm_servicebus_namespace` | Basic | Async messaging between services |
| Storage Account | `azurerm_storage_account` | Standard LRS | Blob storage for documents |
| Key Vault | `azurerm_key_vault` | Standard | Secrets management |
| DNS Zone | `azurerm_dns_zone` | — | Domain management |

### Services Layer

| Resource | Type | Purpose |
|----------|------|---------|
| Container App Environment | `azurerm_container_app_environment` | Shared hosting environment with Log Analytics |
| Container Apps (×5) | `azurerm_container_app` | user-ms, communication-ms, document-ms, translation-ms, template-ms |
| Static Web App | `azurerm_static_web_app` | React frontend (Free tier) |
| DNS CNAME Records | `azurerm_dns_cname_record` | Custom domain routing |

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed and authenticated (`az login`)
- [Terraform](https://developer.hashicorp.com/terraform/downloads) >= 1.5.0
- An active Azure subscription with resource creation permissions
- A domain name for DNS configuration
- Required Azure resource providers registered:

```bash
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights
az provider register --namespace Microsoft.DBforPostgreSQL
```

## Deployment

### Step 1: Bootstrap (one-time)

Creates the storage account for remote Terraform state.

```bash
cd infra/bootstrap
terraform init
terraform apply
```

Creates: resource group `corems-tfstate-rg`, storage account `coremstfstate`, container `tfstate`.

### Step 2: Foundation

Provisions all shared infrastructure.

```bash
cd infra/foundation
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values
terraform init
terraform apply
```

### Step 3: Services

Deploys application workloads. Reads foundation outputs via remote state.

```bash
cd infra/services
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values
terraform init
terraform apply
```

## Variables

### Foundation

| Variable | Description | Default | Sensitive |
|----------|-------------|---------|-----------|
| `resource_group_name` | Azure resource group name | — | No |
| `location` | Azure region | `westeurope` | No |
| `postgres_admin_username` | PostgreSQL admin username | — | Yes |
| `postgres_admin_password` | PostgreSQL admin password | — | Yes |
| `dns_zone_domain` | Domain for DNS zone | — | No |
| `tags` | Tags applied to all resources | `{environment="production", project="corems"}` | No |

### Services

| Variable | Description | Default | Sensitive |
|----------|-------------|---------|-----------|
| `max_replicas` | Max replicas per Container App | `5` | No |
| `image_tags` | Image tag per service | all `latest` | No |
| `custom_domains` | Custom domain per service/frontend | all empty | No |

## Secrets Handling

Never commit `terraform.tfvars` with real secrets. Supply sensitive values via environment variables:

```bash
export TF_VAR_postgres_admin_username="pgadmin"
export TF_VAR_postgres_admin_password="your-strong-password"
terraform apply
```

Or use `-var` flags:

```bash
terraform apply -var="postgres_admin_password=your-strong-password"
```

## Database Strategy

The platform uses a single PostgreSQL database (`corems`) with schema-per-service isolation:

| Schema | Service |
|--------|---------|
| `user_ms` | User management |
| `communication_ms` | Email, SMS, notifications |
| `document_ms` | File storage and documents |
| `translation_ms` | i18n bundle management |
| `template_ms` | Template rendering |

Schemas are created by EF Core migrations at application startup — Terraform only provisions the database server and the `corems` database.

## Container Apps Configuration

Each service runs as a Container App with:
- **CPU**: 0.25 vCPU
- **Memory**: 0.5 Gi
- **Scale**: 0 to `max_replicas` (scale-to-zero enabled)
- **Ingress**: External HTTP, single revision mode
- **Service discovery**: Internal URLs injected as environment variables

Environment variables injected per container:
- `ConnectionStrings__DefaultConnection` — PostgreSQL with service schema
- `ConnectionStrings__ServiceBus` — Azure Service Bus connection string
- `Storage__AccountName` / `Storage__AccessKey` — Blob storage credentials
- `KeyVault__Uri` — Key Vault endpoint
- `Services__<ServiceName>__BaseUrl` — Internal URLs of other services

## Outputs

### Foundation
- `resource_group_name`, `acr_login_server`, `postgres_fqdn`
- `servicebus_connection_string`, `storage_account_name`, `keyvault_uri`
- `dns_zone_name_servers`, `dns_zone_domain`

### Services
- `container_app_fqdns` — Map of service name → public FQDN
- `static_web_app_hostname` — Frontend hostname

## Useful Commands

```bash
# View current state
terraform state list

# Preview changes without applying
terraform plan

# Destroy all resources (caution!)
terraform destroy

# Import existing resource into state
terraform import azurerm_resource_group.main /subscriptions/.../resourceGroups/corems-prod-rg

# View outputs
terraform output
terraform output -json
```
