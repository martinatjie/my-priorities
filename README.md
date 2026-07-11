# My Priorities

A simple .NET 8 console app for ranking items through pairwise comparisons. Create multiple independent lists, add or edit items, and prioritize them by repeatedly choosing which of two items matters more.

## Features

- Multiple independent priority lists
- Add, edit, and delete items
- Insertion-style ranking with binary-search comparisons (fewer prompts than comparing every pair)
- Rankings persisted to a JSON file outside the repository
- Partial ranking support — prioritize only new items without re-ranking the full list
- Configurable auto-prioritize-on-add behaviour
- Cancel and skip during comparison sessions

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run

### Console app

```bash
cd PrioritizationApp.Console
dotnet run
```

### Web app (local)

```bash
cd PrioritizationApp.Web
dotnet run
```

Open the URL shown in the terminal (HTTPS). In Development, auth is open unless you configure Google OAuth and an email allowlist.

On startup the console app prints the data file path in use. The web app stores data in SQLite under `Storage:DataDirectory` (`.` locally, `/data` in Docker).

## Usage

**Main menu**

| Option | Action |
|--------|--------|
| 1 | Select or create a list |
| 2 | Settings |
| 3 | Exit |

**Settings**

| Option | Action |
|--------|--------|
| 1 | Toggle auto prioritize on add (ON / OFF) |
| 2 | Back |

**List menu**

| Option | Action |
|--------|--------|
| 1 | View items |
| 2 | Add item |
| 3 | Edit item |
| 4 | Delete item |
| 5 | Prioritize (pairwise comparisons) |
| 6 | View ranking |
| 7 | Back |

During prioritization you pick the higher-priority item each time.

**Comparison controls** (shown at the start of each session):

| Input | Action |
|-------|--------|
| `1` / `2` | Pick higher-priority item |
| Enter | Skip — leave current item unprioritized, continue with next |
| `x` | Cancel — save progress so far and return to menu |

**Incremental ranking** (when a list is already partially or fully prioritized):

- **Add** (auto ON) — prompted immediately to place the new item; existing order is preserved
- **Add** (auto OFF) — item is added without prompts; run Prioritize later to rank only unprioritized items
- **Edit** — text changes only; ranking is unchanged
- **Delete** — item is removed from the list and ranking without re-prompting

**Prioritize** (option 5):

- Never ranked — ranks all items
- Partially ranked — ranks only unprioritized items into the existing order
- Fully ranked — warns that all items will be re-ranked; asks for confirmation before proceeding

## Data storage

List data is **not** committed to git. By default, `dotnet run` uses Development settings and stores data at:

```
%LOCALAPPDATA%\PrioritizationApp\prioritization-data.json
```

### Configuration

Settings are loaded in this order (later wins):

1. `PrioritizationApp.Console/appsettings.json`
2. `PrioritizationApp.Console/appsettings.Development.json` (when `DOTNET_ENVIRONMENT=Development`)
3. User secrets (machine-local, not in the repo)
4. Environment variable `PRIORITIZATION_DATA__FILEPATH`

**User secrets example**

```bash
cd PrioritizationApp.Console
dotnet user-secrets set "Data:FilePath" "%LOCALAPPDATA%\PrioritizationApp\prioritization-data.json"
```

See `PrioritizationApp.Console/user-secrets.example.json` for the key format.

**Environment variable example**

```bash
set PRIORITIZATION_DATA__FILEPATH=C:\path\to\prioritization-data.json
dotnet run
```

## Tests

```bash
dotnet test PrioritizationApp.sln
```

### Mutation tests (Stryker)

```bash
dotnet tool install -g dotnet-stryker
dotnet-stryker --config-file stryker-config.json
```

## Project layout

```
PrioritizationApp.Core/       Models, services, configuration, JSON repository
PrioritizationApp.Console/    Console host and menus
PrioritizationApp.Web/        Blazor web app (mobile-first UI)
PrioritizationApp.Tests/      xUnit tests for Core
```

