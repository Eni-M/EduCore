# EduCore — architecture

A .NET 10 minimal-API school management system backed by PostgreSQL. Feature branches off
`main`; `main` is what Render deploys.

For running it locally see the [README](../README.md) and [DEV-SETUP.md](DEV-SETUP.md).

## Layout

| Folder | Holds |
| --- | --- |
| `Models/`, `Data/` | EF entities, `AppDbContext` configuration, interceptors, `Migrations/` |
| `DTOs/<Area>/` | Request and response shapes |
| `Validation/<Area>/` | FluentValidation validators |
| `Mapping/` | AutoMapper profile. `CreateWriteMap<TSource, TDestination>()` builds a convention map restricted to the destination's writable members |
| `Services/` | Business rules, plus `Interfaces/`, `Statistics/`, `Assignment/`, `Storage/`, `Email/`, `Account/` |
| `Repositories/` | Data access, plus `Interfaces/` and `Statistics/` |
| `Endpoints/` | Minimal-API groups, wired up with `MapXEndpoints()` in `Program.cs` |
| `Security/` | Policies, claims, the current-user abstraction, CSRF, field encryption |
| `Observability/` | OpenTelemetry metrics and tracing |

## Roles and access

`Enums/UserRole.cs`: `Student=0, Guardian=1, Teacher=2, Directorate=3, RegionalAdmin=4,
StateAdmin=5`. The integer values are load-bearing — the `RoleHierarchyAndGuardians` migration
remapped existing rows and the seed writes the new values directly.

The tiers run **StateAdmin → RegionalAdmin → Directorate → Teacher/Student**, with a read-only
**Guardian** alongside. Each tier manages the one below it: StateAdmin creates RegionalAdmins
(`/api/regional-admins`, `/api/state-admins`), a RegionalAdmin creates Directorates in its city
(`/api/directorates`), and a Directorate creates its own school's Teachers, Students and Guardians
(`/api/guardians`).

Profile entities mirror each other: `Directorate`(UserId, SchoolId, `ISchoolScoped`),
`RegionalAdmin`(UserId, CityId), `StateAdmin`(UserId, StateId) and `Guardian`(UserId, PhoneNumber)
plus the `GuardianStudent` join. Each has its own repository, service and `/api/*` group.

`Security/Policies.cs` names the role sets: `Directorate`, `RegionalAdmin`, `StateAdmin`,
`Guardian`, `RegionalOrState`, `DirectorateOrTeacher`, `ManageAccounts`, `StatisticsViewer`,
`AnySchoolUser` (Directorate/Teacher/Student) and `StudentReadable` (those plus Guardian).

`ICurrentUser` exposes the claims issued at login by `UserService.BuildAuthResponseAsync` —
`school_id`, `teacher_id`, `student_id`, `city_id`, `state_id`, `guardian_id` — each present only
for the roles that have one. A guardian with children at a single school also gets `school_id`, so
the existing student-read paths work unchanged.

### School isolation

Two mechanisms, one for reads and one for writes:

- `ScopeToSchool<T>` query filters (`currentUser.SchoolIds.Contains(e.SchoolId)`) on the read side,
  registered as the EF Core 10 *named* filter `QueryFilters.School`. A guardian with children at
  several schools carries one claim per school; a RegionalAdmin carries none, so its scoping lives
  in `AccessGuard.EnsureCanManageSchoolAsync`. Cross-school reads bypass the filter by name,
  `IgnoreQueryFilters(QueryFilters.SchoolOnly)`, and only from the files allow-listed in
  `EduCore.Tests/BoundaryTests.cs` — as are the `ExecuteUpdate`/`ExecuteDelete` sites, which skip
  the save interceptors.
- `Data/SchoolScopeInterceptor.cs` on the write side, throwing `ForbiddenException` (403) on a
  school mismatch. Bypassed for StateAdmin and RegionalAdmin — their territory is checked by
  `AccessGuard.EnsureCanManageSchoolAsync`.
