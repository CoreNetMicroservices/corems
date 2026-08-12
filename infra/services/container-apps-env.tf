resource "azurerm_log_analytics_workspace" "main" {
  name                = "${local.foundation.resource_group_name}-logs"
  resource_group_name = local.foundation.resource_group_name
  location            = data.azurerm_resource_group.main.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

data "azurerm_resource_group" "main" {
  name = local.foundation.resource_group_name
}

resource "azurerm_container_app_environment" "main" {
  name                       = "${local.foundation.resource_group_name}-cae"
  resource_group_name        = local.foundation.resource_group_name
  location                   = data.azurerm_resource_group.main.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
  }
}
