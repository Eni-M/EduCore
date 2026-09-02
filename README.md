# EduCore

School management platform — REST API (.NET) with a React front end and PostgreSQL.

## Run it (Docker)

The two repositories must sit side by side:

```
parent/
  EduCore/        # this repo (API + compose)
  EduCoreFront/   # the web front end
```

From `EduCore/`:

```bash
cp .env.example .env
# edit .env: set Jwt__Key, Encryption__Key, POSTGRES_PASSWORD, and (first run only) SuperAdmin__Email / SuperAdmin__Password
docker compose up -d --build
```

Open **http://localhost:8080** and sign in with the `SuperAdmin` account. The database
schema is created automatically on start.

Put HTTPS in front of the web service for anything public, then set `Session__Secure=true`
in `.env` and restart.

## Local development

```bash
docker compose up -d db     # Postgres only
dotnet run                  # API — needs Jwt__Key, Encryption__Key and ConnectionStrings__Default in .env
```

Run the front end from `../EduCoreFront` with `npm run dev`.

Tests: `dotnet run --project EduCore.Tests/EduCore.Tests.csproj`