- The database itself: child rows reference their parent through composite `(Id, SchoolId)` keys,
  so Postgres refuses a link to another school's row.

Every "may this caller do X to Y" decision is a named method on `AccessGuard`, covered by the
role × relationship matrix in `AccessGuardRoleMatrixTests`.

Geography (State/City/Zone) is global reference data, not school-scoped. Writes to it are
StateAdmin only.

CSRF is a double-submit cookie (`Security/CsrfProtectionMiddleware.cs`). Rate limiting is 100
requests a minute per authenticated user (falling back to per-IP), with stricter policies on
`/api/auth/login` and password reset. Behind a PaaS proxy whose addresses cannot be listed, set
`ForwardedHeaders:TrustedHops` (Vercel → Render is 2) so the per-IP limits see real clients.
Sign-in also locks an email for 15 minutes after 10 failures, whatever the address.

**Sessions.** A token is checked against the account on every request (`AccountStatus.Admits`):
the account must be live, and the token's `iat` must not be older than `User.SessionsValidFrom`.
Sign-out, password change, self-service reset and admin reset move that cutoff, so every earlier
token — including a copied cookie — stops working. Signing out therefore signs out every device.
Changing the sign-in email or switching two-step sign-in off needs the current password, and an
email change is reported to the old address.

**Passwords.** NIST SP 800-63B-4: at least 15 characters, no composition rules, and a check
against common and predictable choices (`Validation/PasswordBlocklist.cs`). PBKDF2-HMAC-SHA512 at
220,000 iterations; older hashes upgrade at the next sign-in.

**Authenticator apps.** Optional TOTP (`Security/Totp.cs`) in place of emailed codes. Adding one
needs the current password, adding or removing one emails the account, and the office's password
reset removes it (the recovery route for a lost phone). Wrong codes, app or emailed, are capped
per user, five per 15 minutes (`AttemptThrottle`), not only per challenge; so is the current
password asked for again on the account screens.

**Audit.** Every save writes `AuditLog` rows with the school and the changed fields (old and new
values for marks; names only for anything encrypted). `IAuditTrail` adds what a save cannot see:
sign-in successes and failures, rejected two-step codes, and every read of a medical record or of
a student's or guardian's identifiers.

`Security/AccountStatusCache` (60s) removes the per-request `Users` SELECT that every authenticated
call used to make. `Data/AccountStatusInvalidationInterceptor` evicts on any `User` write, so
revocation stays immediate rather than TTL-bounded.

## Encryption at rest

`Student.PersonalNumber` and `Guardian.NationalId` are encrypted through an EF value converter
(`Security/Encryption/AesFieldEncryptor` — deterministic AES-256-CBC with an HMAC-derived synthetic
IV, wired in `AppDbContext.ConfigureUsers`). Because the IV is the MAC of the plaintext, decryption
recomputes it and rejects a tampered value. Every `StudentMedicalRecord` text field is sealed with
randomised AES-256-GCM instead: health data is never searched, so equal values need not look equal.

The key id is part of every envelope (`enc:v1:…`, `enc-gcm:v1:…`), so keys rotate: put the new key
in `Encryption:Key` with a new `Encryption:KeyId`, move the old one to
`Encryption:PreviousKeys:v1`, and deploy. Start-up (`Data/FieldReencryption`) rewrites every value
under the new key and encrypts any legacy plaintext; after that the old key can go. EF caches one
model per encryptor (`EncryptorModelCacheKeyFactory`), since the converters close over it.

Determinism is required so those columns keep their unique indexes and equality lookups; EF applies
the same converter to the compared value, so `== value` still works. Substring search does not —
`GuardianRepository.SearchAsync` matches national ID exactly.

Needs a 32-byte base64 `Encryption:Key`, supplied like `Jwt:Key` (`Encryption__Key` in `.env`,
never committed). Start-up fails without it. Replacing the key without listing the old one under
`PreviousKeys` makes existing values unreadable.

## Domain

