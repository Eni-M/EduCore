# EduCore Performance, Availability & Reliability Engineering Plan

**Status:** Approved as the starting execution framework
**Primary rule:** **Do not implement this document mechanically. Inspect the repository and architecture first, challenge the assumptions, then revise the plan where evidence warrants it.**

**Scope**

Backend:

* Repository: `EduCore`
* .NET 10
* ASP.NET Core
* EF Core
* Npgsql
* Neon PostgreSQL 16
* Docker / Docker Compose

Frontend:

* Repository: `EduCoreFront`
* React 18
* Vite 6
* branch: `feature/ui-revision`

---

# 0. PURPOSE

This document is an engineering plan for improving EduCore's:

* availability;
* resilience;
* database/query efficiency;
* request efficiency;
* authentication correctness;
* cache efficiency;
* background-job durability;
* frontend responsiveness;
* observability;
* operational safety.

It is **not** a fixed list of mandatory code edits.

The intended workflow is:

```text
PLAN
  ↓
INSPECT CURRENT REPOSITORY
  ↓
VERIFY ASSUMPTIONS
  ↓
MEASURE CURRENT BEHAVIOR
  ↓
CHALLENGE PROPOSED SOLUTION
  ↓
REVISE PLAN
  ↓
IMPLEMENT
  ↓
VERIFY CORRECTNESS
  ↓
MEASURE RESULT
  ↓
KEEP / REWORK / REJECT
```

The agent is expected to improve this plan when the actual codebase suggests a better approach.

---

# 1. MANDATORY AGENT BEHAVIOR

## 1.1 Treat this document as a set of hypotheses

Every recommendation in this document has one of these states:

```text
CONFIRMED
LIKELY
HYPOTHESIS
DEFERRED
OPTIONAL
```

The agent must determine which state applies after inspecting the current repository.

Do not assume that:

* file paths still match;
* line numbers are current;
* an optimization is still missing;
* the database state is unchanged;
* deployment architecture is unchanged;
* a proposed technology is still the best choice.

---

# 1.2 The agent has authority to improve the plan

The agent may:

* keep a recommendation;
* modify it;
* split it;
* merge it;
* reorder it;
* defer it;
* reject it;
* replace it;
* introduce a better solution not mentioned here.

The agent should **not** preserve an inferior design merely because it appears in this document.

---

# 1.3 However, the agent may not weaken the engineering standard

The agent may change **what** is implemented.

It may not casually weaken **how correctness is proven**.

The following remain mandatory unless the agent can demonstrate an even stronger verification method:

```text
query equivalence
deterministic CI budgets
EXPLAIN ANALYZE for SQL rewrites
authorization verification
rollback strategy
full relevant test suite
build/type checks
```

---

# 2. PHASE 0 — REPOSITORY & ARCHITECTURE REASSESSMENT

**No production code changes before this phase is complete.**

The purpose is to compare this document against reality.

---

## 2.1 Backend inspection

Inspect at minimum:

```text
Program.cs

authentication
authorization policies
JWT configuration
rate limiting
forwarded headers
middleware ordering

endpoint groups
AuthEndpoints
statistics endpoints

DbContext
DbContextFactory if present
Npgsql configuration
connection string
pooling / pooler configuration
execution strategy
command timeout configuration
transaction usage

repositories
services
statistics services

exception handling
logging
diagnostics
health checks

background services / jobs

Dockerfile
compose.yaml
deployment configuration
CI workflows

EF migrations
model snapshot
```

Search by symbols/behavior instead of trusting the line numbers in this document.

---

## 2.2 Frontend inspection

Inspect:

```text
api client
request deduplication
request caching
reference-data fetches
statistics pages
filter components
chart components
React memoization
useMemo / useCallback usage
loading-state architecture
route splitting
Vite configuration
bundle composition
notifications
demographics
student summary requests
```

Determine what is already implemented.

---

## 2.3 Database inspection

Confirm:

```text
PostgreSQL version
Neon architecture
pooler mode
connection limits
database size
table sizes
index definitions
index usage
autovacuum health
locks
deadlocks
statistics query shapes
important foreign keys
transaction usage
```

Existing measurements may be reused when still demonstrably applicable.

Do not blindly reproduce expensive measurements merely because the plan mentions them.

---

# 2.4 Required reassessment output

Before implementation, produce:

```text
# Repository Reassessment

## Current architecture
...

## Existing relevant optimizations
...

## Assumptions from this plan confirmed
...

## Assumptions stale or incorrect
...

## Proposed changes to this plan
...

## Newly discovered bottlenecks
...

## Recommended execution order
...

## Items to reject/defer
...

## First implementation task
...
```

For major items, use:

```text
KEEP
MODIFY
REORDER
DEFER
REJECT
REPLACE
ALREADY DONE
INVESTIGATE
```

---

# 3. ENGINEERING STANDARD — BINDS EVERY PHASE

---

## 3.1 No blind optimization

Every optimization must begin with:

