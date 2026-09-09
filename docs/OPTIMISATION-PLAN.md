# Optimisation plan

Written during the review of September 2026. Nothing here is implemented — each item is a
finding, why it matters, and what it would take. Ordered by value for effort within each tier.

Measure before and after. Several of these are worth doing only once the network is bigger than
the pilot, and the notes say which.

---

## Tier 1 — Do before the next release

### 1.1 Finish pushing list search and paging into SQL

**Where.** `Helpers/ListPagination.cs` — `ToPagedResult` and `Search` operate on lists that are
already in memory. Its own comment says so: *"it does not reduce the database read"*.

Subjects, schools, directorates, regional admins and teachers were converted during this review.
These still load the whole table and filter it in the service:

| Service | Method | Table |
| --- | --- | --- |
| `AcademicYearService` | `GetPagedAsync` | AcademicYears |
| `ClassService` | `GetPagedAsync` | Classes |
| `StateAdminService` | `GetPagedAsync` | StateAdmins |
| `LocationService` | `GetPagedAsync` ×5 | States, Cities, Zones, Countries, OriginCities |

**Why it matters.** A search for one row reads every row, deserialises it, maps it to a DTO and
throws almost all of it away. Cities and classes are the two that grow with the network.

**Effort.** Low and mechanical — the pattern is now established in five repositories. One paged
method per repository using `SearchTerm.Like` and `ToPageAsync`, one service change, and an entry
in `SearchTranslatesToSqlTests` so it cannot silently regress.

**Risk.** Low. Watch for one behaviour change: in-memory search matched the *DTO's* fields, some
of which are computed. Anything not a real column needs either a join or to be dropped from the
searchable set.

### 1.2 Index the columns the new searches hit

**Where.** `User.FirstName`, `User.LastName`, `User.Email`, `School.Name`, `Subject.Name`,
`City.Name`.

**Why it matters.** `ILIKE '%term%'` cannot use a B-tree index, so every search added in 1.1 and
during this review is a sequential scan. That is fine at pilot size and will not stay fine.

**Effort.** Low. `CREATE EXTENSION pg_trgm` plus a GIN trigram index per column, in one migration.

**Risk.** Low, but measure first — below a few thousand rows a sequential scan is faster than the
index, and the indexes cost write throughput. Do 1.1 first so there is something to measure.

### 1.3 Stop `seed-dump.sql` living in git

**Where.** Repository root, 15 MB, referenced by `docs/DEV-SETUP.md`.

**Why it matters.** It is more than 99% of the repository by size. Every clone and every CI run
pays for it, forever, because git keeps it in history even if it is deleted later.

**Effort.** Low: publish it as a release asset or drop it in the S3 bucket, and point DEV-SETUP at
the URL. Removing it from *history* is a rewrite and should ride along with any other history work
rather than being done for its own sake.

**Risk.** None to the application. Coordinate with the team so nobody's setup breaks.

### 1.4 Give `AssignmentService` the same access checks as the rest

**Where.** `Services/AssignmentService.cs`.

**Why it matters.** It is the newest area of the codebase and does not match the surrounding
style: `Assignment` has to be written as `global::EduCore.Models.Assignment` because the namespace
collides with `EduCore.Services.Assignment`, and the file reads as if it were written against a
different set of conventions from its neighbours. It works, but it is the file a reviewer will
open and judge the codebase by.

