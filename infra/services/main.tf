terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.80"
    }
  }
}

provider "azurerm" {
  features {}
}

data "terraform_remote_state" "foundation" {
  backend = "azurerm"
  config = {
    resource_group_name  = "corems-tfstate-rg"
    storage_account_name = "coremstfstate"
    container_name       = "tfstate"
    key                  = "foundation.terraform.tfstate"
  }
}

locals {
  foundation = data.terraform_remote_state.foundation.outputs
}
