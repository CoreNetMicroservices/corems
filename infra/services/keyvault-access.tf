# Look up the Key Vault directly (avoids dependency on foundation re-apply for the new output)
data "azurerm_key_vault" "main" {
  name                = "${local.foundation.resource_group_name}-kv"
  resource_group_name = local.foundation.resource_group_name
}

# Grant each Container App's managed identity read access to Key Vault secrets.
resource "azurerm_role_assignment" "container_app_keyvault" {
  for_each = local.services

  scope                = data.azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_container_app.services[each.key].identity[0].principal_id
}