```text
What is slow/unsafe?
Why is it slow/unsafe?
What evidence demonstrates that?
What is the smallest fix?
What alternatives were considered?
How will correctness be proven?
How will improvement be measured?
What is the rollback?
```

---

## 3.2 One endpoint per query-rewrite PR

For query rewrites:

> One endpoint per PR.

Do not bundle five unrelated SQL optimizations because they all occur in `StatisticsRepository`.

Infrastructure changes may contain tightly coupled modifications, but unrelated work must remain separate.

---

## 3.3 Query rewrites require equivalence proof

Every rewritten query must be tested for equivalent results across relevant scopes.

Minimum where applicable:

```text
school scope
cross-school scope
grade filtering
date filtering
empty result sets
null values
ties
ordering
duplicate-related cases
rounding boundaries
boundary dates
```

If ordering changes, explicitly test deterministic tie behavior.

If `Math.Round` behavior is involved, explicitly test values around rounding boundaries.

---

## 3.4 Temporary live equivalence harness

For SQL rewrites:

```text
old implementation
vs
new implementation
```

must be compared against the same database data.

The harness should compare:

```text
result sets
counts
ordering where relevant
null behavior
aggregates
paging
```

Temporary files/tests must be removed afterward.

Permanent unit tests remain.

---

## 3.5 `EXPLAIN ANALYZE`

Every material SQL rewrite requires:

```text
EXPLAIN ANALYZE BEFORE
EXPLAIN ANALYZE AFTER
```

The PR must explain:

```text
rows touched
joins
sorts
aggregates
index use
scan strategy
execution time
```

Do not state “the query is more optimized” without identifying what actually improved.

---

## 3.6 Deterministic CI budgets

Add deterministic budget assertions using `DbCommandInterceptor`.

At minimum:

```text
maximum SQL statements per expensive endpoint
maximum result rows returned per endpoint
```

Important distinction:

**Result rows returned are not the same thing as rows scanned by PostgreSQL.**

Use:

* interceptor → command count/result rows;
* `EXPLAIN ANALYZE` → database work.

Do not confuse them.

---

## 3.7 Latency SLAs

Do not make cloud DB p95 latency a hard CI test.

Use:

```text
CI:
  deterministic SQL/result-row budgets

production:
  p50/p95 histogram metrics
  alerts
  structured logging

SQL rewrites:
  EXPLAIN ANALYZE
  representative measurements
```

Time-based CI gates against a shared Neon database will be too noisy to be reliable.

---

## 3.8 No shared-DbContext parallelism

Never:

```csharp
Task.WhenAll(...)
```

over database operations sharing one scoped `DbContext`.

This is an absolute project rule.

If independent parallelism ever becomes necessary:

* prove the benefit;
* use independent contexts;
* measure pool impact;
* do not introduce it casually.

---

## 3.9 Rollback

Rollback is:

```text
revert commit
+
redeploy
```

No runtime kill switches.

No metric-driven automatic rollback.

Every meaningful PR must identify the rollback commit.

---

# 4. PHASE 1 — AVAILABILITY

The original plan strongly suggests the following work, but it must first be validated against the current architecture.

---

# 4.1 Login rate-limit partitioning

### Current hypothesis

`Program.cs` currently uses a fixed partition such as:

```text
"login"
```

which effectively produces one shared login bucket.

The concern is:

```text
5 requests/minute
+
shared instance-wide bucket
=
NAT/class-start lockouts
```

### Proposed direction

Partition by normalized client IP:

```csharp
httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
```

Keep the token-bucket shape:

```text
5/minute
QueueLimit = 0
```

Do not simultaneously change the limiter algorithm.

### Why token bucket remains

The goal is:

```text
allow normal bursts
then throttle sustained credential attempts
```

Do not change to sliding/fixed window merely because the existing configuration has a problem.

---

## 4.1.1 Trusted proxy handling

The per-IP solution is unsafe if client IPs are spoofable through incorrectly trusted forwarding headers.

Inspect:

```text
ForwardedHeaders
KnownNetworks
KnownProxies
```

Do not simply clear trust configuration.

Replace broad/empty trust with the actual configured edge proxies/networks.

The actual client IP used by the limiter must be derived **after trusted forwarding-header normalization**.

---

## 4.1.2 Required verification

Test:

```text
IP A:
  1–5 login requests → allowed
  6th → 429

Retry-After:
  present
  truthful

IP B:
  independent bucket

trusted proxy:
  actual client IP used

untrusted forged forwarding header:
  ignored

/healthz:
  never throttled

NAT spike:
  expected behavior for multiple clients sharing one public IP
```

Do not attempt a sophisticated per-user + per-IP limiter unless real abuse metrics justify it.

The current plan's position is:

```text
per-user only → rejected
IP + user chained limiters → deferred
raising limit blindly → rejected
```

The agent may change these positions if current threat/traffic evidence warrants it.

---

# 4.2 `/me` revocation scoping

### Current hypothesis

JWT lifetime is approximately 60 minutes.

