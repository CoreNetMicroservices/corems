# Look up the storage account directly (avoids depending on a foundation re-apply to publish
# the connection-string output). Name matches the foundation convention: "<rg>stor" minus dashes.
data "azurerm_storage_account" "main" {
  name                = replace("${local.foundation.resource_group_name}stor", "-", "")
  resource_group_name = local.foundation.resource_group_name
}
