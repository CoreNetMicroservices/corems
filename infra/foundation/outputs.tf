output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "acr_login_server" {
  value = azurerm_container_registry.main.login_server
}

output "acr_admin_username" {
  value     = azurerm_container_registry.main.admin_username
  sensitive = true
}

output "acr_admin_password" {
  value     = azurerm_container_registry.main.admin_password
  sensitive = true
}

output "postgres_fqdn" {
  value = azurerm_postgresql_flexible_server.main.fqdn
}

output "postgres_admin_username" {
  value     = azurerm_postgresql_flexible_server.main.administrator_login
  sensitive = true
}

output "postgres_admin_password" {
  value     = var.postgres_admin_password
  sensitive = true
}

output "servicebus_connection_string" {
  value     = azurerm_servicebus_namespace.main.default_primary_connection_string
  sensitive = true
}

output "storage_account_name" {
  value = azurerm_storage_account.main.name
}

output "storage_primary_access_key" {
  value     = azurerm_storage_account.main.primary_access_key
  sensitive = true
}

output "keyvault_uri" {
  value = azurerm_key_vault.main.vault_uri
}

output "dns_zone_name_servers" {
  value = azurerm_dns_zone.main.name_servers
}

output "dns_zone_domain" {
  value = azurerm_dns_zone.main.name
}
