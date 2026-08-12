output "container_app_fqdns" {
  value = { for k, v in azurerm_container_app.services : k => v.ingress[0].fqdn }
}

output "static_web_app_hostname" {
  value = azurerm_static_web_app.frontend.default_host_name
}
