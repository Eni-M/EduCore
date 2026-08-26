-- Realistic sample data for EduCore, inserted directly.
--
-- Everything derives from what is already in the database, so this can run against the
-- existing school without hardcoding any id. The password hash is copied from the existing
-- student, so every seeded account shares that student's password.
BEGIN;

DO $$
DECLARE
  school   uuid;
  year     uuid;
  term     uuid;
  maths    uuid;
  physics  uuid;
  teacher  uuid;
  teacher2 uuid;
  class10a uuid;
  class10b uuid;
  cst      uuid;
  hash     text;
  names    text[][] := ARRAY[
    ARRAY['Arber','Balla'],   ARRAY['Besa','Cela'],    ARRAY['Dritan','Dema'],
    ARRAY['Elona','Gjoni'],   ARRAY['Fatjon','Hasa'],  ARRAY['Gentiana','Idrizi'],
    ARRAY['Ilir','Jaku'],     ARRAY['Jonida','Kraja'], ARRAY['Klaudio','Lleshi'],
    ARRAY['Lorena','Malaj'],  ARRAY['Mirel','Nika'],   ARRAY['Nertila','Prifti'],
    ARRAY['Orges','Qosja'],   ARRAY['Pranvera','Rama'],ARRAY['Redi','Shehu'],
    ARRAY['Sara','Tahiri'],   ARRAY['Taulant','Uka'],  ARRAY['Vera','Xhaferi']
  ];
  first_marks  int[] := ARRAY[5,4,3,5,2,4,3,4,5,1,3,4,2,5,4,3,4,5];
  second_marks int[] := ARRAY[5,4,4,4,2,5,3,5,4,2,4,3,3,4,5,4,3,5];
  phys_first   int[] := ARRAY[4,3,3,4,1,4,2,4,4,2,3,3,2,4,3,3,4,4];
  phys_second  int[] := ARRAY[5,4,3,5,2,3,3,4,5,1,4,4,3,5,4,2,4,5];
  stu      uuid;
  usr      uuid;
  enr      uuid;
  cls      uuid;
  i        int;
