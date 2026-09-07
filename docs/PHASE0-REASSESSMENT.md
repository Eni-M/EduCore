# Repository Reassessment — Phase 0

**Against:** `docs/PERFORMANCE_PLAN.md`
**Repos inspected:** `EduCore` @ `f3bf53a` (clean), `EduCoreFront` @ `1222f72` (`feature/ui-revision`, clean)
**Date:** 2026-09-04
**Production code changed:** none.

Per PERFORMANCE_PLAN §25, this document becomes the active plan.

---

## Current architecture

**Backend.** .NET 10 minimal API, single project. ~28 repositories, ~35 services registered
in `Program.cs`. EF Core 10 + Npgsql, AutoMapper, FluentValidation. Auth is a JWT (60 min)
carried in a cookie, with a CSRF middleware, school-scoped EF query filters, a
`SchoolScopeInterceptor`, an `AuditSaveChangesInterceptor`, and an `AccountStatusEndpointFilter`
on the `/api` group. Statistics are a repository/service pair per domain over a shared
`StatisticsRepositoryBase`.

**Database.** Neon PostgreSQL 16, **free plan**, autoscaling **0.25–2 CU**, 512 MB branch
limit, `eu-central-1`. The configured endpoint is the **`-pooler`** host (transaction pooling).
Local development connects to the branch named `production`.

**Deployment.** `compose.yaml`: `db` (local postgres:16) → `api` → `web`. Migrations run
from `Program.cs` at boot. No migration job, no container health check.

**No CI exists in either repository.** There is no `.github` directory in `EduCore` or in
`EduCoreFront`.

