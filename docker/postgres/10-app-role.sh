#!/bin/sh
# Runs once, on a fresh data volume (the postgres image's docker-entrypoint-initdb.d), as the
# owner. Creates the API's least-privilege login with APP_DB_PASSWORD -- quoted by format(%L), so
# any password is safe -- then applies the grants in docs/db-roles.sql, whose CREATE ROLE is
# skipped because the role now exists.
set -eu
: "${APP_DB_PASSWORD:?set APP_DB_PASSWORD in .env for the API's database login}"
psql -v ON_ERROR_STOP=1 -v app_password="$APP_DB_PASSWORD" --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<'SQL'
SELECT format('CREATE ROLE educore_app LOGIN PASSWORD %L', :'app_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'educore_app')\gexec
\i /docker-entrypoint-initdb.d/db-roles.sql.template
SQL