BEGIN
  SELECT "Id" INTO school FROM "Schools" LIMIT 1;
  SELECT "Id" INTO year   FROM "AcademicYears" WHERE "SchoolId" = school LIMIT 1;
  SELECT "Id" INTO term   FROM "GradingPeriods" WHERE "SchoolId" = school LIMIT 1;
  SELECT "Id" INTO maths  FROM "Subjects" WHERE "SchoolId" = school AND "Name" = 'Mathematics';
  SELECT "Id" INTO teacher FROM "Teachers" WHERE "SchoolId" = school LIMIT 1;
  SELECT "Id" INTO class10a FROM "Classes" WHERE "SchoolId" = school AND "Name" = '10A';
  SELECT "PasswordHash" INTO hash FROM "Users" LIMIT 1;

  -- A second subject and a second teacher, so per-subject and per-teacher figures have
  -- something to compare.
  physics := gen_random_uuid();
  INSERT INTO "Subjects" ("Id","SchoolId","Name") VALUES (physics, school, 'Physics');

  usr := gen_random_uuid();
  teacher2 := gen_random_uuid();
  INSERT INTO "Users" ("Id","FirstName","LastName","Email","PasswordHash","MustChangePassword","Role")
  VALUES (usr,'Blerta','Zeka','blerta.zeka@educore.com',hash,false,1);
  INSERT INTO "Teachers" ("Id","UserId","SchoolId") VALUES (teacher2, usr, school);

  INSERT INTO "TeacherSubjects" ("Id","TeacherId","SubjectId","SchoolId") VALUES (gen_random_uuid(), teacher2, physics, school);
  INSERT INTO "TeacherSubjects" ("Id","TeacherId","SubjectId","SchoolId") VALUES (gen_random_uuid(), teacher, physics, school)
  ON CONFLICT DO NOTHING;

  -- A second class, so class comparison and grade-level breakdowns are not a single row.
  class10b := gen_random_uuid();
  INSERT INTO "Classes" ("Id","Name","SchoolId","AcademicYearId","Grade","HomeroomTeacherId","Capacity")
  VALUES (class10b,'10B',school,year,10,teacher2,28);

  INSERT INTO "ClassSubjectTeachers" ("Id","ClassId","SubjectId","TeacherId","SchoolId")
  VALUES (gen_random_uuid(), class10a, physics, teacher, school);
  INSERT INTO "ClassSubjectTeachers" ("Id","ClassId","SubjectId","TeacherId","SchoolId")
  VALUES (gen_random_uuid(), class10b, maths, teacher, school);
  INSERT INTO "ClassSubjectTeachers" ("Id","ClassId","SubjectId","TeacherId","SchoolId")
  VALUES (gen_random_uuid(), class10b, physics, teacher2, school);

  FOR i IN 1..array_length(names,1) LOOP
    -- Alternating classes gives both a real population rather than loading one.
    cls := CASE WHEN i % 2 = 0 THEN class10b ELSE class10a END;

    usr := gen_random_uuid();
    stu := gen_random_uuid();
    enr := gen_random_uuid();

    INSERT INTO "Users" ("Id","FirstName","LastName","Email","PasswordHash","MustChangePassword","Role")
    VALUES (usr, names[i][1], names[i][2],
            lower(names[i][1] || '.' || names[i][2] || '@educore.com'), hash, false, 0);

    INSERT INTO "Students" ("Id","UserId","SchoolId","DateOfBirth","PersonalNumber","Gender")
    VALUES (stu, usr, school,
            DATE '2010-01-01' + (i * 17), 'PN' || lpad(i::text, 7, '0'),
            -- Gender alternates; a couple are left null so "Not recorded" is exercised.
            CASE WHEN i IN (7, 13) THEN NULL WHEN i % 2 = 0 THEN 0 ELSE 1 END);

    INSERT INTO "Enrollments" ("Id","StudentId","ClassId","SchoolId","EnrolledAt")
    VALUES (enr, stu, cls, school, DATE '2025-09-01');

    -- Maths for everyone.
    SELECT "Id" INTO cst FROM "ClassSubjectTeachers"
    WHERE "ClassId" = cls AND "SubjectId" = maths AND "EndedOn" IS NULL;

    INSERT INTO "Marks" ("Id","EnrollmentId","ClassId","AcademicYearId","SchoolId",
                         "ClassSubjectTeacherId","SubjectId","TeacherId","GradingPeriodId",
                         "FirstMark","SecondMark")
    SELECT gen_random_uuid(), enr, cls, year, school, cst, maths, t."TeacherId", term,
           first_marks[i], second_marks[i]
    FROM "ClassSubjectTeachers" t WHERE t."Id" = cst;

    -- Physics for all but the last two, so "incomplete" and missing coverage are visible.
    IF i <= array_length(names,1) - 2 THEN
      SELECT "Id" INTO cst FROM "ClassSubjectTeachers"
      WHERE "ClassId" = cls AND "SubjectId" = physics AND "EndedOn" IS NULL;

      INSERT INTO "Marks" ("Id","EnrollmentId","ClassId","AcademicYearId","SchoolId",
                           "ClassSubjectTeacherId","SubjectId","TeacherId","GradingPeriodId",
                           "FirstMark","SecondMark")
      SELECT gen_random_uuid(), enr, cls, year, school, cst, physics, t."TeacherId", term,
             phys_first[i],
             -- Two students have no second mark yet: their final stays null, and the
             -- dashboard must show that as incomplete rather than as a low grade.
             CASE WHEN i IN (4, 9) THEN NULL ELSE phys_second[i] END
      FROM "ClassSubjectTeachers" t WHERE t."Id" = cst;
    END IF;
  END LOOP;
END $$;

COMMIT;
