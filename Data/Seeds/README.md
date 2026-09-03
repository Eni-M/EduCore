# Dev seed data

`00-full-dataset.sql` builds the whole development dataset from an empty database. It
replaces the earlier `01-`/`02-` scripts, which could only add rows to a school that already
existed and so could not rebuild anything.

## Rebuilding from scratch

```bash
docker exec EduCore-db psql -U educore -d postgres -c 'DROP DATABASE "EduCoreDB";'
docker exec EduCore-db psql -U educore -d postgres -c 'CREATE DATABASE "EduCoreDB" OWNER educore;'
dotnet ef database update
```

Start the API once so `PlatformOwnerSeeder` creates the first account — the top-tier
**state administrator** and its state (default `Kosova`, override with `SuperAdmin__StateName`).
It needs `SuperAdmin__Email` and `SuperAdmin__Password` in `.env`. Then:

```bash
docker exec -i EduCore-db psql -U educore -d EduCoreDB -v ON_ERROR_STOP=1 < Data/Seeds/00-full-dataset.sql
```

The script copies the state administrator's password hash onto every account it creates, so all
seeded users share that one password and no secret is written down in this repository. It needs
that account to exist first for exactly that reason, and reuses the `Kosova` state it created.

Not idempotent — running it twice violates the unique index on user email. It is for an empty
database only.

## What it contains, and why

The dataset exists to make every branch of the app reachable, so a screen that is broken for
one kind of record cannot look fine because no such record exists.

| | |
|---|---|
| Account tiers | 1 state admin (seeded on start), 3 regional admins (Prishtina/Prizren/Peja), 4 directorates, 4 guardians (one across two schools) |
| Schools | 4 — one Elementary, one JuniorHigh, one HighSchool, and one **switched off** |
| Academic years | 10, each with **exactly two** non-overlapping terms |
| Students | 38 across 11 classes |
| Marks | 195, spanning fails, at-risk grades and passes |
| Lessons / attendance | 142 / 598, all inside their own year and never in the future |
| Timetables | 3 morning shifts (6 periods each) and 32 schedule entries across three classes (5A, 9A, 12A) |

**Academic year selection.** `AcademicYearPicker` has three cases and the data covers the two
that can occur with a started year: Shkolla Ismail Qemali's current year *contains today*,
while the other three schools sit in the summer gap and fall back to the year that most
recently *ended*. The third case (nothing finished yet) is covered by unit tests.

**Graduation.** Grade 5, 9 and 12 are the last grade of each school type, so only those can
graduate. The data holds, deliberately:

- students in grades 5, 9 and 12 who are fully graded and **can** graduate
- a grade 12 student whose last mark is **half-entered** (second component missing)
- a grade 12 student with a subject **never graded** in the second term
- a whole grade 9 class mid-year, with only the first term graded
- students in grades 4, 8, 10 and 11 — never graduable, however well graded
- one student who **graduated this summer**, and two earlier generations (2024, 2025)
- enrollments closed as Graduated, TransferredOut and Withdrawn

**Edge cases.** One deactivated sign-in, one student with no gender recorded, one with no city
recorded, five students with guardians (one guardian shared across two schools by national ID),
and one school with `DeactivatedAt` set.