## Server setup (production web app)

GitHub Actions **builds and deploys** the Docker image on push to `main`. It does **not** set up the server from scratch. Complete this checklist once before the first deploy.

### One-time on the server

| Step | Command / action |
|------|------------------|
| DNS | `A` record: `priorities.example.com` → your server IP |
| Docker volume | `docker volume create priorities-data` |
| Swarm + Traefik | Configure reverse proxy and overlay network (e.g. existing Traefik on Docker Swarm) |
| GHCR login | `docker login ghcr.io` (deploy user; CI also logs in via `GHCR_PAT` on deploy) |

### One-time in GitHub (Actions)

GitHub distinguishes **secrets** (sensitive, hidden) from **variables** (non-sensitive config).  
Go to: **Repository → Settings → Secrets and variables → Actions**

Hostnames are stored as **secrets** so your live site URL is not visible in the repo or in GitHub Variables. Forks use their own `DEPLOY_HOST` / `SSL_HOST` values.

#### Secrets

| Name | Example / notes |
|------|-----------------|
| `GHCR_PAT` | Personal access token with `read:packages` — server uses this to pull images |
| `SERVER_IP` | Your server IP address |
| `SERVER_USER` | SSH user (e.g. `deploy`) |
| `SERVER_SSH_KEY` | Private SSH key for deploy user |
| `DEPLOY_HOST` | Public hostname for this app (e.g. `priorities.example.com`) |
| `SSL_HOST` | Root domain for Traefik STS header (e.g. `example.com`) |
| `GOOGLE_CLIENT_ID` | Google OAuth client ID |
| `GOOGLE_CLIENT_SECRET` | Google OAuth client secret |
| `ALLOWED_EMAIL_1` | First allowed sign-in email |
| `ALLOWED_EMAIL_2` | Second allowed sign-in email (optional; leave empty if unused) |

`GITHUB_TOKEN` is provided automatically by GitHub Actions for pushing images to GHCR.

#### Variables

| Name | Example / notes |
|------|-----------------|
| `IMAGE_TAG` | Docker image tag to build and deploy (e.g. `2026.07.11.1`) — bump when releasing |
| `TRAEFIK_CERT_RESOLVER` | Traefik cert resolver name on your server (default in workflow: `mytlschallenge`) |

Secrets and variables are substituted into `docker-compose-stack.yml` on the server during deploy.

### One-time in Google Cloud Console

Add authorized redirect URI (use your `DEPLOY_HOST` value):

```
https://<DEPLOY_HOST>/signin-google
```

Example: `https://priorities.example.com/signin-google`

### What CI/CD does on push to `main`

- Build and push `ghcr.io/<repository-owner>/my-priorities/prioritization-app:<IMAGE_TAG>`
- Copy `docker-compose-stack.yml` to the server
- Export secrets/variables and run `docker stack deploy` for stack `prioritiesstack`

Bump `IMAGE_TAG` in GitHub **Variables** when releasing a new version.

### Troubleshooting: connection timeout (Traefik ingress)

If the app container is running (`1/1` replicas, logs show "Application started") but the site times out in the browser (`ERR_CONNECTION_TIMED_OUT`), check whether the host is listening on 80/443:

```bash
ss -tlnp | grep -E ':80|:443'
```

If that returns nothing while other published ports work (e.g. n8n on `5678`), Swarm ingress for Traefik may be stuck. Restart the Traefik service (adjust the service name to match your server):

```bash
docker service update --force traefik_stack_traefik
```

After ~30 seconds, 80/443 should appear in `ss` output. Verify routing inside the overlay network:

```bash
docker run --rm --network traefik_swarm_nw curlimages/curl:8.5.0 -sI \
  -H "Host: <your-DEPLOY_HOST>" \
  http://tasks.traefik_stack_traefik:80
```

A `308` redirect to `https://...` means Traefik routing is working. This is a Traefik/ingress issue, not the app stack — redeploying the app alone will not fix it.

## License

Personal project — use and modify as you like.
