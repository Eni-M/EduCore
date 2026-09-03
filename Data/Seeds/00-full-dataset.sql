BEGIN;

CREATE TABLE sd_ctx(hash text, today date);
-- Every seeded user signs in with "SuperAdmin1234!" (PBKDF2 hash below), MustChangePassword=false.
INSERT INTO sd_ctx VALUES (
  'AQAAAAIAAYagAAAAENdg6qZ9/rUL1UtC83XI5TVx3cg1afWv+7Sv4NTxId3Jf3HM+9lq1S5OQs3wLTXxiA==',
  CURRENT_DATE);

CREATE FUNCTION sd_user(fn text, ln text, email text, role int, deactivated boolean DEFAULT false)
RETURNS uuid LANGUAGE plpgsql AS $fn$
DECLARE id uuid := gen_random_uuid();
BEGIN
  INSERT INTO "Users"("Id","FirstName","LastName","Email","PasswordHash","MustChangePassword","Role","DeactivatedAt")
  VALUES (id, fn, ln, email, (SELECT hash FROM sd_ctx), false, role,
          CASE WHEN deactivated THEN now() - interval '9 days' ELSE NULL END);
  RETURN id;
END $fn$;

CREATE FUNCTION sd_year(school uuid, nm text, s date, e date)
RETURNS uuid LANGUAGE plpgsql AS $fn$
DECLARE
  id uuid := gen_random_uuid();
  t1_end date;
  t2_start date;
BEGIN
  INSERT INTO "AcademicYears"("Id","SchoolId","Name","StartDate","EndDate") VALUES (id, school, nm, s, e);

  t1_end   := LEAST(make_date(EXTRACT(YEAR FROM s)::int, 12, 26), e);
  t2_start := GREATEST(make_date(EXTRACT(YEAR FROM s)::int + 1, 1, 7), t1_end + 1);

  INSERT INTO "GradingPeriods"("Id","AcademicYearId","SchoolId","Name","StartDate","EndDate")
  VALUES (gen_random_uuid(), id, school, 'Semestri I', s, t1_end),
         (gen_random_uuid(), id, school, 'Semestri II', t2_start, e);
  RETURN id;
END $fn$;

CREATE FUNCTION sd_teacher(school uuid, fn text, ln text, email text)
RETURNS uuid LANGUAGE plpgsql AS $f$
DECLARE id uuid := gen_random_uuid();
BEGIN
  INSERT INTO "Teachers"("Id","UserId","SchoolId") VALUES (id, sd_user(fn, ln, email, 2), school);
  RETURN id;
END $f$;

CREATE FUNCTION sd_subject(school uuid, nm text)
RETURNS uuid LANGUAGE plpgsql AS $f$
DECLARE id uuid := gen_random_uuid();
BEGIN
  INSERT INTO "Subjects"("Id","SchoolId","Name") VALUES (id, school, nm);
  RETURN id;
END $f$;

CREATE FUNCTION sd_class(school uuid, yr uuid, nm text, grade int, homeroom uuid, cap int DEFAULT 30)
RETURNS uuid LANGUAGE plpgsql AS $f$
DECLARE id uuid := gen_random_uuid();
BEGIN
  INSERT INTO "Classes"("Id","Name","SchoolId","AcademicYearId","Grade","HomeroomTeacherId","Capacity")
  VALUES (id, nm, school, yr, grade, homeroom, cap);
  RETURN id;
END $f$;

CREATE FUNCTION sd_assign(school uuid, cls uuid, subj uuid, tch uuid)
RETURNS uuid LANGUAGE plpgsql AS $f$
DECLARE id uuid := gen_random_uuid();
BEGIN
  INSERT INTO "TeacherSubjects"("Id","TeacherId","SubjectId","SchoolId")
  VALUES (gen_random_uuid(), tch, subj, school)
  ON CONFLICT DO NOTHING;

  INSERT INTO "ClassSubjectTeachers"("Id","ClassId","SubjectId","TeacherId","SchoolId","EndedOn")
  VALUES (id, cls, subj, tch, school, NULL);
  RETURN id;
END $f$;

CREATE FUNCTION sd_student(
  school uuid, fn text, ln text, email text, dob date, pn text,
  gender int, city uuid, graduated date DEFAULT NULL, deactivated boolean DEFAULT false)