**Geography.** State → City → Zone, managed under `/api/states`, `/api/cities`, `/api/zones`.
`School.CityId`/`ZoneId` and `Student.CityId` are real foreign keys with navigation properties;
there is no free-text city or zone anywhere.

**Guardians.** A login entity with a many-to-many to students via `GuardianStudent`, created by a
Directorate for students in its school. `/api/guardian/children` is the portal entry point.
`AccessGuard.EnsureCanViewStudentAsync`/`EnsureCanViewClassAsync` have Guardian branches, and a
child's marks, schedule, class and teachers reuse the normal student and class endpoints gated by
`StudentReadable`.

**Enrolment.** `EnrollmentService` allows one active enrolment per student. Withdrawing sets both
`LeftAt` and `ExitReason`, and both are required — that is what lets statistics tell a transfer
apart from a dropout.

**Teaching.** `ClassSubjectTeacherService` requires a `TeacherSubject` qualification row, enforces
one active teacher per class + subject, and checks the grade band against `School.Type`
(`Services/GradeBands.cs`: Elementary 1-5, JuniorHigh 6-9, HighSchool 10-12) — the same check
`ClassService.CreateAsync`/`UpdateAsync` makes.

Elementary classes are special-cased upstream rather than in `ClassSubjectTeacherService`:
`Services/Assignment/ElementaryAssignmentSync.cs` grants a blanket qualification and creates or
ends `ClassSubjectTeacher` rows across every subject whenever an elementary homeroom teacher
changes, so a homeroom teacher already holds the qualification before anything tries to block them.
`TeacherService.AddSubjectAsync` caps qualifications at `Assignment:MaxSubjectsPerTeacher`
(default 2) for every school type except Elementary.

**Cross-school transcripts.** `StudentAcademicRecord` and `StudentAcademicRecordSubject` are the
only student-owned tables that are *not* `ISchoolScoped`, and are keyed on `StudentId` alone rather
than the composite `(StudentId, SchoolId)` — that composite ties a row to the student's current
school and would reject exactly the rows worth keeping. School, year and subject names are
denormalised so a transcript survives a rename or deletion. Access is two-step: `IAccessGuard`
first, then the record. `RebuildAsync` recompiles from live tables and is idempotent.

## Statistics

`Services/Statistics/*`, `Repositories/Statistics/*`, `DTOs/Statistics/*`.

`StatisticsFilter` (the `[AsParameters]` query object) resolves to a `StatisticsQuery` through
`StatisticsScopeResolver`, which forces tier defaults from claims: RegionalAdmin gets its own
`CityId` and cross-school reads, StateAdmin its own state via `StatisticsQuery.SchoolStateId`,
Directorate its own `SchoolId`. Every statistics repository consumes the query through
`StatisticsRepositoryBase.Scope<T>()`, so city/zone/school-location filtering lives in one place.
`StateId` (student origin, as opposed to school location) is applied in
`StudentStatisticsRepository` against `Student.City.StateId`.

`/statistics` is `StatisticsViewer`; `/demographics` and `/audit-logs` are StateAdmin only.
Demographics groups by the real `School.City`/`School.Zone` navigation properties, not strings.

**Snapshots.** `Models/SchoolStatisticsSnapshot` holds pre-computed per-school aggregates.
`StatisticsSnapshotService` reads them and recomputes on the read that finds a row stale.
Staleness is `SnapshotStaleness`: a write count bumped by `Data/StatisticsInvalidationInterceptor`
for every write to the eight contributing tables (with a threshold that scales with school size),
or `MaximumAgeMinutes`. Only `StatisticsOverviewService` consults it, and only for unfiltered
single-school queries (`IsSnapshotable`); every breakdown endpoint and every filtered query
computes live. Raw aggregates are stored, never presented figures, so rates and privacy
suppression stay derived at read time.

## Dashboards

`GET /api/dashboard` returns one shape for all six roles; the server decides what each sees and the
client renders it without branching. It is composed from existing services rather than new queries,
so a dashboard figure and the page it links to cannot disagree. `Services/DashboardFormat` holds
the pure judgement calls and checks `Measure.Suppressed` before `Value`, so a privacy-withheld
statistic cannot reappear on a screen.

