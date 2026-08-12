resource "azurerm_dns_zone" "main" {
  name                = var.dns_zone_domain
  resource_group_name = azurerm_resource_group.main.name
  tags                = var.tags
}
