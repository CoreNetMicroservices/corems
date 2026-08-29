#!/usr/bin/env bash
#
# setup-custom-domain.sh — one-time, opt-in custom domain setup for a CoreMS deployment.
#
# Attaches a custom domain to the frontend (Static Web App, apex) and each backend service
# (Container Apps, <service>-api.<domain> subdomains), including DNS records and free managed
# TLS certificates. Idempotent: safe to re-run; already-configured domains are skipped.
#
# Why this is a script and not Terraform:
#   - SWA apex domains use dns-txt-token validation where the token is only known AFTER the
#     domain is created — a chicken-and-egg the azurerm provider can't resolve in one apply.
#   - Container Apps managed certs use a managedCertificates/ ID the azurerm provider can't
#     parse (hashicorp/terraform-provider-azurerm#27362).
#
# Prerequisites:
#   - az CLI logged in with rights on the resource group (Contributor + DNS + SWA/ACA).
#   - The DNS zone for BASE_DOMAIN already exists in Azure DNS in the same resource group,
#     and the registrar's nameservers point at that zone.
#   - Services already deployed (Container Apps + Static Web App exist).
#
# Usage:
#   BASE_DOMAIN=example.com \
#   RESOURCE_GROUP=corems-prod-rg \
#   SUBSCRIPTION_ID=<sub-id> \
#   ./setup-custom-domain.sh
#
# Optional env (defaults shown):
#   SERVICES="user-ms communication-ms document-ms translation-ms template-ms"
#   STATIC_WEB_APP="${RESOURCE_GROUP}-swa"
#   CONTAINER_APP_ENV="${RESOURCE_GROUP}-cae"
#   DNS_ZONE="$BASE_DOMAIN"          # DNS zone name if different from BASE_DOMAIN
#   SKIP_FRONTEND=false              # set true to only configure API subdomains
#   SKIP_SERVICES=false              # set true to only configure the frontend apex

set -euo pipefail

# ---- config ---------------------------------------------------------------
: "${BASE_DOMAIN:?Set BASE_DOMAIN (e.g. example.com)}"
: "${RESOURCE_GROUP:?Set RESOURCE_GROUP (e.g. corems-prod-rg)}"
: "${SUBSCRIPTION_ID:?Set SUBSCRIPTION_ID}"

SERVICES="${SERVICES:-user-ms communication-ms document-ms translation-ms template-ms}"
STATIC_WEB_APP="${STATIC_WEB_APP:-${RESOURCE_GROUP}-swa}"
CONTAINER_APP_ENV="${CONTAINER_APP_ENV:-${RESOURCE_GROUP}-cae}"
DNS_ZONE="${DNS_ZONE:-$BASE_DOMAIN}"
SKIP_FRONTEND="${SKIP_FRONTEND:-false}"
SKIP_SERVICES="${SKIP_SERVICES:-false}"

AZ="az --subscription $SUBSCRIPTION_ID"

log()  { echo -e "\033[0;36m[domain]\033[0m $*"; }
ok()   { echo -e "\033[0;32m[  ok  ]\033[0m $*"; }
warn() { echo -e "\033[0;33m[ warn ]\033[0m $*"; }
err()  { echo -e "\033[0;31m[ fail ]\033[0m $*" >&2; }

