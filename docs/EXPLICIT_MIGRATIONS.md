# Explicit database migrations (the "trio")

Finished 2026-09-04. Started by Claude (Program.cs, Dockerfile), completed here
(compose wiring, docs). Plan reference: `docs/PERFORMANCE_PLAN.md` §1.3.

## What changed and why

The API used to run `MigrateAsync()` on every boot, in every environment. That meant:
every replica raced the others to migrate on start, and a broken migration took the
API down with it instead of failing somewhere visible. The new shape separates the
two concerns: migrating is an explicit step, serving refuses a mismatched schema.

| File | Change | Why |
|---|---|---|
| `Program.cs` | `--migrate` flag path: applies pending migrations and exits (non-zero on failure) | One obvious, rerunnable place to migrate; safe to invoke from compose, Render, or a terminal |
| `Program.cs` | Startup gate: throws with an actionable message when migrations are pending | Fail fast with instructions instead of discovering drift one failing query at a time |
| `Program.cs` | Seeding untouched and still in serve-mode only | `--migrate` skips `PlatformOwnerSeeder`; first-run admin creation still happens at API startup |
| `Dockerfile` | `HEALTHCHECK` on `/healthz` (liveness only) + `curl` for the probe | A DB outage must not restart-loop the container while someone diagnoses it; 30 s start-period covers the schema gate |
| `compose.yaml` | One-shot `migrate` service (`dotnet EduCore.dll --migrate`, `restart: "no"`); `api` waits for `db` healthy **and** `migrate` completed successfully; `web` waits for healthy `api` | The missing piece: without it the API crash-loops on any behind database with nothing ever migrating it |
| `docs/DEV-SETUP.md` | Dev flow, Docker notes, and production-migration item rewritten | The old text ("migrations run on startup / hit production automatically") is now false |

## How each environment migrates now

| Environment | Command | What happens |
|---|---|---|
| Local dev | `dotnet run -- --migrate` (only when the model changed), then `dotnet run` | One-shot migration, then normal server; forgetting prints the exact command |
| Docker compose | `docker compose up -d --build` | `migrate` runs to completion first; `docker compose logs migrate` is the first place to look if the API won't boot |
| Render (production) | Set `MigrateOnStart=true` in the service's environment | Render's Pre-Deploy Command is a paid feature, so the one-shot step has nowhere to live. Safe **only** because the service runs a single instance — two replicas would race. Turn it off and restore a separate migrate step before scaling past one |

## Rollback

Revert this commit set and boot-time auto-migrate returns. Alternatively, without
reverting: run the migrate command for your environment by hand and (re)start the API.

## Verification performed

- `dotnet build EduCore.csproj` — green (proves the `Program.cs` changes compile).
- `docker compose config --quiet` — green; services resolve as `db`, `migrate`, `api`, `web`.
- Read-through: migrate mode returns before seeding/mapping/serving; serve mode still
  seeds; the gate needs only a DB connection; healthcheck needs no privileges beyond
  the existing `$APP_UID` user.
- Not verified here (needs environments, not code): an actual fresh-DB compose boot,
  and the Render `MigrateOnStart` path — do both before relying on them.
