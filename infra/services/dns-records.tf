data "azurerm_dns_zone" "main" {
  name                = local.foundation.dns_zone_domain
  resource_group_name = local.foundation.resource_group_name
}

# CNAME records for Container Apps
resource "azurerm_dns_cname_record" "services" {
  for_each = { for k, v in local.services : k => v if var.custom_domains[k] != "" }

  name                = split(".", var.custom_domains[each.key])[0]
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = local.foundation.resource_group_name
  ttl                 = 300
  record              = azurerm_container_app.services[each.key].ingress[0].fqdn
}

# CNAME record for frontend
resource "azurerm_dns_cname_record" "frontend" {
  count = var.custom_domains["frontend"] != "" ? 1 : 0

  name                = split(".", var.custom_domains["frontend"])[0]
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = local.foundation.resource_group_name
  ttl                 = 300
  record              = azurerm_static_web_app.frontend.default_host_name
}

# Custom domain bindings for Container Apps
resource "azurerm_container_app_custom_domain" "services" {
  for_each = { for k, v in local.services : k => v if var.custom_domains[k] != "" }

  name             = var.custom_domains[each.key]
  container_app_id = azurerm_container_app.services[each.key].id

  lifecycle {
    ignore_changes = [certificate_binding_type, container_app_environment_certificate_id]
  }
}

# Custom domain for Static Web App
resource "azurerm_static_web_app_custom_domain" "frontend" {
  count = var.custom_domains["frontend"] != "" ? 1 : 0

  static_web_app_id = azurerm_static_web_app.frontend.id
  domain_name       = var.custom_domains["frontend"]
  validation_type   = "cname-delegation"
}
