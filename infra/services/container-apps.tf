locals {
  services = {
    "user-ms" = {
      port       = 5100
      db_name    = "user_ms"
      image_name = "corems-user-ms"
    }
    "communication-ms" = {
      port       = 5101
      db_name    = "communication_ms"
      image_name = "corems-communication-ms"
    }
    "document-ms" = {
      port       = 5102
      db_name    = "document_ms"
      image_name = "corems-document-ms"
    }
    "translation-ms" = {
      port       = 5103
      db_name    = "translation_ms"
      image_name = "corems-translation-ms"
    }
    "template-ms" = {
      port       = 5104
      db_name    = "template_ms"
      image_name = "corems-template-ms"
    }
  }
}

resource "azurerm_container_app" "services" {
  for_each = local.services

  name                         = each.key
  resource_group_name          = local.foundation.resource_group_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  registry {
    server               = local.foundation.acr_login_server
    username             = local.foundation.acr_admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = local.foundation.acr_admin_password
  }

  template {
    min_replicas = 0
    max_replicas = var.max_replicas

    container {
      name   = each.key
      image  = "${local.foundation.acr_login_server}/${each.value.image_name}:${var.image_tags[each.key]}"
      cpu    = 0.25
      memory = "0.5Gi"

      # Database connection — the app reads ConnectionStrings:corems (see appsettings.json
      # and AddCoreMsDatabase / health checks), so the env var must be named accordingly.
      env {
        name  = "ConnectionStrings__corems"
        value = "Host=${local.foundation.postgres_fqdn};Port=5432;Database=corems;Username=${local.foundation.postgres_admin_username};Password=${local.foundation.postgres_admin_password};SSL Mode=Require;Trust Server Certificate=true;Search Path=${each.value.db_name}"
      }

      # Service Bus
      env {
        name        = "ConnectionStrings__ServiceBus"
        secret_name = "servicebus-connection"
      }

      # Blob Storage (Azure). document-ms selects AzureBlobStorageService when
      # Storage:ConnectionString is set (see StorageOptions.UseAzureBlob).
      env {
        name        = "Storage__ConnectionString"
        secret_name = "storage-connection"
      }
      env {
        name  = "Storage__Container"
        value = "documents"
      }

      # Key Vault
      env {
        name  = "KeyVault__Uri"
        value = local.foundation.keyvault_uri
      }

      # Service discovery — internal URLs of other services
      dynamic "env" {
        for_each = { for k, v in local.services : k => v if k != each.key }
        content {
          name  = "Services__${replace(title(replace(env.key, "-", " ")), " ", "")}__BaseUrl"
          value = "https://${env.key}.internal.${azurerm_container_app_environment.main.default_domain}"
        }
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = each.value.port
    transport        = "http"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  secret {
    name  = "servicebus-connection"
    value = local.foundation.servicebus_connection_string
  }

  secret {
    name  = "storage-connection"
    value = data.azurerm_storage_account.main.primary_connection_string
  }

  # Custom domains + managed certificates are bound out-of-band via `az containerapp
  # hostname bind` in the deploy pipeline. Ignore ingress custom_domain drift so Terraform
  # doesn't try to reconcile (and fail parsing) the CLI-managed certificate bindings.
  lifecycle {
    ignore_changes = [ingress[0].custom_domain]
  }
}
