data "azurerm_dns_zone" "main" {
  name                = local.foundation.dns_zone_domain
  resource_group_name = local.foundation.resource_group_name
}

# CNAME records for Container App services (subdomains)
resource "azurerm_dns_cname_record" "services" {
  for_each = { for k, v in local.services : k => v if local.domains[k] != "" }

  name                = split(".", local.domains[each.key])[0]
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = local.foundation.resource_group_name
  ttl                 = 300
  record              = azurerm_container_app.services[each.key].ingress[0].fqdn
}

# TXT records for Container App custom domain validation
resource "azurerm_dns_txt_record" "services_validation" {
  for_each = { for k, v in local.services : k => v if local.domains[k] != "" }

  name                = "asuid.${split(".", local.domains[each.key])[0]}"
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = local.foundation.resource_group_name
  ttl                 = 300

  record {
    value = azurerm_container_app.services[each.key].custom_domain_verification_id
  }
}

# Frontend: apex domain uses an alias A record
resource "azurerm_dns_a_record" "frontend_apex" {
  count = local.domains["frontend"] == local.foundation.dns_zone_domain ? 1 : 0

  name                = "@"
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = local.foundation.resource_group_name
  ttl                 = 300

  target_resource_id = azurerm_static_web_app.frontend.id
}

# Frontend: subdomain uses a CNAME record (e.g., www.core-microservices.com)
resource "azurerm_dns_cname_record" "frontend" {
  count = local.domains["frontend"] != "" && local.domains["frontend"] != local.foundation.dns_zone_domain ? 1 : 0

  name                = split(".", local.domains["frontend"])[0]
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = local.foundation.resource_group_name
  ttl                 = 300
  record              = azurerm_static_web_app.frontend.default_host_name
}

# NOTE: Container App custom domain binding + managed certificate is handled by the
# "Bind managed certificates to custom domains" step in deploy.yml via `az containerapp
# hostname bind`. The azurerm_container_app_custom_domain resource is intentionally NOT
# used here because it conflicts with CLI-managed certificates and cannot parse the
# managedCertificates/ ID format (provider bug hashicorp/terraform-provider-azurerm#27362).
# Terraform only provisions the DNS records (CNAME + asuid TXT) needed for validation.

# Custom domain for Static Web App
resource "azurerm_static_web_app_custom_domain" "frontend" {
  count = local.domains["frontend"] != "" ? 1 : 0

  static_web_app_id = azurerm_static_web_app.frontend.id
  domain_name       = local.domains["frontend"]
  validation_type   = local.domains["frontend"] == local.foundation.dns_zone_domain ? "dns-txt-token" : "cname-delegation"
}