`AccountStatusEndpointFilter` provides revocation enforcement on data routes but does not currently run on:

```text
GET /api/auth/me
```

because the endpoint sits outside the `/api` group.

Potential result:

```text
deactivated account
→ /me = 200
→ data endpoints = 401
```

### Proposed change

Verify endpoint/filter topology.

Likely intended scope:

```text
GET /me
PUT profile
```

must run the account-status filter.

Keep:

```text
login
logout
change-password
```

outside that revocation restriction where required by the existing password-change flow.

### Verification

```text
deactivated account:
  /me → 401

deactivated account:
  protected data → existing 401 behavior

must-change-password:
  password rotation still works

normal account:
  behavior unchanged
```

Do not change JWT expiry as part of this fix.

---

# 4.3 Application boot / migrations

### Current hypothesis

Application startup currently calls:

```csharp
MigrateAsync()
```

This is undesirable when multiple application instances boot simultaneously or a migration is broken.

### Desired topology

Prefer:

```text
database
  ↓ healthy
migration job
  ↓ success
API
  ↓ healthy
web
```

The actual implementation may be:

```text
Compose init/migration service
```

or:

```text
CI/CD release migration
```

depending on the existing deployment pipeline.

The agent must inspect the repo and select the safer existing integration point.

---

## 4.3.1 Required semantics

A failed migration must result in:

```text
migration job non-zero
API does not pretend migration succeeded
web does not start before API health
```

Do not allow an application to unknowingly run against an incompatible schema.

---

## 4.3.2 Health check

Add:

```text
Docker HEALTHCHECK → /healthz
```

but first inspect whether `/healthz` currently:

* performs DB work;
* requires auth;
* passes through the global rate limiter;
* depends on services that can fail.

The health endpoint should be cheap and dependable.

Do not create a restart loop in which DB failure causes the container to become unhealthy and repeatedly restart while making diagnostics/recovery harder.

Separate readiness/liveness only if the current deployment architecture makes that useful.

---

## 4.3.3 Compose ordering

Target:

```text
web
  depends_on
api: service_healthy
```

and migration completion before API startup.

Do not merely use “service started” semantics for the migration dependency.

---

## 4.3.4 Verification

Cold startup:

```text
docker compose up
```

Verify:

```text
database healthy
migration runs once
migration succeeds
API becomes healthy
web starts afterward
normal requests succeed
health requests receive no 429
startup produces no avoidable 500s
failed migration blocks normal startup
```

---

# 5. PHASE 1.5 — OBSERVABILITY FOUNDATION

This should be intentionally small.

Do not build a complete distributed observability platform.

---

# 5.1 Request-level instrumentation

Collect:

```text
traceId
endpoint
method
status
request elapsed
SQL statement count
SQL result-row count
```

Use structured logs.

Avoid logging:

```text
passwords
tokens
sensitive request payloads
full secret-bearing connection information
```

---

# 5.2 Database instrumentation

Recommended ownership:

```text
middleware:
  request timing / endpoint / status

DbCommandInterceptor:
  command count
  result rows
  command timing
  slow-query identification

structured logs:
  correlation and aggregate analysis
```

Do not create competing definitions of “query count.”

---

# 5.3 Slow query logging

Keep an elapsed-time threshold for warning-level DB logs.

A slow SQL event should be correlatable to:

```text
traceId
endpoint
timestamp
duration
```

SQL/parameter logging must respect sensitive-data rules.

---

# 5.4 CI PostgreSQL service

Create a CI PostgreSQL service for deterministic:

```text
integration tests
budget tests
rollback fixtures
```

Pure unit tests remain DB-free.

---

# 6. OPS DOCUMENTATION

Create or update:

```text
ops/runbooks/auth.md
ops/runbooks/migrations.md
ops/runbooks/digest.md
docs/OPS.md
```

---

## 6.1 Authentication runbook

Include exact commands for:

```text
rate-limit testing
trusted proxy verification
429 investigation
health verification
```

---

## 6.2 Migration runbook

Include:

```text
migration execution
failed migration handling
rollback
snapshot synchronization
CONCURRENTLY index process
```

---

## 6.3 Digest runbook

Include:

```text
lease inspection
duplicate detection
stale lease handling
lease revoke
safe rerun
failure investigation
```

---

## 6.4 `docs/OPS.md`

Document:

```text
Neon pooler behavior
transaction pooling implications
advisory-lock limitation
lease-row decision
connection policy
timeout policy
index creation rules
CONCURRENTLY restrictions
migration strategy
rollback procedure
```

---

# 7. PHASE 2 — DATABASE RESILIENCE

---

# 7.1 Transaction inventory BEFORE execution strategy

Before enabling retries, search for:

```text
BeginTransaction
BeginTransactionAsync
TransactionScope
explicit DbTransaction
```

Classify all paths.

Execution strategies and manually controlled transactions must be composed correctly.

Where required, route the operation through:

```csharp
Database.CreateExecutionStrategy()
```