**Frontend.** React 18 + Vite 6 + react-router-dom. No other runtime dependencies. Route
code-splitting is in place; main bundle 187 KB (matches the plan's §24 baseline). A shared
`api()` client with in-flight GET coalescing. No data-fetching library, no client cache.

---

## Existing relevant optimizations (confirmed present, do not redo)

- Class summaries, at-risk and excessive-absence lists are genuinely SQL-paged with
  deterministic tie-breaking (`AcademicStatisticsRepository`, `AttendanceStatisticsRepository`,
  `StudentStatisticsRepository.GetByClassPagedAsync`).
- `Math.Round` parity between paged and full-list paths, with the reasoning in comments.
- `EnrollmentStatisticsRepository.GetCapacityAsync` already projects and groups in SQL.
- Digest queries are projected/joined in SQL, not N+1.
- Frontend: route splitting, in-flight GET dedup, StrictMode double-fetch neutralized,
  non-blocking font loading.
- `ClassStatisticsServiceTests` retained as the permanent equivalence coverage.
- **Zero explicit transactions in production code** (`BeginTransaction`/`TransactionScope`:
  none outside the temp test file). §7.1's transaction inventory is therefore already complete.
- The single `Task.WhenAll` (`ScheduleDigestService.cs:242`) is over email sends behind a
  semaphore, not over a shared `DbContext`. §3.8 is not violated.

---

## Assumptions from the plan — CONFIRMED

| Ref | Claim | Evidence |
|---|---|---|
| 4.1 | Login limiter uses one fixed `"login"` partition | `Program.cs:281` |
| 4.1.1 | `KnownNetworks`/`KnownProxies` cleared | `Program.cs:320-321` |
| 4.2 | `/api/auth/*` sits outside the filtered `/api` group | `Program.cs:342` vs `:344-347`; `AuthEndpoints.cs:11` |
| 4.3 | `MigrateAsync()` at boot | `Program.cs:303-304` |
| 4.3.2 | No Docker `HEALTHCHECK` | `Dockerfile` |
| 4.3.3 | `web depends_on: [api]` without `service_healthy` | `compose.yaml:47-48` |
| 7.2/7.3/7.4 | No execution strategy, no KeepAlive, no command timeout | `Program.cs:51-55`, `.env` |
| 8 | No caching of any kind registered | `Program.cs` |
| 9.4 | No response compression | `Program.cs` |
| 10.1 | AuditLogs 170,006 rows / 22 MB / `idx_scan = 0` | `pg_stat_user_tables` |
| 10.3 | Platform overview loops schools | `StatisticsOverviewService.cs:66-86` |
| 10.4 | Totals fan out into many statements | students 7, attendance ≤6, academic 6 |
| 10.5 | Distribution materializes every mark | `AcademicStatisticsRepository.cs:167` |
| 10.6 | `GetByAge` materializes every birth date | `StudentStatisticsRepository.cs:135` |
| 10.7 | Trend materializes all enrollments, loops months in C# | `StudentStatisticsRepository.cs:161-181` |
| 10.8 | `/by-student` pages **in memory only** | `AttendanceStatisticsEndpoints.cs:36-44` |
| 10.10 | Teacher coverage nests correlated counts | `TeacherStatisticsRepository.cs:118-121` |
| 10.11 | New arrivals uses correlated `ORDER BY … LIMIT 1` | `StudentStatisticsRepository.cs:41-51` |
| 11.4 | Unbounded recipient list, no per-recipient isolation | `ScheduleDigestService.cs:38,72` |
| 12.1 | No reference-data TTL cache | every picker refetches on mount |
| 12.2 | Filter changes commit immediately | `StatisticsPage.tsx:171-176` |
| 12.3 | Page blanks to `Loading...` on every reload | `StatisticsPage.tsx:245-247` |
| 12.4 | No chart memoization | `src/components/charts/` |
| 12.6 | `pageSize=1` probe requests | `DirectorateDashboard.tsx:27,29`; `StateAdminDashboard.tsx:48` |

Two of these are worse than the plan describes:

- **4.1.1 is a live security defect, not just a precondition.** `UseForwardedHeaders` with
  cleared trust rewrites `Connection.RemoteIpAddress` from an attacker-supplied
  `X-Forwarded-For`. The **global** limiter (`Program.cs:266`) partitions on exactly that
  value, so an unauthenticated client bypasses the 100/min global limit today by rotating a
  header. Per-IP login partitioning would inherit the same hole.
- **10.11 runs the correlated subquery twice per student row** — once in `Where`, once in the
  `CountAsync` predicate.

---

## Assumptions stale or incorrect

**1. §24 "temporary files deleted" — false.**
`EduCore.Tests/TempWave2VerificationTests.cs` (553 lines) is committed. It runs against the
**live `production` Neon branch** and writes evidence to a hard-coded
`C:\Users\enim\AppData\Local\Temp\opencode\perf-wave2.txt`. Nobody else can run the suite,
and the suite cannot run in CI. This violates the plan's own §3.4.

**2. §10.1's proposed index does not fit the measured query.**
`AuditLogRepository.GetPagedAsync` has *optional* `UserId` and *optional* date filters, and
always sorts `Timestamp DESC, Id`. The dominant case is the unfiltered admin listing, which a
`("UserId", "Timestamp" DESC)` index cannot serve — `UserId` is unconstrained. Correct DDL:

```sql
CREATE INDEX CONCURRENTLY "IX_AuditLogs_Timestamp_Id" ON "AuditLogs" ("Timestamp" DESC, "Id");
```

The plan's index remains a *secondary* candidate for the filtered-by-user path only.

**3. §10.14's drop list is partly impossible and wholly unproven.**

- `AK_Enrollments_Id_ClassId` and `AK_Classes_Id_AcademicYearId` are **UNIQUE constraints
  backing 2 and 1 foreign keys** respectively (`pg_constraint.contype = 'u'`, verified).
  `DROP INDEX` will fail; dropping the constraint would delete the composite foreign keys
  that enforce the school-scoping integrity model. **REJECT permanently.**
- The remaining three are droppable, but `idx_scan = 0` is meaningless on this database:
  `PK_AuditLogs`, `PK_Notices`, `PK_GuardianStudents`, `PK_SchoolingHistories` and *every*
  index on `Marks` also read 0, because this database has only ever been seeded and browsed.
  This is exactly the trap §10.14 warns about. **DEFER.**

**4. §3.6, §5.4 and §1.3 assume CI exists. It does not.**
Deterministic SQL budgets, the CI Postgres service, and build/type gates all require CI to be
built first. Until then the plan's verification standard cannot be enforced by anything but
manual runs.

**5. §14 treats "single application instance" as the only scaling fact.**
The larger constraint is the database: Neon **free plan**, 0.25–2 CU, scale-to-zero. Cold
start after idle is a real availability factor the plan never mentions, and it constrains two
of its own recommendations — §4.3.2 (a DB-touching health check would pin the compute awake
and consume the free-tier compute quota) and §7.3 (KeepAlive interacts with suspend).

**6. §11 assumes emails are actually sent.**
`IEmailSender` is registered as `LoggingEmailSender`, which writes a log line
(`Program.cs:176`). There is one replica. A lease table with fencing tokens, heartbeats and a
two-replica kill test would protect duplicate **log lines**. The real defect in this area is
different and unaddressed by a lease: `ScheduledEmailBackgroundService` holds its schedule in
process memory, so **any restart between `RunHourUtc` and midnight silently skips that day's
digest**, with nothing recording that it was missed.

**7. §10.9 (capacity materialization) — ALREADY DONE.**
`EnrollmentStatisticsRepository.cs:70-88` already projects and groups in SQL.

---

## Newly discovered bottlenecks

**A. Statistics scope resolution is re-queried on every service call.**
`StatisticsScopeResolver.ResolveAsync` → `StatisticsPeriodResolver.ResolveAsync` → an
academic-year or grading-period database lookup. *Every* statistics service method calls it
independently. The school overview resolves the identical scope 6× per request; the platform
overview does it ~5× *per school*. The result is a pure function of (filter, current user,
today) — memoizable in a scoped field. Not in the plan; the cheapest real backend win.

**B. The school overview computes student totals twice.**
`TeacherStatisticsService.GetSummaryAsync` calls `students.GetTotalsAsync` (7 statements),
which `StatisticsOverviewService.BuildAsync` has already called on the same context.

**C. `AccountStatusEndpointFilter` issues a `Users` SELECT on every `/api` request.**
`Users.idx_scan = 25,271` is largely this one filter. It is correct and must stay, but it is
the highest-frequency query in the application and the strongest case for §8's short-TTL
cache — subject to §8.3: cache the profile, never the authorization decision.

**D. The global rate limiter is bypassable** (see 4.1.1 above). This promotes §4.1.1 above
§4.1 in priority and makes it a security item, not an availability item.

**E. The dataset cannot support most of Phase 4's evidence standard.**

| Table | Live rows |
|---|---|
| AuditLogs | 170,006 |
| Enrollments | 5,000 |
| Students | 5,008 |
| Classes | 3,840 |
| Teachers | 700 |
| Schools | 80 |
| **ClassSubjectTeachers** | **528** |
| **Marks** | **92** |
| **ScheduleEntries** | **3** |
| **DiaryEntries** | **2** |
| **Attendances** | **1** |

Every attendance and academic statistic reads near-empty tables. `EXPLAIN ANALYZE` against
1 attendance row and 92 marks proves nothing, and §3.5's requirement cannot be met for those
items. A representative `Attendances` / `Marks` / `DiaryEntries` / `ScheduleEntries` seed is a
**prerequisite**, not an optional extra, for §10.4 (attendance/academic half), §10.5, §10.10
and §10.15.

**F. The test suite cannot be run without the live production Neon branch** (consequence of
stale item 1). No isolated test database exists.

---

## Measured evidence

**AuditLogs, unfiltered page 1 — the actual query shape** (`EXPLAIN (ANALYZE, BUFFERS)`,
`ORDER BY "Timestamp" DESC, "Id" LIMIT 20`, left-joined to `Users` for the display name):

| | Rows touched | Plan | Time |
|---|---|---|---|
| cold | 170,006 | Parallel Seq Scan → top-N heapsort | **1177 ms**, 2098 buffers read |
| warm | 170,006 + 9,849 `Users` hashed | Hash Left Join → Sort → Limit | **213 ms** |

To return 20 rows. No index is used. This single table accounts for the plan's "≈8.16M
sequential rows" (170k rows × 72 recorded sequential scans), and it is the one index
candidate in the whole document with measured evidence behind it.

**Platform overview — read from code, not yet instrumented.** 80 schools ×
(students 7 + teachers ~9 + attendance ≤6 + academic 6 + classes N + ~5 scope resolutions)
≈ **2,500+ sequential statements**, each a round trip to `eu-central-1`. Round-trip latency,
not query cost, dominates. This is almost certainly the worst endpoint in the application.
An end-to-end number needs the `DbCommandInterceptor` from §3.6, which needs CI.

---

## Recommended execution order

Rationale: security → availability → *the prerequisites that make verification possible at
all* → the one index with evidence → statement-count reductions (measurable regardless of row
counts) → frontend request reduction (largest user-visible win, zero DB risk) → everything
that is gated on realistic data.

| # | Work | Plan ref | Verdict |
|---|---|---|---|
| 0 | Delete `TempWave2VerificationTests.cs` | §3.4, §24 | **NEW** — pure deletion, unblocks the suite |
| 1 | Forwarded-header trust **+** rate-limit partitioning, as one change | §4.1 + §4.1.1 | **MERGE** — per-IP partitioning is unsafe to ship without the trust fix |
| 2 | `/me` revocation scoping | §4.2 | **MODIFY** — split the filter; deactivation applies to `/me`, must-change-password must not, or password rotation breaks |
| 3 | CI: `dotnet build`, `dotnet run --project EduCore.Tests`, `tsc -b && vite build`, Postgres service | §5.4, §3.6 | **PROMOTE** — nothing after this is verifiable to standard without it |
| 4 | Migration job, Docker `HEALTHCHECK`, `web depends_on api: service_healthy` | §4.3 | **KEEP** — keep `/healthz` DB-free (Neon scale-to-zero) |
| 5 | AuditLogs index, **corrected DDL** | §10.1 | **MODIFY** — `("Timestamp" DESC, "Id")` |
| 6 | Statistics filter draft **+** stale-while-reload, as one PR | §12.2 + §12.3 | **MERGE** — one defect; largest visible win, no DB risk |
| 7 | Frontend reference-data TTL cache | §12.1 | **KEEP**, then re-evaluate §8.1 — it may become unnecessary |
| 8 | Memoize scope resolution; drop the duplicate student totals | new A + B | **NEW** — pure round-trip removal, no SQL rewrite, no equivalence risk |
| 9 | Platform-overview fan-out → grouped queries | §10.3 | **KEEP** — measurable today (80 schools, 5k enrollments) |
| 10 | Student totals consolidation | §10.4 (students) | **KEEP** — real data behind it |
| 11 | Representative `Attendances`/`Marks`/`DiaryEntries` seed | new E | **NEW PREREQUISITE** |
| 12 | `/by-student` SQL paging | §10.8 | **KEEP** — the `IQueryable` already exists |
| 13 | Execution strategy | §7.2 | **SIMPLIFY** — §7.1's inventory is already complete: zero explicit transactions exist |
| 14 | Digest: bounded batches, per-recipient `try/catch`, `last_success_at` for missed-day catch-up | §11 reduced | **MODIFY** — see stale item 6 |

Everything from 11 onward that depends on attendance/academic volume is **blocked on 11**.

---

## Items to reject or defer

**REJECT**

- §10.14 drops of `AK_Enrollments_Id_ClassId` and `AK_Classes_Id_AcademicYearId` — impossible
  as written, and would delete foreign keys.
- §10.9 — already implemented.

**DEFER**

- §10.14's remaining three drops — no workload evidence exists, and `idx_scan = 0` on this
  database is an artifact of seeding, not of uselessness.
- §11 lease / fencing / heartbeat / two-replica test — no email transport, one replica.
- §9 selective output caching and §9.4 compression — measure after items 6–9; likely
  unnecessary once the request count falls.
- §12.4 / §12.5 chart memoization — per §23's own rule, do not optimize React rendering
  before reducing unnecessary requests. Items 6 and 7 remove the requests; re-measure after.
- §8.1 backend reference-data cache — pending the outcome of item 7.

**BLOCKED on item 11 (data)**

§10.5, §10.10, §10.15, and the attendance/academic half of §10.4.

---

## First implementation task

**Item 0**, then **item 1**.

Item 0 is a single file deletion that removes a production-database dependency from the test
suite and satisfies a rule the plan already set. Item 1 is the only finding in this pass that
is a live security defect rather than a performance or availability one.

Item 1's scope, concretely:

- Replace the cleared `KnownNetworks` / `KnownProxies` with the actual edge proxy or network
  (this also resolves the `ASPDEPR005` build warning — move to `KnownIPNetworks`).
- Partition `LoginPolicy` by the normalized client IP, keeping the token bucket at 5/min,
  `QueueLimit = 0`.
- Leave the global limiter's shape alone; it becomes correct once trust is correct.
- Tests per §20: per-IP isolation, 6th request → 429, truthful `Retry-After`, trusted proxy
  honoured, forged forwarding header ignored, `/healthz` never throttled.
