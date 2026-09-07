# EduCore

[![CI](https://github.com/EniMirenaCacttus/EduCore/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/EniMirenaCacttus/EduCore/actions/workflows/ci.yml?query=branch%3Amain)

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
```

The stack is served over HTTPS, so nginx needs a certificate at `certs/fullchain.pem`
and `certs/privkey.pem`. For local use, generate a self-signed one:

```bash
mkdir -p certs && openssl req -x509 -newkey rsa:2048 -nodes -days 365 \
  -keyout certs/privkey.pem -out certs/fullchain.pem \
  -subj "/CN=localhost" -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"
```

On Git Bash for Windows, prefix that command with `MSYS_NO_PATHCONV=1` — otherwise Git
Bash rewrites `/CN=localhost` into a Windows path and openssl rejects the subject.

```bash
docker compose up -d --build
```

Open **https://localhost:8443** and sign in with the `SuperAdmin` account. The browser
warns once about the self-signed certificate; accept it for local use. Port 8080 still
answers, but only to redirect to 8443. The database schema is created automatically on
start.

For anything public, replace `certs/` with a real certificate for your domain, set
`PUBLIC_ORIGIN` to that domain in `.env`, and set `HSTS_MAX_AGE=31536000`. `certs/` is
gitignored — the private key never goes in the repository.

## Local development

```bash
docker compose up -d db     # Postgres only
dotnet run                  # API — needs Jwt__Key, Encryption__Key and ConnectionStrings__Default in .env
```

Run the front end from `../EduCoreFront` with `npm run dev`.

Tests: `dotnet run --project EduCore.Tests/EduCore.Tests.csproj`
