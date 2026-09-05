-- Grade curricula: which subjects each grade is taught, per school.
--
-- Teaching assignments are restricted to the subjects on the class grade's curriculum
-- (ClassSubjectTeacherService), so a school with no rows in "GradeCurricula" cannot have a
-- teacher assigned to anything. This fills that table for every school already in the
-- database, using the grade band its type allows: Elementary 1-5, JuniorHigh 6-9,
-- HighSchool 10-12.
--
-- The syllabus widens as pupils move up rather than repeating one list per grade, which is
-- the point of storing it per grade at all: grade 1 has no English, physics starts in
-- grade 7, chemistry in grade 8, philosophy in grade 11, and biology gives way to sociology
-- in the final year.
--
-- Subjects named here that a school does not have yet are created for it. Nothing else is
-- touched: no teaching assignments are added, so which subjects a class is actually taught
-- (and therefore who can graduate) is left exactly as it was.
--
-- Safe to run more than once, and safe to run on a database that already has some curriculum
-- rows — both inserts are ON CONFLICT DO NOTHING against the existing unique indexes
-- (Subjects on SchoolId+Name, GradeCurricula on SchoolId+Grade+SubjectId).
--
-- Usage:
--   docker exec -i EduCore-db psql -U educore -d EduCoreDB -v ON_ERROR_STOP=1 < Data/Seeds/01-grade-curricula.sql

BEGIN;

DO $$
DECLARE
  r              record;
  subject_id     uuid;
  inserted       int;
  subjects_added int := 0;
  entries_added  int := 0;
BEGIN
  FOR r IN
    SELECT s."Id" AS school_id, p.grade, p.subject_name
    FROM "Schools" s
    JOIN (
      -- Elementary (Type 0), grades 1-5 -------------------------------------------------
      -- Literacy, numeracy and the practical subjects run the whole way through.
      SELECT 0 AS school_type, g AS grade, sub AS subject_name
      FROM generate_series(1, 5) g
      CROSS JOIN unnest(ARRAY[
        'Gjuhe Shqipe', 'Matematike', 'Edukate Figurative', 'Edukate Muzikore', 'Edukate Fizike'
      ]) sub
      UNION ALL
      -- The first foreign language and natural science arrive in grade 3.
      SELECT 0, g, sub
      FROM generate_series(3, 5) g
      CROSS JOIN unnest(ARRAY['Anglisht', 'Dituri Natyre']) sub
      UNION ALL
      -- Society and environment in the last two years.
      SELECT 0, g, 'Shoqeria dhe Mjedisi' FROM generate_series(4, 5) g

      UNION ALL
      -- Junior high (Type 1), grades 6-9 ------------------------------------------------
      SELECT 1, g, sub
      FROM generate_series(6, 9) g
      CROSS JOIN unnest(ARRAY[
        'Gjuhe Shqipe', 'Matematike', 'Anglisht', 'Biologji', 'Histori',
        'Gjeografi', 'Edukate Qytetare', 'TIK', 'Edukate Fizike'
      ]) sub
      UNION ALL
      SELECT 1, g, 'Fizike' FROM generate_series(7, 9) g
      UNION ALL
      SELECT 1, g, 'Kimi'   FROM generate_series(8, 9) g

      UNION ALL
      -- High school (Type 2), grades 10-12 ----------------------------------------------
      SELECT 2, g, sub
      FROM generate_series(10, 12) g
      CROSS JOIN unnest(ARRAY[
        'Matematike', 'Letersi', 'Anglisht', 'Fizike', 'Kimi', 'Histori', 'TIK', 'Edukate Fizike'
      ]) sub
      UNION ALL
      -- Then it specialises: geography ends after grade 10, philosophy starts in 11,
      -- biology gives way to sociology in the final year.
      SELECT 2, 10, sub FROM unnest(ARRAY['Gjeografi', 'Biologji']) sub
      UNION ALL
      SELECT 2, 11, sub FROM unnest(ARRAY['Biologji', 'Filozofi']) sub
      UNION ALL
      SELECT 2, 12, sub FROM unnest(ARRAY['Filozofi', 'Sociologji']) sub
    ) p ON p.school_type = s."Type"
    ORDER BY s."Id", p.grade, p.subject_name
  LOOP
    -- The subject may already exist under this school, may have been created a moment ago
    -- for an earlier grade, or may not exist at all.
    SELECT "Id" INTO subject_id
    FROM "Subjects"
    WHERE "SchoolId" = r.school_id AND "Name" = r.subject_name;

    IF subject_id IS NULL THEN
      subject_id := gen_random_uuid();

      INSERT INTO "Subjects"("Id", "SchoolId", "Name")
      VALUES (subject_id, r.school_id, r.subject_name);

      subjects_added := subjects_added + 1;
    END IF;

    INSERT INTO "GradeCurricula"("Id", "SchoolId", "Grade", "SubjectId")
    VALUES (gen_random_uuid(), r.school_id, r.grade, subject_id)
    ON CONFLICT DO NOTHING;

    -- ON CONFLICT means this is 0 on a re-run, so the notice reports what actually changed.
    GET DIAGNOSTICS inserted = ROW_COUNT;
    entries_added := entries_added + inserted;
  END LOOP;

  RAISE NOTICE 'Subjects created: %', subjects_added;
  RAISE NOTICE 'Curriculum entries created: %', entries_added;
END $$;

COMMIT;

-- What landed, per school and grade.
SELECT s."Name" AS school,
       CASE s."Type" WHEN 0 THEN 'Elementary' WHEN 1 THEN 'JuniorHigh' ELSE 'HighSchool' END AS type,
       gc."Grade" AS grade,
       count(*) AS subjects,
       string_agg(sub."Name", ', ' ORDER BY sub."Name") AS curriculum
FROM "GradeCurricula" gc
JOIN "Schools" s   ON s."Id"   = gc."SchoolId"
JOIN "Subjects" sub ON sub."Id" = gc."SubjectId"
GROUP BY s."Name", s."Type", gc."Grade"
ORDER BY s."Name", gc."Grade";