Do this **before enabling global retry behavior**.

---

# 7.2 Execution strategy

Candidate configuration:

```csharp
EnableRetryOnFailure(
    maxRetryCount: 3,
    maxRetryDelay: TimeSpan.FromSeconds(5)
)
```

But the agent must inspect the write paths first.

### Critical rule

Do not claim:

> GUID primary keys make all retries safe.

They do not.

A client may observe a network failure after PostgreSQL successfully committed.

Therefore:

```text
retryable failure
≠
known rollback
```

Writes must be evaluated for ambiguous-commit/replay behavior.

Client-generated GUIDs help detect some duplicates but are not a complete idempotency design.

Where an operation must be replay-safe, inspect whether the current application already has:

```text
idempotency key
unique business key
upsert semantics
state transition guard
transactional outbox
```

or another appropriate mechanism.

Do not introduce these globally unless evidence justifies it.

---

# 7.3 Connection keepalive

Candidate configuration:

```text
KeepAlive=30
Tcp Keepalive=true
```

Treat this primarily as connection resilience rather than a query-performance change.

Measure:

```text
connection churn
pool utilization
network overhead
stale connection behavior
```

Keep the change only if it improves operational resilience without creating adverse effects.

---

# 7.4 Per-path timeouts

Default:

```text
30 seconds
```

Statistics/reporting paths may use approximately:

```text
120 seconds
```

Do not globally change everything to 120 seconds.

The reason is blast-radius control.

---

# 7.5 Timeout logging

Update `GlobalExceptionHandler` so timeout failures have a recognizable signature distinct from generic DB failures.

The logs should make it possible to answer:

```text
which endpoint?
which request?
which query family?
how long?
```

without logging secrets.

---

# 8. PHASE 3 — CACHING

Caching is a secondary optimization.

Do not use caching to hide poorly designed database queries unless measurement shows caching is the correct behavior.

---

# 8.1 HybridCache

Preferred:

```text
Microsoft.Extensions.Caching.Hybrid
```

Fallback when package restore is genuinely unavailable:

```text
IMemoryCache
+
existing request deduplication
```

Do not redesign the caching architecture merely because package restore temporarily fails.

---

## 8.1.1 Candidate data

Investigate:

```text
/states
/cities
/countries
/academic-years*
/schools/me
/auth/me
```

Use bounded TTLs, approximately:

```text
reference data:
1–5 minutes

user/school profile:
short TTL based on actual write frequency
```

Keys must be namespaced.

Examples:

```text
geo:states
geo:cities
geo:countries
academic-years:{scope}
schools:{schoolId}
me:{userId}
```

---

# 8.2 Cache invalidation

Every write affecting cached data must invalidate the relevant cache.

Review:

```text
profile update
account disable
school membership
role/permissions
school updates
academic-year changes
reference-data updates
```

The agent must determine the complete invalidation graph before enabling caching.

---

# 8.3 `/me` cache security

Caching `/auth/me` is acceptable only if:

```text
authentication/revocation is checked independently
```

The cache must never become the authorization decision.

Conceptually:

```text
authenticated + active
       ↓
cache profile
```

not:

```text
cache says user exists
       ↓
therefore authorized
```

---

# 8.4 Stampede protection

Verify simultaneous cache misses.

The desired behavior is:

```text
100 simultaneous callers
→ bounded backend fetches
→ cached result shared
```

Do not write another ad hoc lock if HybridCache already provides the needed stampede protection.

---

# 9. PHASE 3.5 — SELECTIVE OUTPUT CACHING

Only use after authorization behavior is demonstrated.

Potential candidates:

```text
timetables
notices
calendar
other bounded authenticated school-scoped reads
```

Candidate TTL:

```text
60 seconds
```

Candidate variation:

```text
school identity from trusted authorization context
```

---

# 9.1 Hard security tests

For two users/schools:

```text
User A + School A
User B + School B
```

request the same route.

Verify no response generated for A can be served to B.

Also verify:

```text
unauthorized user
→ no protected cached content
```

Do not vary only on URL.

Do not use user-controlled headers as authorization keys.

---

# 9.2 Middleware ordering

Inspect actual pipeline order.

Do not rely solely on documentation snippets.

The PR must explain why cached responses cannot bypass the intended authorization boundary.

---

# 9.3 Statistics exclusion

Do not output-cache statistics unless measurements show meaningful reuse and bounded cardinality.

The current hypothesis is that:

```text
filter combinations ≈ high cardinality
```

making output caching a poor fit.

---

# 9.4 Compression

Candidate:

```text
Brotli
Gzip
```

Measure:

```text
response size
latency
CPU impact
```

Do not assume a 50% reduction universally.

Treat compression gain as measured output.

---

# 10. PHASE 4 — DATABASE QUERY & INDEX OPTIMIZATION

This is the largest part of the plan.

**Every item is a candidate until repository inspection and measurement confirm it.**

---

# 10.1 AuditLogs index

Existing hypothesis:

```text
AuditLogs ≈170k rows
≈22 MB
measured workload produced ≈8.16M sequential rows
zero index use
```

Candidate:

```sql
CREATE INDEX CONCURRENTLY "IX_AuditLogs_UserId_Timestamp"
ON "AuditLogs" ("UserId", "Timestamp" DESC);
```

Before creating it:

```text
identify exact target query
EXPLAIN ANALYZE before
estimate index size
estimate write impact
```

Deployment:

```text
manual online window
+
migration/model snapshot synchronization
```

Never place `CREATE INDEX CONCURRENTLY` inside a normal transactional EF migration.

After:

```text
EXPLAIN ANALYZE
pg_stat_activity
write latency
index usage
```

Watch for a full meaningful workload cycle.

---

# 10.2 Audit retention

Do not impose retention engineering-side.

Prepare an optional proposal covering:

```text
retention period
archival
storage effect
compliance/product impact
```

Product/operations decide.

---

# 10.3 Platform overview fan-out

Target originally identified:

```text
StatisticsOverviewService.cs:58-95
```

Hypothesis:

```text
loop schools
→ per-school DB operations
```

Candidate:

```text
SchoolId = ANY(@ids)
GROUP BY SchoolId
```

But the agent must inspect the actual implementation first.

Compare alternatives:

```text
array parameter
vs
padded scalar parameter
vs
other grouped shape
```

Choose based on actual PostgreSQL plan/performance.

Do not replace serial fan-out with parallel fan-out merely to hide N+1 behavior.

The latter can consume connections and increase DB contention.

### Acceptance

Statement count should not scale linearly with school count.

Test:

```text
1 school
small school set
larger school set
cross-school
filtered
```

All school-level outputs must remain identical.

---

# 10.4 Totals consolidation

Candidates:

```text
AcademicStatisticsRepository.cs
AttendanceStatisticsRepository.cs
StudentStatisticsRepository.cs
```

Investigate whether existing separate totals can be combined into grouped queries.

Do not optimize simply to reduce source-code line count.

Measure:

```text
SQL statements
rows
plan
execution characteristics
```

---

# 10.5 Distribution

Candidate:

```text
SQL GROUP BY / round()
```

Verify PostgreSQL translation and numeric behavior.

Do not assume provider-neutral `Math` behavior without checking generated SQL.

---

# 10.6 GetByAge

Investigate:

```text
current materialization
calculation
grouping
```

Determine whether SQL-side calculation is:

* supported;
* equivalent;
* materially cheaper.

Do not force SQL computation when it creates worse plans or provider-specific complexity.

---

# 10.7 Enrollment trend

Investigate whether the current trend calculation:

* loads too many rows;
* can aggregate in SQL;
* needs complete-list semantics;
* has date boundary behavior that must remain identical.

---

# 10.8 `/by-student`

Investigate existing:

```text
Skip()
Take()
```

shape.

Candidate optimization is SQL-side pagination if it is not already present.

Verify:

```text
stable ordering
page boundaries
total count behavior
school scope
```

Do not introduce keyset pagination yet unless actual scale invalidates offset paging.

---

# 10.9 Capacity

Inspect whether capacity calculations unnecessarily materialize complete collections.

Only rewrite if measurement demonstrates excessive transfer/materialization.

---

# 10.10 Teacher workload / coverage

Target originally identified:

```text
TeacherStatisticsRepository.cs:57-128
```

Potentially the deepest nesting.

Do not perform a massive “rewrite everything” change.

Break down:

```text
scope
joins
teacher grouping
coverage grouping
aggregation
paging
projection
```

Measure each relevant stage.

Preserve authorization and school scoping.

---

# 10.11 New arrivals

Target:

```text
StudentStatisticsRepository.cs:41-51
```

Hypothesis:

```text
correlated ORDER BY + LIMIT 1
per student
```

Candidate:

```text
GROUP BY student
MIN(EnrolledAt)
```

Verify:

```text
same student
same earliest enrollment
same null behavior
same school scope
same date behavior
```

---

# 10.12 Candidate indexes

Initial candidates:

```text
Enrollments(
    SchoolId,
    EnrolledAt
)
WHERE LeftAt IS NULL
```

```text
Marks(
    SchoolId,
    ClassId,
    GradingPeriodId
)
WHERE FirstMark IS NOT NULL
  AND SecondMark IS NOT NULL
```

```text
Attendances(
    SchoolId,
    ClassId,
    DiaryEntryId
)
```

```text
Students(
    SchoolId,
    GraduationDate
)
```

These are **gated candidates**, not guaranteed indexes.

For each candidate:

```text
target query
current plan
expected plan
expected rows reduction
index size
write impact
rollback DDL
CONCURRENTLY procedure
```

Only create if the evidence supports it.

---

# 10.13 Index lifecycle

After creating:

```text
EXPLAIN after
watch pg_stat_activity
watch write latency
watch index usage
```

Do not declare an index successful merely because its creation succeeded.

---

# 10.14 Unused/mirror index drops

