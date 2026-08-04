---
inclusion: fileMatch
fileMatchPattern: "backend/**"
---

# Modularity & Project Structure Decisions

## Decision: Monorepo (Final)

After evaluating submodules, forks, multi-repo, and symlink approaches, we settled on **monorepo** for the .NET CoreMS project. All services (core and future extensions) live in one repository.

## Rationale

- Project is public and educational — simplicity wins
- One clone, one build, everything works
- No submodule pain (forgotten recursive clones, stale folders, broken PRs)
- No symlink issues on Windows
- YouTube viewers / new developers get the easiest onboarding: "clone this, run this one command"
- Extensions are just folders — unused services don't build if not wired into Aspire
- Monorepo doesn't prevent extracting services later if needed

## Rejected Alternatives

| Approach | Why Rejected |
|---|---|
| Git submodules (per service) | Solution file breaks when submodule missing; Directory.Build.props inheritance fails standalone; Aspire AppHost can't conditionally reference projects easily; CI/CD needs access to multiple repos |
| Fork + merge remotes | Accidental core changes go unnoticed; hard to uninstall extensions; merge conflicts when pulling upstream |
| Symlinks | Windows has poor symlink support; CI/CD pipelines break; IDEs don't always follow symlinks |
| Multi-repo with setup script | Same problems as submodules plus more repos to manage |

## Current Structure

```
corems-parent/                        # One repo, everything lives here
├── backend/
│   ├── aspire/                       # Orchestrator (AppHost + ServiceDefaults)
│   ├── common/                       # Shared libraries
│   ├── user-ms/                      # Core service
│   ├── communication-ms/             # Core service
│   ├── document-ms/                  # Core service
│   ├── translation-ms/              # Core service
│   ├── template-ms/                  # Core service
│   ├── <future-service>/             # Future services added as folders
│   ├── Directory.Build.props
│   ├── Directory.Packages.props
│   └── CoreMs.slnx
├── frontend/                         # React + Vite + TypeScript
├── infra/                            # Terraform (Azure deployment)
│   ├── bootstrap/
│   ├── foundation/
│   └── services/
└── README.md
```

## How New Services Are Added

1. Create a new folder in `backend/` (e.g., `campaign-ms/`)
2. Follow the standard three-layer structure: Api / Core / Infrastructure
3. Add project references to `CoreMs.slnx`
4. Wire into Aspire AppHost
5. Add Terraform Container App entry in `infra/services/container-apps.tf`
6. Add GitHub Actions build step in `.github/workflows/deploy.yml`

## Future: Aspire Dynamic Discovery (Optional Enhancement)

If the number of services grows significantly, the AppHost could scan `backend/` for service folders dynamically instead of hardcoding project references. For now, manual wiring is fine and more explicit.


