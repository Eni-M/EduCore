# EduCore — Developer Setup

How to run EduCore on your own machine and work alongside the team.

## The mental model (read this first)

There are **two separate worlds**. Don't mix them up.

| | **Production** (live) | **Local** (your machine) |
|---|---|---|
| Database | Neon | Postgres in Docker |
| Backend | Render | `dotnet run` |
| Frontend | Vercel (`edu-core-front.vercel.app`) | Vite dev server |
| When it updates | when code is merged to `main` | instantly, as you edit |
| Who touches it | **nobody directly** — only by merging to `main` | just you |

You do **all** your work in the Local world. You never touch Production by hand —
it updates itself when your Pull Request is merged into `main`.

---

## 1. Prerequisites

- **Docker Desktop** (running)
- **.NET 10 SDK**
- **Node 20+**
- **Git**

## 2. Clone both repos as siblings

The backend's `compose.yaml` builds the frontend from `../EduCoreFront`, so the two
folders **must sit next to each other**:

```
your-folder/
  EduCore/         # backend (this repo)
  EduCoreFront/    # frontend
```

```bash
git clone https://github.com/EniMirenaCacttus/EduCore.git
git clone https://github.com/EniMirenaCacttus/EduCoreFront.git
```

## 3. Create the backend `.env`

In `EduCore/`, copy the example and fill in the two required keys:

```bash
cp .env.example .env
```

Then edit `EduCore/.env`:

```
Jwt__Key=<a 32-byte key, e.g. openssl rand -base64 32>
Encryption__Key=<a 32-byte base64 key, e.g. openssl rand -base64 32>
ConnectionStrings__Default=Host=localhost;Port=5434;Database=EduCoreDB;Username=educore;Password=educore123
```

> **Team rule:** use the **same `Encryption__Key`** as everyone else (get it from the
> team password manager). National IDs are encrypted with it — a different key means
> the seed data's IDs come out unreadable. `.env` is gitignored and never committed.

## 4. Start the database

```bash
docker compose up -d db
```

This runs **only Postgres** (on `localhost:5434`), which is all you need for development.

## 5. Load the sample data (recommended)

The database starts empty. To get the full sample dataset (5,000 students, etc.),
load the seed dump **into the fresh database, before you run the backend**:

```bash
docker exec -i EduCore-db psql -U educore -d EduCoreDB -v ON_ERROR_STOP=1 < seed-dump.sql
```

> Already ran the backend once (so the DB isn't empty anymore)? Reset it first:
> ```bash
> docker compose down -v && docker compose up -d db
> ```
> then load the seed.
>
> Prefer to start empty instead? Skip this step and set `SuperAdmin__Email` /
> `SuperAdmin__Password` in `.env` — the backend creates one admin account on first run.

## 6. Run the app (dev mode, hot reload)

Two terminals:

```bash
# terminal 1 — backend (http://localhost:5128)
cd EduCore
dotnet run
```

```bash
# terminal 2 — frontend (http://localhost:5173)
cd EduCoreFront
npm install
npm run dev
```

Open **http://localhost:5173**. Edits reload automatically. The backend refuses to
start against a schema it does not match, so apply migrations explicitly first:

```bash
cd EduCore
dotnet run -- --migrate   # one-shot: applies pending migrations and exits
dotnet run                # normal dev server
```

Forget the flag and the startup error tells you exactly this. You only need the
migrate step when the model changed (new migration), not on every run.

### Alternative: run everything in Docker

To run the whole stack the way production does (no hot reload — rebuilds on change):

```bash
docker compose up -d --build   # app at http://localhost:8080
```

The one-shot `migrate` container applies pending migrations before the API starts,
and the frontend waits for a healthy API. If the API refuses to boot, check
`docker compose logs migrate` first — a failed migration stops everything downstream
by design.

Use this to sanity-check the production build; use the dev mode above for actual work.

---

## Daily workflow

`main` is **production**. Merging to it deploys to Render + Vercel automatically.
So never commit to `main` directly:

```bash
git checkout main
git pull                       # get the latest
git checkout -b my-feature     # branch for your work

# ...make changes, commit as you go...
git add -A && git commit -m "Describe the change"

git push -u origin my-feature  # push your branch
```

Then open a **Pull Request** on GitHub, get it reviewed, and merge into `main`.
The merge is what ships it to production.

### Two things to be careful about

1. **Database migrations no longer run on their own.** The API (and `dotnet run`)
   refuses to start when the database is behind the code. Flow: add the EF migration,
   test it locally (`dotnet run -- --migrate` against your dev DB), then merge.
   `docker compose` migrates automatically via the one-shot `migrate` service before
   the API starts. **Render needs a Pre-Deploy Command** running the same step
   (`dotnet <publish-path>/EduCore.dll --migrate` — adjust the path to match the
   service's existing Start Command): without it, a deploy with pending migrations
   crash-loops the API by design. Test migrations locally first; be very deliberate
   with anything that drops columns or tables.
2. **Never edit the Neon / Render / Vercel settings by hand** to "try something." Change
   things through code + a Pull Request so the whole team stays in sync.

## Ports reference

| Service | URL |
|---|---|
| Frontend (dev) | http://localhost:5173 |
| Backend (dev) | http://localhost:5128 |
| Full Docker stack | http://localhost:8080 |
| Postgres | localhost:5434 (user `educore`, db `EduCoreDB`) |