RETURNS uuid LANGUAGE plpgsql AS $f$
DECLARE id uuid := gen_random_uuid();
BEGIN
  -- Student origin is a Country now (separate from school geography). The legacy
  -- `city` argument only signals whether an origin was recorded; map it to Kosovo,
  -- or leave it unrecorded when null.
  INSERT INTO "Students"("Id","UserId","SchoolId","DateOfBirth","PersonalNumber","GraduationDate","Gender","CountryId")
  VALUES (id, sd_user(fn, ln, email, 0, deactivated), school, dob, pn, graduated, gender,
          CASE WHEN city IS NULL THEN NULL ELSE 'b1000000-0000-0000-0000-000000000001'::uuid END);
  RETURN id;
END $f$;

CREATE FUNCTION sd_enroll(school uuid, stu uuid, cls uuid, from_date date,
                          left_date date DEFAULT NULL, reason int DEFAULT NULL)
RETURNS uuid LANGUAGE plpgsql AS $f$
DECLARE id uuid := gen_random_uuid();
BEGIN
  INSERT INTO "Enrollments"("Id","StudentId","ClassId","SchoolId","EnrolledAt","LeftAt","ExitReason")
  VALUES (id, stu, cls, school, from_date, left_date, reason);
  RETURN id;
END $f$;

CREATE FUNCTION sd_mark(enr uuid, cst uuid, term uuid, first_mark int, second_mark int)
RETURNS void LANGUAGE plpgsql AS $f$
BEGIN
  INSERT INTO "Marks"("Id","EnrollmentId","ClassId","AcademicYearId","SchoolId",
                      "ClassSubjectTeacherId","SubjectId","TeacherId","GradingPeriodId",
                      "FirstMark","SecondMark")
  SELECT gen_random_uuid(), enr, a."ClassId", c."AcademicYearId", a."SchoolId",
         a."Id", a."SubjectId", a."TeacherId", term, first_mark, second_mark
  FROM "ClassSubjectTeachers" a
  JOIN "Classes" c ON c."Id" = a."ClassId"
  WHERE a."Id" = cst;
END $f$;

CREATE FUNCTION sd_lesson(cst uuid, on_date date, slot int, topic text, absent_every_n int DEFAULT 0)
RETURNS uuid LANGUAGE plpgsql AS $f$
DECLARE
  id uuid := gen_random_uuid();
  a  record;
  i  int := 0;
  e  record;
BEGIN
  SELECT "ClassId","SubjectId","TeacherId","SchoolId" INTO a FROM "ClassSubjectTeachers" WHERE "Id" = cst;

  INSERT INTO "DiaryEntries"("Id","ClassId","SubjectId","TeacherId","SchoolId","Date","Period","Topic","Notes")
  VALUES (id, a."ClassId", a."SubjectId", a."TeacherId", a."SchoolId", on_date, slot, topic, NULL);

  FOR e IN
    SELECT "Id" FROM "Enrollments"
    WHERE "ClassId" = a."ClassId" AND "EnrolledAt" <= on_date AND ("LeftAt" IS NULL OR "LeftAt" >= on_date)
    ORDER BY "Id"
  LOOP
    i := i + 1;
    INSERT INTO "Attendances"("Id","DiaryEntryId","EnrollmentId","ClassId","SchoolId","IsPresent","IsExcused","Comment","BehaviorMark")
    VALUES (gen_random_uuid(), id, e."Id", a."ClassId", a."SchoolId",
            NOT (absent_every_n > 0 AND i % absent_every_n = 0),
            (absent_every_n > 0 AND i % absent_every_n = 0 AND i % 2 = 0),
            NULL,
            CASE WHEN i % 7 = 0 THEN 4 ELSE NULL END);
  END LOOP;
  RETURN id;
END $f$;

CREATE FUNCTION sd_guardian(school uuid, stu uuid, fn text, ln text, email text, phone text)
RETURNS void LANGUAGE plpgsql AS $f$
DECLARE gid uuid := gen_random_uuid();
BEGIN
  INSERT INTO "Guardians"("Id","UserId","NationalId","PhoneNumber")
  VALUES (gid, sd_user(fn, ln, email, 1),
          lpad((floor(random() * 1e10))::bigint::text, 10, '0'), phone);

  INSERT INTO "GuardianStudents"("Id","GuardianId","StudentId","SchoolId")
  VALUES (gen_random_uuid(), gid, stu, school);
END $f$;

