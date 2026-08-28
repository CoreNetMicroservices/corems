variable "max_replicas" {
  type        = number
  description = "Maximum number of replicas per Container App"
  default     = 5
}

variable "image_tags" {
  type        = map(string)
  description = "Container image tags per service"
  default = {
    user-ms          = "latest"
    communication-ms = "latest"
    document-ms      = "latest"
    translation-ms   = "latest"
    template-ms      = "latest"
  }
}

variable "custom_domains" {
  type        = map(string)
  description = "Custom domain names for services (computed from base_domain if not overridden)"
  default = {
    frontend         = ""
    user-ms          = ""
    communication-ms = ""
    document-ms      = ""
    translation-ms   = ""
    template-ms      = ""
  }
}

variable "base_domain" {
  type        = string
  description = "Base domain for the application (e.g., core-microservices.com). Subdomains are derived automatically."
  default     = ""
}