## Files

`Services/Storage/IFileStorage` is the seam between the features that keep files (student
documents, assignment attachments, submissions) and where those files live. `Storage:Provider`
selects the backend: `Local` writes under `Storage:LocalRoot`, `S3` writes to `Storage:S3Bucket`
in `Storage:S3Region`. Local disk is fine for development but is lost when a container restarts,
so anything deployed should be on S3. AWS credentials come from the ambient chain (environment
variables, or the instance role) and are never configuration.

`StorageOptions.EnsureAccepted` is the single upload rule — size cap and content-type allowlist —
applied by every upload path.

## Email

`IEmailSender` is either `SmtpEmailSender` (MailKit) or `LoggingEmailSender`, chosen by
`Email:Provider`; start-up warns if a non-Development environment is still on `Log`.

`ScheduledEmailBackgroundService` runs daily at `ScheduledEmail:RunHourUtc` — schedule digests
daily, week-ahead and `GuardianDigestService` on Sundays. Account mail (welcome, password changed)
is queued by `Data/AccountMailInterceptor` and sent off the request path by
`AccountMailBackgroundService`. `User.EmailDigestsOptOut` covers every digest and is set through
`PUT /api/auth/me/email-digests`; transactional mail is deliberately not covered by it.

Password reset is `/api/auth/forgot-password` + `/reset-password`, rate-limited, with tokens hashed
at rest and single-use. `forgot-password` always returns 204, so it cannot be used to test which
addresses hold accounts.

## Configuration

Every tunable value lives in `appsettings.json`, and nothing operational is a literal in the
code. Each section binds to an options class beside the code that reads it, and each is checked
at start-up — through a validator, or, for the few read before the container exists (`Database`,
`RateLimiting`, the `Jwt` issuer, audience and lifetimes, `ScheduledEmail:RunHourUtc`), through
`Validate()` / `StartupChecks` called from `Program.cs` — so a bad value stops the application
rather than surfacing as odd behaviour later.

| Section | What it governs |
| --- | --- |
| `Jwt`, `Session` | Token lifetime and clock skew; cookie names and flags |
| `TwoFactor` | Code lifetime, length, attempt cap, and which roles cannot opt out |
| `PasswordReset` | Reset-link lifetime and where the link points |
| `Caching` | Public-statistics and account-status cache windows |
| `Pagination` | Default and maximum page size, and the tighter audit-log cap |
| `RateLimiting` | The global, sign-in and password-reset token buckets, and `Retry-After` |
| `Database` | Connection retry count and delay, command timeout |
| `Privacy`, `Statistics`, `StatisticsSnapshot` | Small-cell threshold, maximum query window, snapshot staleness |
| `Grading`, `AcademicCalendar`, `Assignment` | Mark weights and pass marks, term boundaries, qualification cap |
| `Email`, `ScheduledEmail` | Transport, send concurrency, queue capacity, digest run hour |
| `Storage` | Local or S3, size cap, content-type allowlist |
| `Observability` | Metrics and tracing |
| `SuperAdmin` | First-run administrator, its state name and password floor |

Two values are deliberately *not* configurable, because they are facts rather than choices:
the AES block size in `AesFieldEncryptor`, and the seeder's fallback constants, which are read
before options validation runs.

Anything here can be overridden per environment the usual way, including as `Section__Key`
environment variables — which is how compose supplies the secrets.

## Operations

**Observability.** OpenTelemetry metrics on `/metrics` (Prometheus format) and tracing, shipped to
`Observability:OtlpEndpoint` when one is configured and collected in process regardless.
`GlobalExceptionHandler` reports the W3C trace id, which is the id the trace is filed under, rather
than ASP.NET's per-process `TraceIdentifier`, and writes errors as RFC 9457
`application/problem+json`. With an OTLP endpoint set, logs are exported too, carrying the trace id.
`/metrics` requires a StateAdmin session unless `Observability:MetricsRequireAuthentication` is
turned off (compose does, for its private network).