-- Creates a shift with six standard 45-minute periods and returns its id.
CREATE FUNCTION sd_shift(school uuid, nm text)
RETURNS uuid LANGUAGE plpgsql AS $f$
DECLARE id uuid := gen_random_uuid();
BEGIN
  INSERT INTO "SchoolShifts"("Id","SchoolId","Name","IsActive") VALUES (id, school, nm, true);
  INSERT INTO "ShiftPeriods"("Id","SchoolId","SchoolShiftId","PeriodNumber","StartTime","EndTime") VALUES
    (gen_random_uuid(), school, id, 1, TIME '08:00', TIME '08:45'),
    (gen_random_uuid(), school, id, 2, TIME '08:50', TIME '09:35'),
    (gen_random_uuid(), school, id, 3, TIME '09:45', TIME '10:30'),
    (gen_random_uuid(), school, id, 4, TIME '10:40', TIME '11:25'),
    (gen_random_uuid(), school, id, 5, TIME '11:30', TIME '12:15'),
    (gen_random_uuid(), school, id, 6, TIME '12:20', TIME '13:05');
  RETURN id;
END $f$;

-- Places one recurring lesson in a class's timetable. School, academic year and the shift
-- period are derived from the class + period number (each school has a single seeded shift).
CREATE FUNCTION sd_sched(cls uuid, subj uuid, tch uuid, period_no int, dow int)
RETURNS void LANGUAGE plpgsql AS $f$
DECLARE sch uuid; yr uuid; per uuid;
BEGIN
  SELECT "SchoolId","AcademicYearId" INTO sch, yr FROM "Classes" WHERE "Id" = cls;
  SELECT "Id" INTO per FROM "ShiftPeriods"
    WHERE "SchoolId" = sch AND "PeriodNumber" = period_no LIMIT 1;
  INSERT INTO "ScheduleEntries"
    ("Id","SchoolId","AcademicYearId","ClassId","SubjectId","TeacherId","ShiftPeriodId","DayOfWeek")
  VALUES (gen_random_uuid(), sch, yr, cls, subj, tch, per, dow);
END $f$;

CREATE FUNCTION sd_fill_marks(
  enr uuid, cls uuid, yr uuid, base int,
  half_grade boolean DEFAULT false,
  skip_subject boolean DEFAULT false,
  first_term_only boolean DEFAULT false)
RETURNS void LANGUAGE plpgsql AS $f$
DECLARE
  a        record;
  t        record;
  si       int := 0;
  ti       int;
  f        int;
  s        int;
  n_subj   int;
BEGIN
  SELECT count(*) INTO n_subj FROM "ClassSubjectTeachers" WHERE "ClassId" = cls AND "EndedOn" IS NULL;

  FOR a IN SELECT "Id" FROM "ClassSubjectTeachers"
           WHERE "ClassId" = cls AND "EndedOn" IS NULL ORDER BY "Id"
  LOOP
    si := si + 1;
    ti := 0;

    FOR t IN SELECT "Id" FROM "GradingPeriods" WHERE "AcademicYearId" = yr ORDER BY "StartDate"
    LOOP
      ti := ti + 1;

      CONTINUE WHEN first_term_only AND ti > 1;

      CONTINUE WHEN skip_subject AND ti = 2 AND si = n_subj;

      f := 1 + ((base + si) % 5);
      s := 1 + ((base + si + ti) % 5);

      IF half_grade AND ti = 2 AND si = n_subj THEN
        PERFORM sd_mark(enr, a."Id", t."Id", f, NULL);
      ELSE
        PERFORM sd_mark(enr, a."Id", t."Id", f, s);
      END IF;
    END LOOP;
  END LOOP;
END $f$;

DO $seed$
DECLARE
  today        date := (SELECT today FROM sd_ctx);

  kosova       uuid;
  shqiperia    uuid := gen_random_uuid();
  prishtina    uuid := gen_random_uuid();
  prizren      uuid := gen_random_uuid();
  peja         uuid := gen_random_uuid();
  tirana       uuid := gen_random_uuid();

  elem         uuid := gen_random_uuid();
  junior       uuid := gen_random_uuid();
  high         uuid := gen_random_uuid();
  closed       uuid := gen_random_uuid();

  zone_qendra_prishtina uuid := gen_random_uuid();
  zone_dardania         uuid := gen_random_uuid();
  zone_qendra_prizren   uuid := gen_random_uuid();
  zone_qendra_peja      uuid := gen_random_uuid();

  e_y25 uuid; e_y26 uuid; e_y27 uuid;
  j_y26 uuid; j_y27 uuid;
  h_y24 uuid; h_y25 uuid; h_y26 uuid; h_y27 uuid;
  c_y26 uuid;

  e_alb uuid; e_mat uuid; e_nat uuid;
  j_alb uuid; j_mat uuid; j_fiz uuid; j_eng uuid;
  h_mat uuid; h_fiz uuid; h_let uuid; h_eng uuid;
  c_mat uuid;

  e_t1 uuid; e_t2 uuid;
  j_t1 uuid; j_t2 uuid;
  h_t1 uuid; h_t2 uuid;
  c_t1 uuid;

  e_c4 uuid; e_c5 uuid; e_c5b uuid;
  j_c8 uuid; j_c9 uuid; j_c9b uuid;
  h_c12_24 uuid; h_c12_25 uuid; h_c11 uuid; h_c12 uuid;
  c_c10 uuid;

  stu          uuid;
  enr          uuid;
  i            int;
  d            date;
  cst          uuid;