Initial candidates:

```text
StudentDocuments(SchoolId,StudentId)
SchoolingHistories(SchoolId,StudentId)
CalendarEvents(StudentId,SchoolId)
Attendances(EnrollmentId)
Classes(Id,AcademicYearId)
```

Before dropping:

```text
inspect dependent queries
check pg_stat_user_indexes
check query plans
check workload coverage
wait for meaningful workload cycle
```

`idx_scan = 0` alone is insufficient evidence.

A rarely used index may protect an important expensive path.

After dropping:

```text
observe query latency
observe errors
recheck plans
observe writes
```

---

# 10.15 `COUNT(*) OVER()` decision

Keep as an open measurement question.

Compare:

```text
COUNT(*) OVER()
```

against:

```text
data query
+
count query
```

Measure:

```text
round trips
execution time
rows processed
memory
EF projection complexity
```

Do not adopt it solely because one round trip disappears.

---

# 11. PHASE 5 — DIGEST DURABILITY

Inspect the existing digest job before creating new structures.

The desired architecture remains a PostgreSQL lease row rather than a session advisory lock.

---

# 11.1 Lease schema candidate

```text
DigestRuns
-----------
job_name PK
owner_id
lease_id
locked_until
last_started_at
last_success_at
failure_count
```

`lease_id` provides a unique ownership token and prevents stale workers from modifying a newer lease.

---

# 11.2 Lease acquisition

Candidate SQL:

```sql
UPDATE DigestRuns
SET
    locked_until = now() + interval '25 hours',
    owner_id = @owner,
    lease_id = @lease,
    last_started_at = now()
WHERE job_name = 'digest'
  AND locked_until < now()
RETURNING job_name;
```

Use database server time.

No application clock comparison for lease expiration.

No advisory locks.

---

# 11.3 Heartbeat

Heartbeat must require matching:

```text
job_name
owner_id
lease_id
```

A stale process must not be able to extend another worker's lease.

---

# 11.4 Recipient processing

Do not build an unbounded:

```text
mails = all recipients
```

collection.

Use bounded batches.

Each recipient:

```text
try
  send
  record success
catch
  log failure
  continue
```

One malformed recipient must not destroy the entire daily run.

---

# 11.5 Recovery

Required scenarios:

```text
normal completion
duplicate start
worker crash
lease expiration
new worker acquisition
stale worker heartbeat
partial recipient failures
```

---

# 11.6 Two-replica manual test

Run two instances.

Kill one during execution.

Verify:

```text
one effective completion
no duplicate ownership
expired lease recovers
stale worker cannot modify new ownership
failure metadata persists
```

---

# 12. PHASE 6 — FRONTEND PERFORMANCE

Independent from backend SQL work where practical.

---

# 12.1 Reference-data TTL cache

Candidate implementation:

```text
Map-based cache
```

API:

```text
getOrFetch(key, ttlMs, factory)
```

Location:

```text
api/
```

Do not put policy inside the lowest-level transport function.

Existing in-flight dedup handles concurrency.

TTL cache handles repeated requests separated in time.

Different concerns.

Do not introduce React Query solely for this.

Do not use localStorage for short-lived server reference data without a demonstrated reason.

---

# 12.2 Statistics filter Apply-on-Done

Current desired behavior:

```text
modal local draft
     ↓
change many controls
     ↓
zero requests
     ↓
Done
     ↓
single committed filter state
     ↓
required fetches
```

Do not rely on debounce to solve this.

Debounce still potentially creates multiple requests during an interaction.

---

# 12.3 Stale-while-reload

Do not destroy rendered sections into:

```text
Loading...
```

during every statistics request.

Keep existing sections mounted.

Use:

```text
aria-busy
loading indicator
```

while preserving useful content.

This improves perceived stability and avoids replacing the page with low-information loading placeholders.

---

# 12.4 Chart memoization

Before canvas:

```text
React.memo
+
stable props
+
useMemo for point arrays
+
isolated hover state
```

Important:

`React.memo` does not help if props are recreated every render.

Fix repeated:

```text
toPoint()
```

mapping first.

---

# 12.5 Chart axis density

Cap labels:

```text
maxAxisLabels
```

Bucket beyond top-N:

```text
Other
```

Keep SVG until measurements show it is insufficient.

The existing hypothesis is that hundreds of SVG nodes are acceptable when renders are isolated and memoized.

---

# 12.6 Student summary endpoint

Candidate:

```http
GET /api/students/summary
```

response:

```json
{
  "active": 0,
  "total": 0
}
```

Replace two:

```text
pageSize=1
```

requests.

Verify that authorization/scoping is identical to the existing requests.

---

# 12.7 Dashboard request audit

Inspect:

```text
demographicsByZone
NotificationsPanel
```

Determine whether:

```text
lazy mounting
request folding
conditional fetch
```

reduces initial work.

Do not make data disappear merely to improve LCP.

Preserve feature behavior.

---

# 13. FRONTEND PERFORMANCE MEASUREMENT