**HTTPS.** nginx terminates TLS (`nginx.conf.template` in the front-end repo, an envsubst template
so `PUBLIC_ORIGIN` and `HSTS_MAX_AGE` are not baked into the image). `ForceHttpsRedirect` applies
only when the API itself is the TLS endpoint.

**Migrations.** `dotnet EduCore.dll --migrate` is the separate one-shot path, which is what compose
runs. `MigrateOnStart=true` migrates during start-up instead, for hosts with no separate step.
EF Core 9+ takes a database-wide lock while migrating, so replicas no longer race. Both paths
migrate through `ConnectionStrings:Migrations` when it is set -- the schema owner -- so the API's own
login (`ConnectionStrings:Default`) can be the data-only `educore_app` role from
`docs/db-roles.sql`: it reads and writes rows, and cannot create, alter or drop anything. Compose
sets this up on a fresh volume (`docker/postgres/10-app-role.sh`); on Neon, run the script once
as the owner. Start-up otherwise refuses to
serve against a schema it does not match. On Neon, run migrations over the direct (non `-pooler`)
endpoint; if the app connects through the pooler, add `No Reset On Close=true` to its connection
string.

**Scale-out checklist.** These assume one API instance. Before running two:

- `NotificationSignals` wakes bell streams in-process only — move to Postgres `LISTEN/NOTIFY` over
  a dedicated direct connection (Neon's pooler does not carry it).
- `AccountMailQueue` is an in-memory channel — a crash loses queued mail; use a durable outbox.
- The rate-limit partitions, the per-email sign-in lockout and `AccountStatusCache` are
  per-process — move them to a shared store (Redis, or the database).
- `ScheduledEmailBackgroundService` would send each digest once per instance — elect one sender.
- `MigrateOnStart` is safe (EF's lock), but a separate `--migrate` step with its own DDL role is
  the better shape.

**CI.** `.github/workflows/ci.yml` runs a Release build with `-warnaserror` (which also fails on a
vulnerable NuGet package), the tests, and a check that the EF model has no un-scaffolded changes.
The migration check distinguishes real drift from a tooling failure by reading EF's output rather
than its exit status. The workflow token is read-only, actions are pinned by commit SHA, and
Dependabot opens weekly update PRs.

## Seed data

`PlatformOwnerSeeder` runs on start-up and creates the top StateAdmin user and its `State` (default
`Kosova`, override with `SuperAdmin__StateName`) from `SuperAdmin__Email` / `SuperAdmin__Password`.

`Data/Seeds/00-full-dataset.sql` is the development dataset: four schools (one per type, one
deactivated) each with a Directorate account, one RegionalAdmin per active city, and three
Guardians. It reuses the state the seeder created. Every seeded account shares the StateAdmin's
password hash — whatever `SuperAdmin__Password` was at seed time, one-way and not recoverable; you
reset it by reseeding, not through any API. The script is not idempotent, so only run it against an
otherwise-empty database. Rebuild steps are in `Data/Seeds/README.md`.

## Testing

`EduCore.Tests` (xUnit v3). Run it with:

```bash
dotnet run --project EduCore.Tests/EduCore.Tests.csproj
```

not `dotnet test`. The test project is **not** in `EduCore.slnx`, so a bare `dotnet restore` or
`dotnet build` at the root skips it — restore and build it by path.

The integration tests in `EduCore.Tests/Integration` host the real app in memory
(`WebApplicationFactory`) against PostgreSQL in a container (Testcontainers), so **they need
Docker**. They cover what fakes cannot: migrations on a fresh database, the query filters, the save
interceptor, policies, CSRF, session cut-off, encryption at rest and auditing.

There is no mocking library; tests use hand-written fakes implementing the repository interfaces.
`ListScopingTests.cs` is the reference pattern — fake repositories, a real `MapperConfiguration`
built from `MappingProfile`, and a `FakeCurrentUser`. Follow it for new service tests rather than
introducing Moq.
