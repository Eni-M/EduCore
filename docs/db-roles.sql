-- Least privilege for the API's database login.
--
-- The schema owner (the role that runs migrations: compose's POSTGRES_USER, or Neon's project
-- owner) keeps CREATE / ALTER / DROP. The API connects as educore_app, which can read and write
-- rows and nothing else -- so a SQL injection or a compromised API process cannot drop a table,
-- alter a column or read another database's catalog of privileges.
--
-- Run once, connected as the schema owner, on the application database. Replace the password.
-- Compose runs the same statements from docker/postgres/10-app-role.sh on a fresh volume.
--
-- Then:
--   API          ConnectionStrings__Default    -> Username=educore_app;Password=...
--   migrations   ConnectionStrings__Migrations -> the owner's connection string
-- On Neon, point Migrations at the direct (non -pooler) endpoint.

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'educore_app') THEN
        CREATE ROLE educore_app LOGIN PASSWORD 'replace-with-a-long-random-password';
    END IF;
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO educore_app', current_database());
END
$$;
GRANT USAGE ON SCHEMA public TO educore_app;

-- What exists now.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO educore_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO educore_app;

-- What future migrations create, as long as the owner creates it.
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO educore_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO educore_app;

-- No CREATE on the schema: the app role cannot add objects of its own.
REVOKE CREATE ON SCHEMA public FROM educore_app;
