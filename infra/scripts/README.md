# Infra Scripts

One-off operational scripts that sit outside the regular Terraform/CI flow.

## `setup-custom-domain.sh`

Attaches a custom domain to a deployed CoreMS environment. **Optional and one-time** — most
deployments run fine on the default Azure hostnames (`*.azurestaticapps.net` for the frontend,
`*.azurecontainerapps.io` for the APIs), so you only run this if you want a branded domain.

It configures:

- **Frontend (Static Web App):** apex domain (e.g. `example.com`) via alias `A` record +
  `dns-txt-token` validation, with a free managed TLS certificate.
- **Backend services (Container Apps):** `<service>-api.<domain>` subdomains (e.g.
  `user-api.example.com`) via `CNAME` + `asuid` TXT validation, each with a free managed cert.

### Why it's not in Terraform

The Azure providers can't cleanly express this:

1. **SWA apex** uses `dns-txt-token` validation, where the token only exists *after* the
   custom-domain resource is created — a chicken-and-egg a single `terraform apply` can't solve.
2. **Container Apps managed certificates** are created under a `managedCertificates/` resource
   ID the `azurerm` provider cannot parse
   ([hashicorp/terraform-provider-azurerm#27362](https://github.com/hashicorp/terraform-provider-azurerm/issues/27362)).

Keeping domain setup as an idempotent script keeps the core Terraform + deploy pipeline simple
and domain-agnostic.

### Prerequisites

- `az` CLI logged in with rights on the resource group.
- The DNS zone for your domain already exists in **Azure DNS** in the same resource group, and
  your registrar's nameservers point at that zone.
- Services already deployed (Container Apps + Static Web App exist).

### Usage

```bash
BASE_DOMAIN=example.com \
RESOURCE_GROUP=corems-prod-rg \
SUBSCRIPTION_ID=<your-subscription-id> \
./setup-custom-domain.sh
```

Optional overrides (defaults shown):

| Env var | Default | Purpose |
|---------|---------|---------|
| `SERVICES` | `user-ms communication-ms document-ms translation-ms template-ms` | Services to attach subdomains to |
| `STATIC_WEB_APP` | `${RESOURCE_GROUP}-swa` | Static Web App resource name |
| `CONTAINER_APP_ENV` | `${RESOURCE_GROUP}-cae` | Container Apps environment name |
| `DNS_ZONE` | `$BASE_DOMAIN` | DNS zone name if it differs from the base domain |
| `SKIP_FRONTEND` | `false` | Set `true` to only configure API subdomains |
| `SKIP_SERVICES` | `false` | Set `true` to only configure the frontend apex |

The script is **idempotent** — already-configured domains are detected and skipped, so it's
safe to re-run.

### Notes

- Apex domain validation on Azure Static Web Apps can take 15+ minutes to flip from
  `Validating` to `Ready`. The script sets everything up and lets Azure validate in the
  background. Check status with:
  ```bash
  az staticwebapp hostname show -n corems-prod-rg-swa -g corems-prod-rg \
    --hostname example.com --query status -o tsv
  ```
- Managed certificates auto-renew; no further action is needed once bound.
