variable "resource_group_name" {
  type        = string
  description = "Name of the Azure resource group"
}

variable "location" {
  type        = string
  description = "Azure region for all resources"
  default     = "northeurope"
}

variable "postgres_admin_username" {
  type        = string
  description = "PostgreSQL administrator username"
  sensitive   = true
}

variable "postgres_admin_password" {
  type        = string
  description = "PostgreSQL administrator password"
  sensitive   = true
}

variable "dns_zone_domain" {
  type        = string
  description = "Domain name for the DNS zone"
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to all resources"
  default = {
    environment = "production"
    project     = "corems"
  }
}
