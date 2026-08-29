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