BEGIN
  -- The platform-owner seeder (app start) already created the state administrator
  -- and their state (default 'Kosova'). Reuse it; only create it if absent.
  SELECT "Id" INTO kosova FROM "States" WHERE "Name" = 'Kosova';
  IF kosova IS NULL THEN
    kosova := gen_random_uuid();
    INSERT INTO "States"("Id","Name") VALUES (kosova,'Kosova');
  END IF;
  INSERT INTO "States"("Id","Name") VALUES (shqiperia,'Shqiperia');

  INSERT INTO "Cities"("Id","Name","StateId") VALUES
    (prishtina,'Prishtina',kosova), (prizren,'Prizren',kosova),
    (peja,'Peja',kosova), (tirana,'Tirana',shqiperia);
  INSERT INTO "Zones"("Id","Name","CityId") VALUES
    (zone_qendra_prishtina,'Qendra',prishtina), (zone_dardania,'Dardania',prishtina),
    (zone_qendra_prizren,'Qendra',prizren), (zone_qendra_peja,'Qendra',peja);

  INSERT INTO "Schools"("Id","Name","Type","CityId","ZoneId","DeactivatedAt") VALUES
    (elem,  'Shkolla Fillore Naim Frasheri', 0, prishtina, zone_qendra_prishtina, NULL),
    (junior,'Shkolla Ismail Qemali',         1, prishtina, zone_dardania,         NULL),
    (high,  'Gjimnazi Sami Frasheri',        2, prizren,   zone_qendra_prizren,   NULL),
    (closed,'Akademia e Vjeter',             2, peja,      zone_qendra_peja,      now() - interval '30 days');

  INSERT INTO "Directorates"("Id","UserId","SchoolId") VALUES
    (gen_random_uuid(), sd_user('Arta','Krasniqi','admin.fillore@educore.com',3), elem),
    (gen_random_uuid(), sd_user('Blerim','Gashi','admin.qemali@educore.com',3), junior),
    (gen_random_uuid(), sd_user('Fisnik','Berisha','admin.gjimnazi@educore.com',3), high),
    (gen_random_uuid(), sd_user('Lume','Hoxha','admin.akademia@educore.com',3), closed);

  -- One regional administrator per active city (role 4).
  INSERT INTO "RegionalAdmins"("Id","UserId","CityId") VALUES
    (gen_random_uuid(), sd_user('Rina','Aliu','regional.prishtina@educore.com',4), prishtina),
    (gen_random_uuid(), sd_user('Besart','Doni','regional.prizren@educore.com',4), prizren),
    (gen_random_uuid(), sd_user('Leke','Nikci','regional.peja@educore.com',4), peja);

  e_y25 := sd_year(elem,'2024/2025', DATE '2024-09-01', DATE '2025-06-30');
  e_y26 := sd_year(elem,'2025/2026', DATE '2025-09-01', DATE '2026-06-30');
  e_y27 := sd_year(elem,'2026/2027', DATE '2026-09-01', DATE '2027-06-30');

  e_alb := sd_subject(elem,'Gjuhe Shqipe');
  e_mat := sd_subject(elem,'Matematike');
  e_nat := sd_subject(elem,'Dituri Natyre');

  e_t1 := sd_teacher(elem,'Vlora','Morina','vlora.morina@educore.com');
  e_t2 := sd_teacher(elem,'Agron','Zeka','agron.zeka@educore.com');

  e_c4  := sd_class(elem, e_y26, '4A', 4, e_t1, 25);
  e_c5  := sd_class(elem, e_y26, '5A', 5, e_t2, 25);
  e_c5b := sd_class(elem, e_y27, '5B', 5, e_t1, 25);

  PERFORM sd_assign(elem, e_c4, e_alb, e_t1); PERFORM sd_assign(elem, e_c4, e_mat, e_t1);
  PERFORM sd_assign(elem, e_c4, e_nat, e_t1);
  PERFORM sd_assign(elem, e_c5, e_alb, e_t2); PERFORM sd_assign(elem, e_c5, e_mat, e_t2);
  PERFORM sd_assign(elem, e_c5, e_nat, e_t2);
  PERFORM sd_assign(elem, e_c5b, e_alb, e_t1);

  FOR i IN 1..3 LOOP
    stu := sd_student(elem,'Nxenes'||i,'Pesta','e5.'||i||'@educore.com',
                      DATE '2015-04-10', 'EL5000'||i, i % 2, prishtina);
    enr := sd_enroll(elem, stu, e_c5, DATE '2025-09-01');
    PERFORM sd_fill_marks(enr, e_c5, e_y26, i);
    IF i = 1 THEN
      PERFORM sd_guardian(elem, stu,'Shpresa','Pesta','shpresa.pesta@educore.com','+383 44 111 001');
    END IF;
  END LOOP;

  stu := sd_student(elem,'Ardit','Pesta','e5.off@educore.com', DATE '2015-06-02','EL50099', 1, prishtina,
                    NULL, true);
  enr := sd_enroll(elem, stu, e_c5, DATE '2025-09-01');
  PERFORM sd_fill_marks(enr, e_c5, e_y26, 4);

  FOR i IN 1..3 LOOP
    stu := sd_student(elem,'Nxenes'||i,'Katerta','e4.'||i||'@educore.com',
                      DATE '2016-03-05','EL4000'||i,
                      CASE WHEN i = 3 THEN NULL ELSE i % 2 END,
                      CASE WHEN i = 3 THEN NULL ELSE prishtina END);
    enr := sd_enroll(elem, stu, e_c4, DATE '2025-09-01');
    PERFORM sd_fill_marks(enr, e_c4, e_y26, i + 1);
  END LOOP;

  j_y26 := sd_year(junior,'2025/2026', DATE '2025-09-01', DATE '2026-06-30');
  j_y27 := sd_year(junior,'2026/2027', DATE '2026-08-15', DATE '2027-05-31');

  j_alb := sd_subject(junior,'Gjuhe Shqipe');
  j_mat := sd_subject(junior,'Matematike');
  j_fiz := sd_subject(junior,'Fizike');
  j_eng := sd_subject(junior,'Anglisht');

  j_t1 := sd_teacher(junior,'Teuta','Rexhepi','teuta.rexhepi@educore.com');
  j_t2 := sd_teacher(junior,'Driton','Maloku','driton.maloku@educore.com');

  j_c8  := sd_class(junior, j_y26, '8A', 8, j_t1, 28);
  j_c9  := sd_class(junior, j_y26, '9A', 9, j_t2, 28);
  j_c9b := sd_class(junior, j_y27, '9B', 9, j_t1, 28);

  PERFORM sd_assign(junior, j_c8, j_alb, j_t1); PERFORM sd_assign(junior, j_c8, j_mat, j_t2);
  PERFORM sd_assign(junior, j_c9, j_alb, j_t1); PERFORM sd_assign(junior, j_c9, j_mat, j_t2);
  PERFORM sd_assign(junior, j_c9b, j_fiz, j_t1); PERFORM sd_assign(junior, j_c9b, j_eng, j_t2);

  FOR i IN 1..3 LOOP
    stu := sd_student(junior,'Nxenes'||i,'Nenta','j9.'||i||'@educore.com',
                      DATE '2011-05-14','JR9000'||i, i % 2, prishtina);
    enr := sd_enroll(junior, stu, j_c9, DATE '2025-09-01');
    PERFORM sd_fill_marks(enr, j_c9, j_y26, i);
  END LOOP;

  stu := sd_student(junior,'Endrit','Larguar','j9.withdrawn@educore.com',
                    DATE '2011-08-21','JR90099', 1, prishtina);
  enr := sd_enroll(junior, stu, j_c9, DATE '2025-09-01', DATE '2026-01-20', 1);
  PERFORM sd_fill_marks(enr, j_c9, j_y26, 2, false, false, true);

  FOR i IN 1..3 LOOP
    stu := sd_student(junior,'Nxenes'||i,'Teta','j8.'||i||'@educore.com',
                      DATE '2012-02-11','JR8000'||i, i % 2, prishtina);
    enr := sd_enroll(junior, stu, j_c8, DATE '2025-09-01');
    PERFORM sd_fill_marks(enr, j_c8, j_y26, i + 2);
  END LOOP;

  FOR i IN 1..4 LOOP
    stu := sd_student(junior,'Nxenes'||i,'Tanishem','j9b.'||i||'@educore.com',
                      DATE '2011-11-03','JR9B00'||i, i % 2, prishtina);
    enr := sd_enroll(junior, stu, j_c9b, DATE '2026-08-15');
    PERFORM sd_fill_marks(enr, j_c9b, j_y27, i, false, false, true);
  END LOOP;

  h_y24 := sd_year(high,'2023/2024', DATE '2023-09-01', DATE '2024-06-30');
  h_y25 := sd_year(high,'2024/2025', DATE '2024-09-01', DATE '2025-06-30');
  h_y26 := sd_year(high,'2025/2026', DATE '2025-09-01', DATE '2026-06-30');
  h_y27 := sd_year(high,'2026/2027', DATE '2026-09-01', DATE '2027-06-30');

  h_mat := sd_subject(high,'Matematike');
  h_fiz := sd_subject(high,'Fizike');
  h_let := sd_subject(high,'Letersi');
  h_eng := sd_subject(high,'Anglisht');

  h_t1 := sd_teacher(high,'Shpend','Kastrati','shpend.kastrati@educore.com');
  h_t2 := sd_teacher(high,'Rina','Dervishi','rina.dervishi@educore.com');

  h_c12_24 := sd_class(high, h_y24,'12A',12, h_t1, 30);
  h_c12_25 := sd_class(high, h_y25,'12A',12, h_t1, 30);
  h_c11    := sd_class(high, h_y26,'11A',11, h_t2, 30);
  h_c12    := sd_class(high, h_y26,'12A',12, h_t1, 30);

  PERFORM sd_assign(high, h_c12_24, h_mat, h_t1); PERFORM sd_assign(high, h_c12_24, h_let, h_t2);
  PERFORM sd_assign(high, h_c12_25, h_mat, h_t1); PERFORM sd_assign(high, h_c12_25, h_let, h_t2);
  PERFORM sd_assign(high, h_c11, h_mat, h_t1);    PERFORM sd_assign(high, h_c11, h_fiz, h_t1);
  PERFORM sd_assign(high, h_c11, h_let, h_t2);    PERFORM sd_assign(high, h_c11, h_eng, h_t2);
  PERFORM sd_assign(high, h_c12, h_mat, h_t1);    PERFORM sd_assign(high, h_c12, h_fiz, h_t1);
  PERFORM sd_assign(high, h_c12, h_let, h_t2);    PERFORM sd_assign(high, h_c12, h_eng, h_t2);

  FOR i IN 1..3 LOOP
    stu := sd_student(high,'Brezi'||i,'Njezetkater','h24.'||i||'@educore.com',
                      DATE '2006-01-15','HS2400'||i, i % 2, prizren, DATE '2024-06-30');
    enr := sd_enroll(high, stu, h_c12_24, DATE '2023-09-01', DATE '2024-06-30', 2);
    PERFORM sd_fill_marks(enr, h_c12_24, h_y24, i);
  END LOOP;

  FOR i IN 1..3 LOOP
    stu := sd_student(high,'Brezi'||i,'Njezetpese','h25.'||i||'@educore.com',
                      DATE '2007-02-20','HS2500'||i, i % 2, prizren, DATE '2025-06-30');
    enr := sd_enroll(high, stu, h_c12_25, DATE '2024-09-01', DATE '2025-06-30', 2);
    PERFORM sd_fill_marks(enr, h_c12_25, h_y25, i + 1);
  END LOOP;

  FOR i IN 1..4 LOOP
    stu := sd_student(high,'Nxenes'||i,'Njembedhjete','h11.'||i||'@educore.com',
                      DATE '2009-07-07','HS1100'||i, i % 2, prizren);
    enr := sd_enroll(high, stu, h_c11, DATE '2025-09-01');
    PERFORM sd_fill_marks(enr, h_c11, h_y26, i);
  END LOOP;

  FOR i IN 1..4 LOOP
    stu := sd_student(high,'Gati'||i,'Dymbedhjete','h12.ready'||i||'@educore.com',
                      DATE '2008-09-19','HS12R0'||i, i % 2, prizren);
    enr := sd_enroll(high, stu, h_c12, DATE '2025-09-01');
    PERFORM sd_fill_marks(enr, h_c12, h_y26, i);
    IF i <= 2 THEN
      PERFORM sd_guardian(high, stu,'Kujtim','Dymbedhjete',
                          'kujtim.dymbedhjete'||i||'@educore.com','+383 44 222 00'||i);
    END IF;
  END LOOP;

  stu := sd_student(high,'Gjysme','Dymbedhjete','h12.half@educore.com',
                    DATE '2008-10-01','HS12H01', 0, prizren);
  enr := sd_enroll(high, stu, h_c12, DATE '2025-09-01');
  PERFORM sd_fill_marks(enr, h_c12, h_y26, 2, true);

  stu := sd_student(high,'Mungon','Dymbedhjete','h12.missing@educore.com',
                    DATE '2008-11-11','HS12M01', 1, prizren);
  enr := sd_enroll(high, stu, h_c12, DATE '2025-09-01');
  PERFORM sd_fill_marks(enr, h_c12, h_y26, 3, false, true);

  stu := sd_student(high,'Sapo','Diplomuar','h12.graduated@educore.com',
                    DATE '2008-05-05','HS12G01', 0, prizren, DATE '2026-06-30');
  enr := sd_enroll(high, stu, h_c12, DATE '2025-09-01', DATE '2026-06-30', 2);
  PERFORM sd_fill_marks(enr, h_c12, h_y26, 1);

  stu := sd_student(high,'Transferuar','Dymbedhjete','h12.transferred@educore.com',
                    DATE '2008-03-03','HS12T01', 1, tirana);
  enr := sd_enroll(high, stu, h_c12, DATE '2025-09-01', DATE '2026-02-01', 0);
  PERFORM sd_fill_marks(enr, h_c12, h_y26, 4, false, false, true);

  c_y26 := sd_year(closed,'2025/2026', DATE '2025-09-01', DATE '2026-06-30');
  c_mat := sd_subject(closed,'Matematike');
  c_t1  := sd_teacher(closed,'Valon','Krasniqi','valon.krasniqi@educore.com');
  c_c10 := sd_class(closed, c_y26,'10A',10, c_t1, 20);
  PERFORM sd_assign(closed, c_c10, c_mat, c_t1);

  FOR i IN 1..2 LOOP
    stu := sd_student(closed,'Nxenes'||i,'Mbyllur','c10.'||i||'@educore.com',
                      DATE '2010-04-04','CL1000'||i, i % 2, peja);
    enr := sd_enroll(closed, stu, c_c10, DATE '2025-09-01');
    PERFORM sd_fill_marks(enr, c_c10, c_y26, i);
  END LOOP;

  -- Timetables ------------------------------------------------------------
  -- One morning shift (6 periods) per active school, then a weekly plan for one
  -- class in each. Cells are all distinct, so no class- or teacher-slot clashes.
  PERFORM sd_shift(elem,   'Turni i paradites');
  PERFORM sd_shift(junior, 'Turni i paradites');
  PERFORM sd_shift(high,   'Turni i paradites');

  -- Elementary 5A (all three subjects taught by e_t2). Guardian Shpresa follows a 5A pupil.
  PERFORM sd_sched(e_c5, e_alb, e_t2, 1, 1); PERFORM sd_sched(e_c5, e_mat, e_t2, 2, 1);
  PERFORM sd_sched(e_c5, e_nat, e_t2, 3, 1);
  PERFORM sd_sched(e_c5, e_mat, e_t2, 1, 2); PERFORM sd_sched(e_c5, e_alb, e_t2, 2, 2);
  PERFORM sd_sched(e_c5, e_nat, e_t2, 1, 3); PERFORM sd_sched(e_c5, e_mat, e_t2, 2, 3);
  PERFORM sd_sched(e_c5, e_alb, e_t2, 1, 4); PERFORM sd_sched(e_c5, e_nat, e_t2, 2, 4);
  PERFORM sd_sched(e_c5, e_mat, e_t2, 1, 5);

  -- Junior 9A (Gjuhe Shqipe = j_t1, Matematike = j_t2).
  PERFORM sd_sched(j_c9, j_alb, j_t1, 1, 1); PERFORM sd_sched(j_c9, j_mat, j_t2, 2, 1);
  PERFORM sd_sched(j_c9, j_mat, j_t2, 1, 2); PERFORM sd_sched(j_c9, j_alb, j_t1, 2, 2);
  PERFORM sd_sched(j_c9, j_alb, j_t1, 1, 3); PERFORM sd_sched(j_c9, j_mat, j_t2, 2, 3);
  PERFORM sd_sched(j_c9, j_mat, j_t2, 1, 4);
  PERFORM sd_sched(j_c9, j_alb, j_t1, 1, 5);

  -- High 12A (Mat/Fiz = h_t1, Let/Eng = h_t2). Guardian Kujtim follows two 12A pupils.
  PERFORM sd_sched(h_c12, h_mat, h_t1, 1, 1); PERFORM sd_sched(h_c12, h_let, h_t2, 2, 1);
  PERFORM sd_sched(h_c12, h_fiz, h_t1, 3, 1); PERFORM sd_sched(h_c12, h_eng, h_t2, 4, 1);
  PERFORM sd_sched(h_c12, h_fiz, h_t1, 1, 2); PERFORM sd_sched(h_c12, h_eng, h_t2, 2, 2);
  PERFORM sd_sched(h_c12, h_mat, h_t1, 3, 2); PERFORM sd_sched(h_c12, h_let, h_t2, 4, 2);
  PERFORM sd_sched(h_c12, h_let, h_t2, 1, 3); PERFORM sd_sched(h_c12, h_mat, h_t1, 2, 3);
  PERFORM sd_sched(h_c12, h_eng, h_t2, 1, 4); PERFORM sd_sched(h_c12, h_fiz, h_t1, 2, 4);
  PERFORM sd_sched(h_c12, h_mat, h_t1, 1, 5); PERFORM sd_sched(h_c12, h_let, h_t2, 2, 5);

  -- Cross-school guardian: one national ID, children in two different schools.
  DECLARE
    gx  uuid := gen_random_uuid();
    s1  uuid; sc1 uuid; s2 uuid; sc2 uuid;
  BEGIN
    SELECT "Id","SchoolId" INTO s1, sc1 FROM "Students" WHERE "PersonalNumber" = 'EL50002';
    SELECT "Id","SchoolId" INTO s2, sc2 FROM "Students" WHERE "PersonalNumber" = 'HS11001';

    INSERT INTO "Guardians"("Id","UserId","NationalId","PhoneNumber")
    VALUES (gx, sd_user('Fatmir','Berisha','fatmir.berisha@educore.com',1), '1010101010', '+383 44 900 900');

    INSERT INTO "GuardianStudents"("Id","GuardianId","StudentId","SchoolId") VALUES
      (gen_random_uuid(), gx, s1, sc1),
      (gen_random_uuid(), gx, s2, sc2);
  END;

  FOR cst IN
    SELECT a."Id"
    FROM "ClassSubjectTeachers" a
    JOIN "Classes" c ON c."Id" = a."ClassId"
    JOIN "AcademicYears" y ON y."Id" = c."AcademicYearId"
    WHERE y."StartDate" <= today
    ORDER BY a."ClassId", a."Id"
  LOOP
    DECLARE
      slot       int;
      year_start date;
      year_end   date;
    BEGIN
      SELECT t.ys, t.ye, t.s INTO year_start, year_end, slot
      FROM (
        SELECT y."StartDate" AS ys, y."EndDate" AS ye, a."Id" AS aid,
               row_number() OVER (PARTITION BY a."ClassId" ORDER BY a."Id") AS s
        FROM "ClassSubjectTeachers" a
        JOIN "Classes" c ON c."Id" = a."ClassId"
        JOIN "AcademicYears" y ON y."Id" = c."AcademicYearId"
      ) t
      WHERE t.aid = cst;

      FOR i IN 0..5 LOOP
        d := year_start + (7 * i + 2);

        CONTINUE WHEN d > today OR d > year_end;

        PERFORM sd_lesson(cst, d, slot, 'Mesimi ' || (i + 1),
                          CASE WHEN i % 2 = 0 THEN 3 ELSE 4 END);
      END LOOP;
    END;
  END LOOP;
