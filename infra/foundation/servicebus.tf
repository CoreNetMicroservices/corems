resource "azurerm_servicebus_namespace" "main" {
  name                = "${var.resource_group_name}-servicebus"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  # Standard SKU is required: MassTransit uses topics/subscriptions for pub/sub, which the
  # Basic SKU does not support (queues only).
  sku                 = "Standard"
  tags                = var.tags
}
