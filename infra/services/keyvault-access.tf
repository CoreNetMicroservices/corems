# Grant each Container App's managed identity read access to Key Vault secrets
resource "azurerm_role_assignment" "container_app_keyvault" {
  for_each = local.services

  scope                = local.foundation.keyvault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_container_app.services[each.key].identity[0].principal_id
}
