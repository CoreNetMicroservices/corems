resource "azurerm_servicebus_namespace" "main" {
  name                = "${var.resource_group_name}-servicebus"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  tags                = var.tags
}
