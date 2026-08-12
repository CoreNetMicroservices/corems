resource "azurerm_static_web_app" "frontend" {
  name                = "${local.foundation.resource_group_name}-swa"
  resource_group_name = local.foundation.resource_group_name
  location            = "eastus2"
  sku_tier            = "Free"
  sku_size            = "Free"

  app_settings = {
    VITE_USER_MS_BASE_URL          = "https://${azurerm_container_app.services["user-ms"].ingress[0].fqdn}"
    VITE_COMMUNICATION_MS_BASE_URL = "https://${azurerm_container_app.services["communication-ms"].ingress[0].fqdn}"
    VITE_DOCUMENT_MS_BASE_URL      = "https://${azurerm_container_app.services["document-ms"].ingress[0].fqdn}"
    VITE_TRANSLATION_MS_BASE_URL   = "https://${azurerm_container_app.services["translation-ms"].ingress[0].fqdn}"
    VITE_TEMPLATE_MS_BASE_URL      = "https://${azurerm_container_app.services["template-ms"].ingress[0].fqdn}"
  }
}
