# Aspire Configuration Guide

## Overview

The AppHost (`backend/aspire/CoreMs.AppHost/`) is the single place that wires secrets and environment variables into all services. Configuration is split across two files with identical structure — one committed, one gitignored.

## Files

| File | Committed | Purpose |
|------|-----------|---------|
| `appsettings.json` | ✅ Yes | Placeholder skeleton — all keys present, all values empty or safe defaults |
| `appsettings.Development.json` | ❌ No (gitignored) | Real values for local development |

**Rule: every key that exists in `appsettings.json` must also exist in `appsettings.Development.json`, and vice versa.** The two files must stay in sync.

## Structure

Configuration is grouped by service. Each section maps directly to what `Program.cs` reads via `builder.Configuration.GetSection(...)`.

```json
{
  "Parameters": {
    // Aspire named parameters — infrastructure credentials
    // Injected via builder.AddParameter(), not GetSection()
  },
  "Common": {
    // Shared across all services (e.g. JWT signing key)
  },
  "UserMs": {
    // Secrets only user-ms needs
  },
  "CommunicationMs": {
    // Secrets only communication-ms needs
  },
  "DocumentMs": {
    // Secrets only document-ms needs
  }
  // Add a new section when a new service needs its own secrets
}
```

## How Secrets Flow to Services

`Program.cs` reads each section and passes values to services via `.WithEnvironment()`:

```csharp
var common = builder.Configuration.GetSection("Common");
var userMs_ = builder.Configuration.GetSection("UserMs");
var communicationMs_ = builder.Configuration.GetSection("CommunicationMs");
var documentMs_ = builder.Configuration.GetSection("DocumentMs");

builder.AddProject<Projects.CoreMs_UserMs_Api>("user-ms")
    .WithEnvironment("Jwt__SecretKey", common["JwtSecretKey"] ?? "")
    .WithEnvironment("SocialAuth__Google__ClientId", userMs_["GoogleClientId"] ?? "");
```

The `__` double-underscore maps to nested config sections in the target service (e.g. `Jwt__SecretKey` → `Jwt:SecretKey`).

## Adding a New Secret — Checklist

When adding any new secret or config value to a service via AppHost:

- [ ] Add the key with an empty/default value to `appsettings.json` (committed placeholder)
- [ ] Add the key with the real dev value to `appsettings.Development.json` (local only)
- [ ] Add `.WithEnvironment("Section__Key", section["Key"] ?? "fallback")` in `Program.cs`
- [ ] If it's a new service, add a new `GetSection` variable and a new JSON section to both files
- [ ] Never add real credentials to `appsettings.json`

## Adding a New Service — Checklist

- [ ] Add a new section in `appsettings.json` with all keys empty
- [ ] Add the same section in `appsettings.Development.json` with real dev values
- [ ] Add `var newMs_ = builder.Configuration.GetSection("NewMs");` in `Program.cs`
- [ ] Wire all env vars via `.WithEnvironment(...)` on the new project resource
- [ ] Add the service entry to the port reference table below

## Port Reference

| Service | Port |
|---------|------|
| user-ms | 5100 |
| communication-ms | 5101 |
| document-ms | 5102 |
| translation-ms | 5103 |
| template-ms | 5104 |
| PostgreSQL | 5432 |
| RabbitMQ | 5672 |
| MinIO API | 9000 |
| MinIO Console | 9001 |
| pgAdmin | dynamic |

## Current Secret Inventory

### Parameters (infrastructure — via `builder.AddParameter`)
| Key | Used by |
|-----|---------|
| `postgres-password` | Postgres container |
| `rabbitmq-password` | RabbitMQ container |
| `minio-access-key` | MinIO container |
| `minio-secret-key` | MinIO container |

### Common
| Key | Used by |
|-----|---------|
| `JwtSecretKey` | All services — JWT token validation |

### UserMs
| Key | Used by |
|-----|---------|
| `JwtPrivateKeyBase64` | user-ms — RS256 token signing |
| `JwtPublicKeyBase64` | user-ms — RS256 token verification |
| `GoogleClientId` | user-ms — Google OAuth |
| `GoogleClientSecret` | user-ms — Google OAuth |
| `GitHubClientId` | user-ms — GitHub OAuth |
| `GitHubClientSecret` | user-ms — GitHub OAuth |
| `LinkedInClientId` | user-ms — LinkedIn OAuth |
| `LinkedInClientSecret` | user-ms — LinkedIn OAuth |

### CommunicationMs
| Key | Used by |
|-----|---------|
| `MailEnabled` | communication-ms — toggles real SMTP sending |
| `MailHost` | communication-ms — SMTP server hostname |
| `MailPort` | communication-ms — SMTP port (587 = STARTTLS, 465 = SSL) |
| `MailUsername` | communication-ms — SMTP auth username |
| `MailPassword` | communication-ms — SMTP auth password |
| `MailDefaultFrom` | communication-ms — default From address |
| `MailUseSsl` | communication-ms — use implicit SSL (false = STARTTLS) |

### DocumentMs
| Key | Used by |
|-----|---------|
| `DocumentLinkSigningKey` | document-ms — signs pre-signed download URLs |
