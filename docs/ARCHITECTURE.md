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

- `ScopeToSchool<T>` query filters (`currentUser.SchoolIds.Contains(e.SchoolId)`) on the read side.
  A guardian with children at several schools carries one claim per school; a RegionalAdmin carries
  none, so its scoping lives in the service layer instead. Cross-school repository methods call
  `IgnoreQueryFilters()` explicitly.
- `Data/SchoolScopeInterceptor.cs` on the write side, throwing `ForbiddenException` on a school
  mismatch. Bypassed for StateAdmin and RegionalAdmin — the latter's city ownership is verified in
  the service layer.

Geography (State/City/Zone) is global reference data, not school-scoped. Writes to it are
StateAdmin only.

CSRF is a double-submit cookie (`Security/CsrfProtectionMiddleware.cs`). Rate limiting is 100
requests a minute per authenticated user (falling back to per-IP), with stricter policies on
`/api/auth/login` and password reset.

`Security/AccountStatusCache` (60s) removes the per-request `Users` SELECT that every authenticated
call used to make. `Data/AccountStatusInvalidationInterceptor` evicts on any `User` write, so
revocation stays immediate rather than TTL-bounded.

## Encryption at rest

`Student.PersonalNumber` and `Guardian.NationalId` are encrypted through an EF value converter
(`Security/Encryption/AesFieldEncryptor` — deterministic AES-256-CBC with an HMAC-derived synthetic
IV, wired in `AppDbContext.ConfigureUsers`).

Determinism is required so those columns keep their unique indexes and equality lookups; EF applies
the same converter to the compared value, so `== value` still works. Substring search does not —
`GuardianRepository.SearchAsync` matches national ID exactly.

Needs a 32-byte base64 `Encryption:Key`, supplied like `Jwt:Key` (`Encryption__Key` in `.env`,
never committed). Start-up fails without it, and changing the key makes existing values unreadable.
Rows written before encryption pass through `Decrypt` unchanged and re-encrypt on next save.

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

## Operations

**Observability.** OpenTelemetry metrics on `/metrics` (Prometheus format) and tracing, shipped to
`Observability:OtlpEndpoint` when one is configured and collected in process regardless.
`GlobalExceptionHandler` reports the W3C trace id, which is the id the trace is filed under, rather
than ASP.NET's per-process `TraceIdentifier`.

**HTTPS.** nginx terminates TLS (`nginx.conf.template` in the front-end repo, an envsubst template
so `PUBLIC_ORIGIN` and `HSTS_MAX_AGE` are not baked into the image). `ForceHttpsRedirect` applies
only when the API itself is the TLS endpoint.

**Migrations.** `dotnet EduCore.dll --migrate` is the separate one-shot path, which is what compose
runs. `MigrateOnStart=true` migrates during start-up instead, for hosts with no separate step —
safe on one instance only, since replicas would race. Start-up otherwise refuses to serve against a
schema it does not match.

**CI.** `.github/workflows/ci.yml` runs a Release build with `-warnaserror`, the tests, and a check
that the EF model has no un-scaffolded changes. The migration check distinguishes real drift from a
tooling failure by reading EF's output rather than its exit status.

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

There is no mocking library; tests use hand-written fakes implementing the repository interfaces.
`ListScopingTests.cs` is the reference pattern — fake repositories, a real `MapperConfiguration`
built from `MappingProfile`, and a `FakeCurrentUser`. Follow it for new service tests rather than
introducing Moq.