# ---- frontend (Static Web App apex, dns-txt-token) ------------------------
setup_frontend() {
  local domain="$BASE_DOMAIN"
  log "Frontend apex: $domain -> $STATIC_WEB_APP"

  local status
  status=$($AZ staticwebapp hostname show -n "$STATIC_WEB_APP" -g "$RESOURCE_GROUP" \
    --hostname "$domain" --query "status" -o tsv 2>/dev/null || echo "None")

  if [ "$status" == "Ready" ]; then
    ok "Frontend apex already Ready, skipping."
    return 0
  fi

  # Ensure the apex alias A record points at the SWA (idempotent).
  local swa_id
  swa_id=$($AZ staticwebapp show -n "$STATIC_WEB_APP" -g "$RESOURCE_GROUP" --query "id" -o tsv)
  log "Ensuring apex ALIAS A record -> SWA"
  $AZ network dns record-set a create -g "$RESOURCE_GROUP" -z "$DNS_ZONE" -n "@" \
    --target-resource "$swa_id" --ttl 300 >/dev/null 2>&1 || true

  # Register the hostname (generates a validation token). Non-blocking: --no-wait so we can
  # write the TXT record the validation needs.
  log "Registering hostname (generating validation token)..."
  $AZ staticwebapp hostname set -n "$STATIC_WEB_APP" -g "$RESOURCE_GROUP" \
    --hostname "$domain" --validation-method dns-txt-token --no-wait >/dev/null 2>&1 || true

  # Poll for the token (it can take a minute to generate).
  local token=""
  for _ in $(seq 1 12); do
    token=$($AZ staticwebapp hostname show -n "$STATIC_WEB_APP" -g "$RESOURCE_GROUP" \
      --hostname "$domain" --query "validationToken" -o tsv 2>/dev/null || echo "")
    [ -n "$token" ] && break
    sleep 5
  done

  if [ -z "$token" ]; then
    err "Never received a validation token for $domain"
    return 1
  fi
  log "Validation token: $token"

  # Replace the apex TXT record with the current token.
  $AZ network dns record-set txt delete -g "$RESOURCE_GROUP" -z "$DNS_ZONE" -n "@" --yes >/dev/null 2>&1 || true
  $AZ network dns record-set txt create -g "$RESOURCE_GROUP" -z "$DNS_ZONE" -n "@" --ttl 300 >/dev/null 2>&1 || true
  $AZ network dns record-set txt add-record -g "$RESOURCE_GROUP" -z "$DNS_ZONE" -n "@" \
    --value "$token" >/dev/null
  ok "Apex TXT record set."

  log "Azure will validate the apex domain in the background (can take 15+ minutes for apex)."
  log "Check status:  az staticwebapp hostname show -n $STATIC_WEB_APP -g $RESOURCE_GROUP --hostname $domain --query status -o tsv"
}

# ---- backend services (Container Apps subdomains, CNAME + managed cert) ----
setup_services() {
  for svc in $SERVICES; do
    # user-ms -> user-api, communication-ms -> communication-api, ...
    local subdomain="${svc%-ms}-api.${BASE_DOMAIN}"
    local label="${svc%-ms}-api"
    log "Service $svc -> $subdomain"

    local binding
    binding=$($AZ containerapp hostname list -g "$RESOURCE_GROUP" -n "$svc" \
      --query "[?name=='$subdomain'].bindingType" -o tsv 2>/dev/null || echo "")
    if [ "$binding" == "SniEnabled" ]; then
      ok "$subdomain already bound, skipping."
      continue
    fi

    local fqdn verify_id
    fqdn=$($AZ containerapp show -g "$RESOURCE_GROUP" -n "$svc" \
      --query "properties.configuration.ingress.fqdn" -o tsv)
    verify_id=$($AZ containerapp show -g "$RESOURCE_GROUP" -n "$svc" \
      --query "properties.customDomainVerificationId" -o tsv)

    # CNAME <label> -> <app fqdn>, and asuid.<label> TXT -> verification id (idempotent).
    log "Ensuring CNAME + asuid TXT for $label"
    $AZ network dns record-set cname set-record -g "$RESOURCE_GROUP" -z "$DNS_ZONE" \
      --record-set-name "$label" --cname "$fqdn" --ttl 300 >/dev/null 2>&1 || true
    $AZ network dns record-set txt delete -g "$RESOURCE_GROUP" -z "$DNS_ZONE" -n "asuid.$label" --yes >/dev/null 2>&1 || true
    $AZ network dns record-set txt create -g "$RESOURCE_GROUP" -z "$DNS_ZONE" -n "asuid.$label" --ttl 300 >/dev/null 2>&1 || true
    $AZ network dns record-set txt add-record -g "$RESOURCE_GROUP" -z "$DNS_ZONE" -n "asuid.$label" \
      --value "$verify_id" >/dev/null

    # Bind hostname + provision managed cert, retrying to absorb DNS propagation.
    local bound=false
    for attempt in $(seq 1 6); do
      if $AZ containerapp hostname bind --hostname "$subdomain" -g "$RESOURCE_GROUP" \
           -n "$svc" --environment "$CONTAINER_APP_ENV" --validation-method CNAME >/dev/null 2>&1; then
        bound=true
        ok "Bound $subdomain (attempt $attempt)."
        break
      fi
      warn "Bind attempt $attempt failed (DNS likely not propagated), waiting 30s..."
      sleep 30
    done
    if [ "$bound" != "true" ]; then
      err "Failed to bind $subdomain after retries."
      return 1
    fi
  done
}

# ---- main -----------------------------------------------------------------
log "Custom domain setup for base domain: $BASE_DOMAIN (RG: $RESOURCE_GROUP)"

if [ "$SKIP_FRONTEND" != "true" ]; then setup_frontend; else log "Skipping frontend."; fi
if [ "$SKIP_SERVICES" != "true" ]; then setup_services; else log "Skipping services."; fi

ok "Custom domain setup complete."