For meaningful frontend changes record:

```text
production bundle size
request count
network payload
initial loading behavior
render/rerender behavior
```

Existing known improvement:

```text
main bundle:
~370 KB → ~187 KB
```

Do not regress this without evidence.

Required gates:

```text
tsc
vite build
```

---

# 14. NON-GOALS

These are intentionally excluded unless the agent produces strong evidence to change them.

---

## Redis

Not currently justified.

Reason:

```text
single application instance
HybridCache can later use Redis without changing call sites
```

---

## Hangfire / Quartz

Not justified for:

```text
one daily job
```

---

## MediatR / generic Unit of Work

Not justified solely for abstraction.

EF `DbContext` already provides unit-of-work behavior.

---

## Microservices

Not justified.

Current architecture:

```text
single DB
school tenancy
single application
```

No measured scaling boundary requires service decomposition.

---

## Keyset pagination

Not currently required.

Offset paging is acceptable while:

```text
page sizes ≤ ~100
deterministic ordering
```

remain true.

---

## `IAsyncEnumerable`

Not a substitute for reducing excessive rows/queries.

---

## Output-caching statistics

Cardinality is expected to be high because filter combinations are numerous.

---

## Cache warming on boot

Single-flight/stampede protection should solve initial concurrency.

Do not create boot-time dependency on remote/database cache warming.

---

## Runtime feature kill switches

Do not add without actual operational need.

---

## Metric-driven auto rollback

Not part of this architecture.

---

## Scheduled manual VACUUM

Autovacuum is currently healthy.

Revisit only if bloat evidence changes.

---

## Chaos testing

Single-replica environment does not justify a large chaos program yet.

---

## Full OpenTelemetry stack

No collector/backend currently justifies it.

Structured logging is enough for this stage.

---

# 15. DEFERRED TRIGGERS

Revisit only when true.

```text
Second application replica
→ chaos tests
→ Redis-backed HybridCache consideration

Dedicated CI runners
→ p95 CI latency gates

Measured database bloat
→ maintenance strategy

Third background job
→ queue infrastructure reconsideration

2,000+ chart nodes / demonstrated SVG bottleneck
→ canvas reconsideration
```

The agent may add new triggers discovered during implementation.

---

# 16. EXECUTION ORDER — BASELINE, NOT IMMUTABLE

The original suggested sequence:

```text
Turn 1:
Phase 1 availability

Turn 2:
Phase 2 resilience
+
AuditLogs index

Turn 3:
HybridCache

Turns 4–5:
database/query rewrites

Turn 6:
digest + frontend
```

is a baseline.

After Phase 0 reassessment, the agent may change this order.

### Priority rule

Generally prioritize:

```text
security/correctness
↓
availability
↓
failure resilience
↓
high-value DB bottlenecks
↓
request reduction
↓
caching
↓
micro-optimizations
```

But measurable evidence may justify a different order.

---

# 17. PR / COMMIT TEMPLATE

Every meaningful change should report:

```text
Problem:
...

Current implementation:
...

Evidence:
...

Original plan recommendation:
...

Agent assessment:
KEEP / MODIFY / REPLACE / etc.

Revised solution:
...

Why this solution:
...

Behavior preserved:
...

Security/authorization impact:
...

Tests:
...

Performance measurement:
...

EXPLAIN:
...

SQL statements before/after:
...

Result rows before/after:
...

Rollback:
...
```

---

# 18. QUERY-REWRITE PR TEMPLATE

Use exactly this structure for database rewrites:

```text
Endpoint:
ONE endpoint only.

BEFORE
Statement count:
Result rows:
Representative timing:
EXPLAIN ANALYZE:

AFTER
Statement count:
Result rows:
Representative timing:
EXPLAIN ANALYZE:

EQUIVALENCE
Harness:
School scope:
Cross-school:
Grade:
Date filters:
Empty cases:
Ties:
Ordering:
Null cases:
Rounding boundaries:

PLAN DELTA
Rows touched:
Join strategy:
Sort nodes:
Aggregate strategy:
Index usage:
Why cheaper:

INDEXES
None
or:
DDL:
CONCURRENTLY deployment:
Write impact:
Rollback DDL:

BUDGETS
Statements ≤:
Result rows ≤:
Test:

ROLLBACK
Commit:
```

---

# 19. INDEX LIFECYCLE TEMPLATE

For every index addition:

```text
Target query:
Current plan:
Expected plan:
Expected benefit:
Index DDL:
Index size estimate:
Write impact:
Rollback DDL:
Online deployment procedure:
```

Post-create:

```text
EXPLAIN ANALYZE
pg_stat_activity
write latency
idx_scan
query behavior
```

For drops:

```text
usage evidence
dependent query inspection
full workload-cycle observation
DROP DDL
rollback CREATE DDL
post-drop observation
```

---

# 20. PERMANENT TEST MATRIX

## Authentication