END $seed$;

DROP FUNCTION sd_sched(uuid,uuid,uuid,int,int);
DROP FUNCTION sd_shift(uuid,text);
DROP FUNCTION sd_fill_marks(uuid,uuid,uuid,int,boolean,boolean,boolean);
DROP FUNCTION sd_lesson(uuid,date,int,text,int);
DROP FUNCTION sd_guardian(uuid,uuid,text,text,text,text);
DROP FUNCTION sd_mark(uuid,uuid,uuid,int,int);
DROP FUNCTION sd_enroll(uuid,uuid,uuid,date,date,int);
DROP FUNCTION sd_student(uuid,text,text,text,date,text,int,uuid,date,boolean);
DROP FUNCTION sd_assign(uuid,uuid,uuid,uuid);
DROP FUNCTION sd_class(uuid,uuid,text,int,uuid,int);
DROP FUNCTION sd_subject(uuid,text);
DROP FUNCTION sd_teacher(uuid,text,text,text);
DROP FUNCTION sd_year(uuid,text,date,date);
DROP FUNCTION sd_user(text,text,text,int,boolean);

-- The platform owner (StateAdmin, role 5) was seeded before this script ran; give
-- it the same "SuperAdmin1234!" password so every account uses one credential.
UPDATE "Users"
SET "PasswordHash" = (SELECT hash FROM sd_ctx), "MustChangePassword" = false
WHERE "Role" = 5;

DROP TABLE sd_ctx;

COMMIT;
