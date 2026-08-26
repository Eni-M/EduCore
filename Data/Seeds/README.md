# Dev seed scripts

Manual test data for the main seeded school (`Shkolla Naim Frasheri`). Run with:

```bash
docker exec -i EduCore-db psql -U educore -d EduCoreDB < Data/Seeds/01-sample-people-and-marks.sql
docker exec -i EduCore-db psql -U educore -d EduCoreDB < Data/Seeds/02-parents-and-attendance.sql
```

Both derive every id from what's already in the database (the first school, its academic year,
its Mathematics subject, its first teacher and its `10A` class) rather than hardcoding GUIDs,
so they can run again after a fresh `dotnet ef database update` without editing anything.

`01` adds 18 students split across two classes, a second subject and teacher, and marks --
including two students left with only one component entered, so "incomplete" marks show up in
the statistics rather than every mark being fully graded.

`02` adds parents for a third of the students, and ten lessons per class-subject with a full
attendance register each -- present/absent/excused varies by student so excessive-absence
statistics have a real ranking instead of a flat rate.

Not idempotent: running either twice creates duplicates. They're for a fresh database only.
