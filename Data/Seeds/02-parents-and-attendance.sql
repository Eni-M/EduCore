-- Second seeding pass: fills in the tables the first pass left empty (Parents, Attendances,
-- DiaryEntries) for the main high school, so attendance and coverage statistics have real
-- substance to test against.
BEGIN;

DO $$
DECLARE
  school       uuid;
  hash         text;
  student_row  RECORD;
  assignment   RECORD;
  entry        uuid;
  lesson_date  date;
  day_offset   int;
  parent_i     int := 0;
  assignment_i int := 0;
BEGIN
  SELECT "Id" INTO school FROM "Schools" WHERE "Name" = 'Shkolla Naim Frasheri';
  SELECT "PasswordHash" INTO hash FROM "Users" LIMIT 1;

  -- A parent for every third student, so the feature has real rows without being tedious.
  FOR student_row IN
    SELECT s."Id", u."LastName"
    FROM "Students" s JOIN "Users" u ON u."Id" = s."UserId"
    WHERE s."SchoolId" = school
    ORDER BY u."LastName"
  LOOP
    parent_i := parent_i + 1;

    IF parent_i % 3 = 0 THEN
      INSERT INTO "Parents" ("Id", "StudentId", "SchoolId", "Name", "PhoneNumber")
      VALUES (
        gen_random_uuid(), student_row."Id", school,
        student_row."LastName" || ' family',
        '+355 6' || lpad((10000000 + parent_i)::text, 8, '0')
      );
    END IF;
  END LOOP;

  -- Ten lesson days per active class-subject assignment, spread over two weeks with weekends
  -- skipped, each with a full attendance register.
  FOR assignment IN
    SELECT cst."ClassId", cst."SubjectId", cst."TeacherId"
    FROM "ClassSubjectTeachers" cst
    WHERE cst."SchoolId" = school AND cst."EndedOn" IS NULL
  LOOP
    -- Distinct starting point per class-subject pair -- otherwise two subjects taught to the
    -- same class would land lessons on the same date, and DiaryEntry's (ClassId, Date, Period)
    -- uniqueness would reject the second one.
    assignment_i := assignment_i + 1;
    day_offset := assignment_i * 2;

    FOR i IN 1..10 LOOP
      -- Walk forward skipping Saturday(6)/Sunday(0) so lessons land on school days.
      LOOP
        day_offset := day_offset + 1;
        lesson_date := DATE '2025-09-01' + day_offset;
        EXIT WHEN EXTRACT(DOW FROM lesson_date) NOT IN (0, 6);
      END LOOP;

      entry := gen_random_uuid();

      -- Each class-subject pair gets its own 4-period band (1-4, 5-8, ...), so two subjects
      -- for the same class can never collide on (ClassId, Date, Period) even if their dates
      -- happen to coincide -- relying on the dates staying apart would only be probabilistic.
      INSERT INTO "DiaryEntries" ("Id", "ClassId", "SubjectId", "TeacherId", "SchoolId",
                                  "Date", "Period", "Topic", "Notes")
      VALUES (
        entry, assignment."ClassId", assignment."SubjectId", assignment."TeacherId", school,
        lesson_date, 1 + ((assignment_i - 1) * 4 + (i % 4)), 'Lesson ' || i, NULL
      );

      -- One register row per student enrolled in this class on this date. Absence rate
      -- deliberately varies by student (i % 7) so excessive-absence statistics have a real
      -- top of the list rather than a flat rate everywhere.
      INSERT INTO "Attendances" ("Id", "DiaryEntryId", "EnrollmentId", "ClassId", "SchoolId",
                                 "IsPresent", "IsExcused", "Comment", "BehaviorMark")
      SELECT
        gen_random_uuid(), entry, e."Id", assignment."ClassId", school,
        NOT (mod(abs(hashtext(e."Id"::text || i::text)), 7) = 0),
        mod(abs(hashtext(e."Id"::text || i::text)), 7) = 0 AND mod(abs(hashtext(e."Id"::text)), 2) = 0,
        NULL,
        CASE WHEN mod(abs(hashtext(e."Id"::text || i::text || 'b')), 3) = 0
             THEN 2 + mod(abs(hashtext(e."Id"::text || i::text || 'c')), 4)
             ELSE NULL END
      FROM "Enrollments" e
      WHERE e."ClassId" = assignment."ClassId" AND e."LeftAt" IS NULL;
    END LOOP;
  END LOOP;
END $$;

COMMIT;