**Effort.** Low. Rename the `Services/Assignment` folder namespace (or the service's own), and
bring the guard clauses in line with `StudentDocumentService`.

**Risk.** Low, contained by the existing tests.

---

## Tier 2 — Worth doing before the network grows

### 2.1 Give the dashboard its own queries

**Where.** `Services/DashboardService.cs` (~394 lines, the largest service).

**Why it matters.** The dashboard is deliberately composed from existing services so that a figure
and the page it links to cannot disagree — that is a good decision and should stay. The cost is
that it calls several services that each fetch more than the dashboard needs; `GetAllAsync(null, ct)`
on classes is called to count them.

**Effort.** Medium. Add count-only repository methods (`CountAsync`, `CountWithoutHomeroomAsync`)
and let the dashboard use those, keeping composition for anything that is actually displayed.

**Risk.** Medium — this is the one place where a "faster" change can make two screens disagree.
Add a test per figure that asserts the dashboard number equals the list page's number.

### 2.2 Split `StudentDetailPage.tsx`

**Where.** `src/pages/Student/StudentDetailPage.tsx`, 1,076 lines and by far the largest file in
the front end. It contains the profile, schooling history, medical, documents, guardians, marks
and academic history sections as one module.

**Why it matters.** It is a maintenance problem more than a performance one, but it is also the
largest lazy-loaded chunk after the statistics page, so a directorate pays for all seven sections
to open one.

**Effort.** Medium, purely mechanical: one file per section, imported by the page.

**Risk.** Low. Typecheck catches everything that matters.

### 2.3 Reduce over-fetching on the student and guardian screens

**Where.** `MyMarksPage`, `MyAttendancePage`, `SchedulePage`, `ReportPage` all call
`/api/students/{id}/profile`, which returns the student's entire mark and attendance history, and
then each shows one slice of it.

**Why it matters.** A student in their final year carries several years of marks and thousands of
attendance rows into the browser to render one term's table. It grows every year the pupil stays.

**Effort.** Medium. Either add `from`/`to`/`gradingPeriodId` parameters to a slimmer profile
endpoint, or point these pages at the existing `/students/{id}/marks` and `/students/{id}/attendance`
endpoints, which already take those filters. The printable report is the one page that legitimately
wants everything.

**Risk.** Low, but touches the guardian portal, so test both roles.

### 2.4 Cache the reference data the pickers load

**Where.** `CityPicker`, `ZonePicker`, `SchoolPicker`, `OriginPicker`, `TermPicker`.

**Why it matters.** Geography changes a few times a year and is fetched on every mount. The
existing in-flight coalescing in `api/client.ts` only merges *simultaneous* calls; sequential
navigations refetch.

**Effort.** Low–medium. A small TTL cache in the client for `/api/states`, `/api/cities`,
`/api/zones`, or an `ETag` on those endpoints and letting the browser do it. Prefer the ETag —
it needs no cache-invalidation logic in the front end.

**Risk.** Low, provided a city added by an administrator appears without a hard refresh. A short
TTL (minutes) or an ETag both satisfy that.

---

## Tier 3 — When there is evidence it is needed

### 3.1 Server-rendered PDF reports

The printable report added in this review uses the browser's own print dialogue, which is right
for a family printing one document. A school office producing three hundred end-of-year reports
wants a real file from the server, with the school's letterhead, generated once and stored.

**Effort.** Medium, plus a new dependency (QuestPDF). **Do not start this until someone asks for
bulk output** — the browser path covers the stated requirement today.

### 3.2 Offline attendance and marks

The classroom is where connectivity is worst and it is the screen a teacher opens every hour. A
service worker with a queued write would remove the one failure teachers will complain about.

**Effort.** High, and it introduces conflict resolution. Worth it only with evidence from real
schools that this is the problem.

### 3.3 Widen the snapshot to filtered queries

`StatisticsSnapshotService` only serves unfiltered single-school queries. Every breakdown — by
class, grade, subject, teacher — computes live. That is the right first cut, because narrowing
multiplies the key space beyond what is worth storing.

If the breakdown screens turn out to be the slow ones in practice, the next step is to snapshot the
*most-used* two or three breakdowns rather than all of them. **Get the usage numbers from `/metrics`
first** — this is easy to get wrong by caching the combinations nobody opens.

### 3.4 Trim the initial bundle

The front end is already route-split and the shared chunk is ~62 KB gzipped, which is healthy. The
statistics page is the largest route at ~8 KB gzipped, most of it the hand-written chart components.

There is nothing to do here yet. Revisit only if a real measurement on a real connection says the
first paint is slow — and resist adding a charting library, which would cost more than the
components it replaced.

---

## Deliberately not doing

- **Adding a caching layer in front of the database.** The snapshot table already covers the
  expensive reads, and the account-status cache covers the frequent one. A general cache would add
  invalidation bugs to buy something we have not measured a need for.
- **Replacing the hand-written test fakes with a mocking library.** The fakes are more code, but
  they fail to compile when an interface changes — which is how the missing search methods were
  caught during this review. A mock would have silently returned null.
- **Splitting the API into services.** One deployable is correct at this size. Revisit if two parts
  of the system ever need to scale differently, which they currently do not.