```text
rate-limit partition by IP
6th request → 429
Retry-After
trusted proxy
forged forwarding header
health exemption
deactivated /me
must-change-password
```

## Statistics/query performance

```text
statement budget
result-row budget
overview
at-risk
excessive
equivalence unit coverage
rounding
ordering
paging
school isolation
```

## Retry/resilience

```text
execution strategy
transaction compatibility
timeout classification
retry/replay-sensitive writes
```

## Digest

```text
lease acquire
lease skip
heartbeat
lease expiration
stale owner rejection
failure metadata
batch processing
```

## Frontend

```text
tsc
vite build
logic-level tests where actual state logic is added
```

Do not add rendering test infrastructure merely for the sake of increasing test count.

---

# 21. STOP CONDITIONS

The agent should stop an individual optimization and reassess when:

```text
the stated bottleneck no longer exists

the existing repository already solved it

the proposed solution duplicates an existing mechanism

the optimization produces negligible benefit

the optimization creates disproportionate complexity

result equivalence cannot be proven

authorization boundaries become unclear

cache invalidation cannot be made complete

retry semantics are ambiguous

database plan becomes worse in an important workload

index write impact is unacceptable

a better architecture-level solution is discovered

the repository's deployment topology differs materially from the assumptions
```

Do not force the code to satisfy the document.

Update the plan instead.

---

# 22. DEFINITION OF SUCCESS

Success does **not** mean:

```text
every item implemented
```

Success means:

```text
measured bottlenecks identified

unnecessary work removed

security boundaries preserved

business behavior preserved

availability improved

failure behavior improved

SQL work reduced where beneficial

request volume reduced where beneficial

frontend work reduced where beneficial

tests strengthened

operational behavior documented

rollback understood

architecture remains coherent

complexity does not increase without justification
```

---

# 23. FINAL ENGINEERING PRINCIPLES

The coding agent must consistently follow these principles:

```text
Measure before optimizing.

Inspect before implementing.

Challenge the plan.

Prefer existing architecture over duplicate infrastructure.

Prefer the smallest change that solves the measured problem.

Correctness before speed.

Authorization before caching.

SQL proof before query rewrite.

EXPLAIN before index creation.

Index usage before index deletion.

Retry semantics before execution-strategy enablement.

Database time before application intuition.

Result rows and database rows-scanned are different metrics.

Per-IP throttling requires trustworthy client-IP derivation.

Health checks must not become a source of cascading failure.

A cache must never become an authorization mechanism.

GUIDs help deduplication but do not magically make retries idempotent.

Leases require ownership/fencing.

Pooler behavior determines which locking mechanisms are valid.

Do not add infrastructure before the existing architecture demonstrates the need.

Do not optimize React rendering before reducing unnecessary requests/data.

Do not retain an optimization solely because it appears in this document.

Do not reject a better optimization merely because it was not predicted by this document.

Document why a decision was made so the next engineer can reproduce the reasoning.
```

---

# 24. INITIAL KNOWN BASELINE — DO NOT REDO UNNECESSARILY

The following work is already reported as complete.

## Backend P0

```text
class summaries paged in SQL
ORDER BY Name, Id
per-page restricted aggregates
at-risk grouped counts
redundant class IN subquery removed
per-student at-risk/excessive lists SQL-paged
Math.Round SQL parity
commit: 91881f0
```

## Wave-2 verification

```text
temporary live equivalence tests
school
cross-school
grade
date
ties
rounding boundaries
rollback fixtures
EXPLAIN ANALYZE before/after
temporary files deleted
permanent ClassStatisticsServiceTests retained
```

## Existing measurements

```text
cross-school overview: 16 statements
grade-filtered: 204 summaries
at-risk/excessive SQL paging complete
132 indexes inspected
table sizes measured
vacuum healthy
zero observed bloat
zero observed deadlocks
zero observed lock issues
```

## Existing DB facts

```text
Neon transaction pooler
KeepAlive=0
no EF execution strategy
AuditLogs ≈170k rows
AuditLogs ≈22 MB
measured workload ≈8.16M sequential rows
zero index use on measured AuditLogs workload
random_page_cost=4
```

## Existing frontend work

```text
route code splitting
main bundle ≈370 KB → ≈187 KB
font stylesheet deblocked using CSP-safe DOM injection
in-flight GET deduplication
React StrictMode duplicate fetch neutralization
```

**Do not repeat these optimizations simply because they appear as relevant topics. Verify their current state first.**

---

# 25. REQUIRED FIRST ACTION

Before touching production code:

1. Inspect the actual repositories.
2. Compare their current architecture with this document.
3. Identify anything already implemented.
4. Identify anything stale.
5. Identify anything missing from this plan.
6. Reorder the work based on evidence.
7. Present the revised execution plan.
8. Then begin the highest-value verified task.

**The revised plan produced after repository inspection becomes the active plan.**

This document is the starting point, not the final truth.

The objective is not to make EduCore look like this document.

The objective is to make EduCore **better than this document predicted**, while retaining correctness, security, explainability, and operational safety.
