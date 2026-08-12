terraform {
  backend "azurerm" {
    resource_group_name  = "corems-tfstate-rg"
    storage_account_name = "coremstfstate"
    container_name       = "tfstate"
    key                  = "foundation.terraform.tfstate"
  }
}
