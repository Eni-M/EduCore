using EduCore.Models;
using EduCore.Security;
using EduCore.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EduCore.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser, IFieldEncryptor encryptor)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<Directorate> Directorates => Set<Directorate>();
    public DbSet<RegionalAdmin> RegionalAdmins => Set<RegionalAdmin>();
    public DbSet<StateAdmin> StateAdmins => Set<StateAdmin>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<GuardianStudent> GuardianStudents => Set<GuardianStudent>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<GradingPeriod> GradingPeriods => Set<GradingPeriod>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<SchoolingHistory> SchoolingHistories => Set<SchoolingHistory>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<StudentMedicalRecord> StudentMedicalRecords => Set<StudentMedicalRecord>();
    public DbSet<StudentAcademicRecord> StudentAcademicRecords => Set<StudentAcademicRecord>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<StudentAcademicRecordSubject> StudentAcademicRecordSubjects =>
        Set<StudentAcademicRecordSubject>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<OriginCity> OriginCities => Set<OriginCity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<ClassSubjectTeacher> ClassSubjectTeachers => Set<ClassSubjectTeacher>();
    public DbSet<GradeCurriculum> GradeCurricula => Set<GradeCurriculum>();
    public DbSet<SchoolStatisticsSnapshot> SchoolStatisticsSnapshots => Set<SchoolStatisticsSnapshot>();

    public DbSet<SchoolShift> SchoolShifts => Set<SchoolShift>();
    public DbSet<ShiftPeriod> ShiftPeriods => Set<ShiftPeriod>();
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureSchoolCalendar(modelBuilder);
        ConfigureClasses(modelBuilder);
        ConfigureLessons(modelBuilder);
        ConfigureScheduling(modelBuilder);
        ConfigureStudentRecords(modelBuilder);
        ConfigureCommunication(modelBuilder);
        ConfigureAssignments(modelBuilder);
        ConfigureMessaging(modelBuilder);

        modelBuilder.Entity<AuditLog>().Property(a => a.Action).HasMaxLength(1000);
        modelBuilder.Entity<AuditLog>().Property(a => a.TraceId).HasMaxLength(200);

        // The admin listing sorts Timestamp DESC, Id and pages 20 rows. Without this the
        // dominant unfiltered case is a parallel sequential scan over the whole table followed
        // by a top-N sort — measured at 1177 ms cold on 170k rows, to return twenty of them.
        // Descending to match the sort, with Id as the tie-break the query already applies.
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.Timestamp, a.Id })
            .IsDescending(true, false);
    }

    private void ScopeToSchool<T>(ModelBuilder modelBuilder) where T : class, ISchoolScoped =>
        modelBuilder.Entity<T>().HasQueryFilter(e => currentUser.SchoolIds.Contains(e.SchoolId));

    private void ConfigureUsers(ModelBuilder modelBuilder)
    {
        // Encrypt national identifiers at rest. The conversion is deterministic, so the unique
        // indexes on these columns and the equality lookups (PersonalNumberExists / NationalIdExists)
        // keep working: EF applies the same converter to the compared value in a WHERE clause.
        var encrypted = new ValueConverter<string, string>(
            v => encryptor.Encrypt(v),
            v => encryptor.Decrypt(v));

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().Property(u => u.FirstName).HasMaxLength(100);
        modelBuilder.Entity<User>().Property(u => u.LastName).HasMaxLength(100);
        modelBuilder.Entity<User>().Property(u => u.Email).HasMaxLength(320);
        modelBuilder.Entity<User>().Property(u => u.PasswordHash).HasMaxLength(500);
        modelBuilder.Entity<User>().Property<uint>("Version").IsRowVersion();

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(t => t.User).WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        // Redemption looks a token up by its hash and nothing else, so this index is the
        // whole read path. Unique because two live tokens must never collide.
        modelBuilder.Entity<PasswordResetToken>().HasIndex(t => t.TokenHash).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().Property(t => t.TokenHash).HasMaxLength(64);
        // Backs both "invalidate this user's outstanding tokens" and the expiry sweep.
        modelBuilder.Entity<PasswordResetToken>().HasIndex(t => new { t.UserId, t.ExpiresAt });

        modelBuilder.Entity<Student>()
            .HasOne(s => s.User).WithOne()
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Student>().HasIndex(s => s.UserId).IsUnique();
        modelBuilder.Entity<Student>()
            .HasOne(s => s.School).WithMany()
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Student>().HasAlternateKey(s => new { s.Id, s.SchoolId });
        modelBuilder.Entity<Student>().HasIndex(s => s.PersonalNumber).IsUnique();
        modelBuilder.Entity<Student>().Property(s => s.PersonalNumber).HasMaxLength(256).HasConversion(encrypted);
        modelBuilder.Entity<Student>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Student>(modelBuilder);

        modelBuilder.Entity<Guardian>()
            .HasOne(g => g.User).WithOne()
            .HasForeignKey<Guardian>(g => g.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Guardian>().HasIndex(g => g.UserId).IsUnique();
        modelBuilder.Entity<Guardian>().Property(g => g.NationalId).HasMaxLength(256).HasConversion(encrypted);
        modelBuilder.Entity<Guardian>().HasIndex(g => g.NationalId).IsUnique();
        modelBuilder.Entity<Guardian>().Property(g => g.PhoneNumber).HasMaxLength(30);
        modelBuilder.Entity<Guardian>().Property<uint>("Version").IsRowVersion();

        modelBuilder.Entity<GuardianStudent>()
            .HasOne(gs => gs.Guardian).WithMany(g => g.Children)
            .HasForeignKey(gs => gs.GuardianId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GuardianStudent>()
            .HasOne(gs => gs.Student).WithMany(s => s.Guardians)
            .HasForeignKey(gs => new { gs.StudentId, gs.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GuardianStudent>().HasIndex(gs => new { gs.GuardianId, gs.StudentId }).IsUnique();
        modelBuilder.Entity<GuardianStudent>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<GuardianStudent>(modelBuilder);

        modelBuilder.Entity<Teacher>()
            .HasOne(t => t.User).WithOne()
            .HasForeignKey<Teacher>(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Teacher>().HasIndex(t => t.UserId).IsUnique();
        modelBuilder.Entity<Teacher>()
            .HasOne(t => t.School).WithMany()
            .HasForeignKey(t => t.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Teacher>().HasAlternateKey(t => new { t.Id, t.SchoolId });
        modelBuilder.Entity<Teacher>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Teacher>(modelBuilder);

        modelBuilder.Entity<Directorate>()
            .HasOne(a => a.User).WithOne()
            .HasForeignKey<Directorate>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Directorate>().HasIndex(a => a.UserId).IsUnique();
        modelBuilder.Entity<Directorate>()
            .HasOne(a => a.School).WithMany()
            .HasForeignKey(a => a.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Directorate>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Directorate>(modelBuilder);

        modelBuilder.Entity<RegionalAdmin>()
            .HasOne(r => r.User).WithOne()
            .HasForeignKey<RegionalAdmin>(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RegionalAdmin>().HasIndex(r => r.UserId).IsUnique();
        modelBuilder.Entity<RegionalAdmin>()
            .HasOne(r => r.City).WithMany()
            .HasForeignKey(r => r.CityId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RegionalAdmin>().Property<uint>("Version").IsRowVersion();

        modelBuilder.Entity<StateAdmin>()
            .HasOne(s => s.User).WithOne()
            .HasForeignKey<StateAdmin>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StateAdmin>().HasIndex(s => s.UserId).IsUnique();
        modelBuilder.Entity<StateAdmin>()
            .HasOne(s => s.State).WithMany()
            .HasForeignKey(s => s.StateId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StateAdmin>().Property<uint>("Version").IsRowVersion();
    }

    private void ConfigureSchoolCalendar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<School>().Property(s => s.Name).HasMaxLength(200);
        modelBuilder.Entity<School>().Property<uint>("Version").IsRowVersion();

        modelBuilder.Entity<AcademicYear>()
            .HasOne<School>().WithMany()
            .HasForeignKey(a => a.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AcademicYear>().HasAlternateKey(a => new { a.Id, a.SchoolId });
        modelBuilder.Entity<AcademicYear>().HasIndex(a => new { a.SchoolId, a.Name }).IsUnique();
        modelBuilder.Entity<AcademicYear>().Property(a => a.Name).HasMaxLength(50);
        modelBuilder.Entity<AcademicYear>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<AcademicYear>(modelBuilder);

        modelBuilder.Entity<GradingPeriod>()
            .HasOne<AcademicYear>().WithMany()
            .HasForeignKey(g => new { g.AcademicYearId, g.SchoolId })
            .HasPrincipalKey(a => new { a.Id, a.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GradingPeriod>().HasAlternateKey(g => new { g.Id, g.AcademicYearId, g.SchoolId });
        modelBuilder.Entity<GradingPeriod>().HasIndex(g => new { g.AcademicYearId, g.Name }).IsUnique();
        modelBuilder.Entity<GradingPeriod>().Property(g => g.Name).HasMaxLength(100);
        modelBuilder.Entity<GradingPeriod>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<GradingPeriod>(modelBuilder);

        modelBuilder.Entity<Subject>()
            .HasOne<School>().WithMany()
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Subject>().HasAlternateKey(s => new { s.Id, s.SchoolId });
        modelBuilder.Entity<Subject>().HasIndex(s => new { s.SchoolId, s.Name }).IsUnique();
        modelBuilder.Entity<Subject>().Property(s => s.Name).HasMaxLength(100);
        modelBuilder.Entity<Subject>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Subject>(modelBuilder);

        modelBuilder.Entity<State>().Property(s => s.Name).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<State>().HasIndex(s => s.Name).IsUnique();

        modelBuilder.Entity<City>().Property(c => c.Name).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<City>()
            .HasOne(c => c.State)
            .WithMany(s => s.Cities)
            .HasForeignKey(c => c.StateId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<City>().HasIndex(c => new { c.StateId, c.Name }).IsUnique();

        modelBuilder.Entity<Zone>().Property(z => z.Name).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<Zone>()
            .HasOne(z => z.City).WithMany()
            .HasForeignKey(z => z.CityId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Zone>().HasIndex(z => new { z.CityId, z.Name }).IsUnique();

        // Student origin — its own tables, never joined to the school State/City/Zone.
        modelBuilder.Entity<Country>().Property(c => c.Name).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<Country>().HasIndex(c => c.Name).IsUnique();

        modelBuilder.Entity<OriginCity>().Property(c => c.Name).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<OriginCity>()
            .HasOne(c => c.Country).WithMany(c => c.Cities)
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OriginCity>().HasIndex(c => new { c.CountryId, c.Name }).IsUnique();

        SeedCountries(modelBuilder);

        modelBuilder.Entity<Student>()
            .HasOne(s => s.Country).WithMany()
            .HasForeignKey(s => s.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Student>()
            .HasOne(s => s.OriginCity).WithMany()
            .HasForeignKey(s => s.OriginCityId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Student>().Property(s => s.Address).HasMaxLength(300);

        modelBuilder.Entity<School>()
            .HasOne(s => s.City).WithMany()
            .HasForeignKey(s => s.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<School>()
            .HasOne(s => s.Zone).WithMany()
            .HasForeignKey(s => s.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    // Kosovo plus the neighbours students most often come from. Seeded so the
    // origin dropdown works out of the box; more can be added by a state admin.
    private static void SeedCountries(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Country>().HasData(
            new Country { Id = new Guid("b1000000-0000-0000-0000-000000000001"), Name = "Kosovo" },
            new Country { Id = new Guid("b1000000-0000-0000-0000-000000000002"), Name = "Albania" },
            new Country { Id = new Guid("b1000000-0000-0000-0000-000000000003"), Name = "North Macedonia" },
            new Country { Id = new Guid("b1000000-0000-0000-0000-000000000004"), Name = "Serbia" },
            new Country { Id = new Guid("b1000000-0000-0000-0000-000000000005"), Name = "Montenegro" });

    private void ConfigureClasses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Class>()
            .HasOne<School>().WithMany()
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Class>()
            .HasOne<AcademicYear>().WithMany()
            .HasForeignKey(c => new { c.AcademicYearId, c.SchoolId })
            .HasPrincipalKey(a => new { a.Id, a.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Class>()
            .HasOne<Teacher>().WithMany()
            .HasForeignKey(c => c.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Class>()
            .HasOne(c => c.Shift).WithMany()
            .HasForeignKey(c => c.ShiftId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Class>().HasAlternateKey(c => new { c.Id, c.SchoolId });
        modelBuilder.Entity<Class>().HasAlternateKey(c => new { c.Id, c.AcademicYearId });
        modelBuilder.Entity<Class>().HasIndex(c => new { c.SchoolId, c.AcademicYearId, c.Name }).IsUnique();
        modelBuilder.Entity<Class>().Property(c => c.Name).HasMaxLength(100);
        modelBuilder.Entity<Class>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Class>(modelBuilder);

        modelBuilder.Entity<Enrollment>()
            .HasOne<Student>().WithMany(s => s.Enrollments)
            .HasForeignKey(e => new { e.StudentId, e.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Enrollment>()
            .HasOne<Class>().WithMany()
            .HasForeignKey(e => new { e.ClassId, e.SchoolId })
            .HasPrincipalKey(c => new { c.Id, c.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Enrollment>().HasAlternateKey(e => new { e.Id, e.ClassId });
        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.StudentId, e.ClassId })
            .IsUnique()
            .HasFilter("\"LeftAt\" IS NULL");
        modelBuilder.Entity<Enrollment>().Property<uint>("Version").IsRowVersion();
        modelBuilder.Entity<Enrollment>().HasIndex(e => new { e.SchoolId, e.ClassId, e.LeftAt });
        ScopeToSchool<Enrollment>(modelBuilder);

        modelBuilder.Entity<TeacherSubject>()
            .HasOne<Teacher>().WithMany(t => t.TeacherSubjects)
            .HasForeignKey(ts => new { ts.TeacherId, ts.SchoolId })
            .HasPrincipalKey(t => new { t.Id, t.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Subject).WithMany()
            .HasForeignKey(ts => new { ts.SubjectId, ts.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TeacherSubject>().HasAlternateKey(ts => new { ts.TeacherId, ts.SubjectId, ts.SchoolId });
        modelBuilder.Entity<TeacherSubject>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<TeacherSubject>(modelBuilder);

        modelBuilder.Entity<ClassSubjectTeacher>()
            .HasOne<Class>().WithMany()
            .HasForeignKey(cst => new { cst.ClassId, cst.SchoolId })
            .HasPrincipalKey(c => new { c.Id, c.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClassSubjectTeacher>()
            .HasOne<TeacherSubject>().WithMany()
            .HasForeignKey(cst => new { cst.TeacherId, cst.SubjectId, cst.SchoolId })
            .HasPrincipalKey(ts => new { ts.TeacherId, ts.SubjectId, ts.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClassSubjectTeacher>()
            .HasAlternateKey(cst => new { cst.Id, cst.ClassId, cst.SubjectId, cst.TeacherId, cst.SchoolId });
        modelBuilder.Entity<ClassSubjectTeacher>()
            .HasIndex(cst => new { cst.ClassId, cst.SubjectId })
            .IsUnique()
            .HasFilter("\"EndedOn\" IS NULL");
        modelBuilder.Entity<ClassSubjectTeacher>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<ClassSubjectTeacher>(modelBuilder);

        modelBuilder.Entity<GradeCurriculum>()
            .HasOne(gc => gc.Subject).WithMany()
            .HasForeignKey(gc => gc.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<GradeCurriculum>()
            .HasIndex(gc => new { gc.SchoolId, gc.Grade, gc.SubjectId }).IsUnique();
        modelBuilder.Entity<GradeCurriculum>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<GradeCurriculum>(modelBuilder);

        // One row per (school, window). The lookup is always by the whole key, and it must be
        // unique or two concurrent recomputes would leave duplicates that disagree.
        modelBuilder.Entity<SchoolStatisticsSnapshot>()
            .HasIndex(s => new { s.SchoolId, s.From, s.To, s.AcademicYearId, s.GradingPeriodId })
            .IsUnique();
        // Deliberately no row version: a snapshot is derived data. Two requests racing to
        // refresh the same row both write the same numbers, so last-writer-wins is correct
        // and a concurrency exception would only fail a read for no benefit.
        ScopeToSchool<SchoolStatisticsSnapshot>(modelBuilder);
    }

    private void ConfigureScheduling(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchoolShift>()
            .HasOne<School>().WithMany()
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SchoolShift>().HasAlternateKey(s => new { s.Id, s.SchoolId });
        modelBuilder.Entity<SchoolShift>().HasIndex(s => new { s.SchoolId, s.Name }).IsUnique();
        modelBuilder.Entity<SchoolShift>().Property(s => s.Name).HasMaxLength(100);
        modelBuilder.Entity<SchoolShift>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<SchoolShift>(modelBuilder);

        modelBuilder.Entity<ShiftPeriod>()
            .HasOne(p => p.Shift).WithMany(s => s.Periods)
            .HasForeignKey(p => new { p.SchoolShiftId, p.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ShiftPeriod>().HasAlternateKey(p => new { p.Id, p.SchoolId });
        modelBuilder.Entity<ShiftPeriod>().HasIndex(p => new { p.SchoolShiftId, p.PeriodNumber }).IsUnique();
        modelBuilder.Entity<ShiftPeriod>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<ShiftPeriod>(modelBuilder);

        modelBuilder.Entity<ScheduleEntry>()
            .HasOne<AcademicYear>().WithMany()
            .HasForeignKey(e => new { e.AcademicYearId, e.SchoolId })
            .HasPrincipalKey(a => new { a.Id, a.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScheduleEntry>()
            .HasOne<Class>().WithMany()
            .HasForeignKey(e => new { e.ClassId, e.SchoolId })
            .HasPrincipalKey(c => new { c.Id, c.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScheduleEntry>()
            .HasOne<Subject>().WithMany()
            .HasForeignKey(e => new { e.SubjectId, e.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        // Teacher must be qualified for the subject -- reuse TeacherSubject as the source of truth.
        modelBuilder.Entity<ScheduleEntry>()
            .HasOne<TeacherSubject>().WithMany()
            .HasForeignKey(e => new { e.TeacherId, e.SubjectId, e.SchoolId })
            .HasPrincipalKey(ts => new { ts.TeacherId, ts.SubjectId, ts.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScheduleEntry>()
            .HasOne<ShiftPeriod>().WithMany()
            .HasForeignKey(e => new { e.ShiftPeriodId, e.SchoolId })
            .HasPrincipalKey(p => new { p.Id, p.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        // One lesson per class per slot.
        modelBuilder.Entity<ScheduleEntry>()
            .HasIndex(e => new { e.SchoolId, e.AcademicYearId, e.ClassId, e.DayOfWeek, e.ShiftPeriodId })
            .IsUnique();
        // A teacher cannot be in two places at once.
        // slot-based conflict (period, not clock time). Upgrade to time-range overlap
        // only if a school runs shifts whose periods overlap in wall-clock time.
        modelBuilder.Entity<ScheduleEntry>()
            .HasIndex(e => new { e.SchoolId, e.AcademicYearId, e.TeacherId, e.DayOfWeek, e.ShiftPeriodId })
            .IsUnique();
        modelBuilder.Entity<ScheduleEntry>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<ScheduleEntry>(modelBuilder);
    }

    private void ConfigureLessons(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiaryEntry>()
            .HasOne<Class>().WithMany()
            .HasForeignKey(d => new { d.ClassId, d.SchoolId })
            .HasPrincipalKey(c => new { c.Id, c.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DiaryEntry>()
            .HasOne<Subject>().WithMany()
            .HasForeignKey(d => new { d.SubjectId, d.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DiaryEntry>()
            .HasOne<TeacherSubject>().WithMany()
            .HasForeignKey(d => new { d.TeacherId, d.SubjectId, d.SchoolId })
            .HasPrincipalKey(ts => new { ts.TeacherId, ts.SubjectId, ts.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DiaryEntry>().HasAlternateKey(d => new { d.Id, d.ClassId });
        modelBuilder.Entity<DiaryEntry>().HasIndex(d => new { d.ClassId, d.Date, d.Period }).IsUnique();
        modelBuilder.Entity<DiaryEntry>().HasIndex(d => new { d.SchoolId, d.Date });
        modelBuilder.Entity<DiaryEntry>().Property(d => d.Topic).HasMaxLength(500);
        modelBuilder.Entity<DiaryEntry>().Property(d => d.Notes).HasMaxLength(4000);
        modelBuilder.Entity<DiaryEntry>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<DiaryEntry>(modelBuilder);

        modelBuilder.Entity<Attendance>()
            .HasOne<Enrollment>().WithMany()
            .HasForeignKey(a => new { a.EnrollmentId, a.ClassId })
            .HasPrincipalKey(e => new { e.Id, e.ClassId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Attendance>()
            .HasOne<DiaryEntry>().WithMany()
            .HasForeignKey(a => new { a.DiaryEntryId, a.ClassId })
            .HasPrincipalKey(d => new { d.Id, d.ClassId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Attendance>()
            .HasOne<Class>().WithMany()
            .HasForeignKey(a => new { a.ClassId, a.SchoolId })
            .HasPrincipalKey(c => new { c.Id, c.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Attendance>().HasIndex(a => new { a.DiaryEntryId, a.EnrollmentId }).IsUnique();
        modelBuilder.Entity<Attendance>().Property(a => a.Comment).HasMaxLength(2000);
        modelBuilder.Entity<Attendance>().Property<uint>("Version").IsRowVersion();
        modelBuilder.Entity<Attendance>().HasIndex(a => a.EnrollmentId);
        ScopeToSchool<Attendance>(modelBuilder);

        modelBuilder.Entity<Mark>()
            .HasOne<Enrollment>().WithMany()
            .HasForeignKey(m => new { m.EnrollmentId, m.ClassId })
            .HasPrincipalKey(e => new { e.Id, e.ClassId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Mark>()
            .HasOne<Class>().WithMany()
            .HasForeignKey(m => new { m.ClassId, m.SchoolId })
            .HasPrincipalKey(c => new { c.Id, c.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Mark>()
            .HasOne<Class>().WithMany()
            .HasForeignKey(m => new { m.ClassId, m.AcademicYearId })
            .HasPrincipalKey(c => new { c.Id, c.AcademicYearId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Mark>()
            .HasOne<ClassSubjectTeacher>().WithMany()
            .HasForeignKey(m => new { m.ClassSubjectTeacherId, m.ClassId, m.SubjectId, m.TeacherId, m.SchoolId })
            .HasPrincipalKey(cst => new { cst.Id, cst.ClassId, cst.SubjectId, cst.TeacherId, cst.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Mark>()
            .HasOne<GradingPeriod>().WithMany()
            .HasForeignKey(m => new { m.GradingPeriodId, m.AcademicYearId, m.SchoolId })
            .HasPrincipalKey(g => new { g.Id, g.AcademicYearId, g.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Mark>().Property<uint>("Version").IsRowVersion();
        modelBuilder.Entity<Mark>()
            .HasIndex(m => new { m.EnrollmentId, m.SubjectId, m.GradingPeriodId })
            .IsUnique();
        modelBuilder.Entity<Mark>().HasIndex(m => new { m.SchoolId, m.GradingPeriodId, m.SubjectId });
        modelBuilder.Entity<Mark>().HasIndex(m => new { m.TeacherId, m.GradingPeriodId });
        ScopeToSchool<Mark>(modelBuilder);
    }

    // Records that hang off a single student. All three use the same composite (StudentId,
    // SchoolId) -> Student(Id, SchoolId) key the rest of the model uses, so a row can never point
    // at a student in another school, and Restrict keeps the history from vanishing under a delete.
    private void ConfigureStudentRecords(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchoolingHistory>()
            .HasOne<Student>().WithMany()
            .HasForeignKey(h => new { h.StudentId, h.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SchoolingHistory>().HasIndex(h => new { h.SchoolId, h.StudentId });
        modelBuilder.Entity<SchoolingHistory>().Property(h => h.PreviousSchoolName).HasMaxLength(200);
        modelBuilder.Entity<SchoolingHistory>().Property(h => h.City).HasMaxLength(150);
        modelBuilder.Entity<SchoolingHistory>().Property(h => h.GradeOrLevel).HasMaxLength(50);
        modelBuilder.Entity<SchoolingHistory>().Property(h => h.ExitReason).HasMaxLength(200);
        modelBuilder.Entity<SchoolingHistory>().Property(h => h.Notes).HasMaxLength(2000);
        modelBuilder.Entity<SchoolingHistory>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<SchoolingHistory>(modelBuilder);

        modelBuilder.Entity<StudentDocument>()
            .HasOne<Student>().WithMany()
            .HasForeignKey(d => new { d.StudentId, d.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StudentDocument>().HasIndex(d => new { d.SchoolId, d.StudentId });
        modelBuilder.Entity<StudentDocument>().Property(d => d.Title).HasMaxLength(200);
        modelBuilder.Entity<StudentDocument>().Property(d => d.FileName).HasMaxLength(260);
        modelBuilder.Entity<StudentDocument>().Property(d => d.ContentType).HasMaxLength(150);
        modelBuilder.Entity<StudentDocument>().Property(d => d.StorageKey).HasMaxLength(400);
        modelBuilder.Entity<StudentDocument>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<StudentDocument>(modelBuilder);

        // One medical record per student.
        modelBuilder.Entity<StudentMedicalRecord>()
            .HasOne<Student>().WithMany()
            .HasForeignKey(m => new { m.StudentId, m.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StudentMedicalRecord>().HasIndex(m => m.StudentId).IsUnique();
        modelBuilder.Entity<StudentMedicalRecord>().Property(m => m.BloodType).HasMaxLength(10);
        modelBuilder.Entity<StudentMedicalRecord>().Property(m => m.Allergies).HasMaxLength(2000);
        modelBuilder.Entity<StudentMedicalRecord>().Property(m => m.ChronicConditions).HasMaxLength(2000);
        modelBuilder.Entity<StudentMedicalRecord>().Property(m => m.Medications).HasMaxLength(2000);
        modelBuilder.Entity<StudentMedicalRecord>().Property(m => m.EmergencyContactName).HasMaxLength(200);
        modelBuilder.Entity<StudentMedicalRecord>().Property(m => m.EmergencyContactPhone).HasMaxLength(30);
        modelBuilder.Entity<StudentMedicalRecord>().Property(m => m.Notes).HasMaxLength(4000);
        modelBuilder.Entity<StudentMedicalRecord>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<StudentMedicalRecord>(modelBuilder);

        // Transcript rows. The only student-owned table here that is NOT school-scoped, and the
        // only one keyed on StudentId alone rather than the composite (StudentId, SchoolId).
        //
        // Both differences are the point. The composite key used above ties a row to the student's
        // *current* school; a transcript row belongs to a school the student has usually already
        // left, so that key would reject exactly the rows worth keeping. Restrict on delete, so
        // removing a student is refused while history exists rather than silently erasing it.
        modelBuilder.Entity<StudentAcademicRecord>()
            .HasOne<Student>().WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One row per student per school per year. Rebuilding a record must update the existing
        // row rather than accumulate duplicates, and this is what makes that detectable.
        modelBuilder.Entity<StudentAcademicRecord>()
            .HasIndex(r => new { r.StudentId, r.SchoolId, r.AcademicYearId })
            .IsUnique();

        // The read path is always "this student's whole history, newest first".
        modelBuilder.Entity<StudentAcademicRecord>().HasIndex(r => new { r.StudentId, r.EnrolledAt });

        modelBuilder.Entity<StudentAcademicRecord>().Property(r => r.SchoolName).HasMaxLength(200);
        modelBuilder.Entity<StudentAcademicRecord>().Property(r => r.AcademicYearName).HasMaxLength(100);
        modelBuilder.Entity<StudentAcademicRecord>().Property(r => r.ClassName).HasMaxLength(100);
        modelBuilder.Entity<StudentAcademicRecord>().Property<uint>("Version").IsRowVersion();

        // Cascade, unlike everything else in this method: a subject line has no meaning without
        // the record it belongs to, and rebuilding a record replaces its lines wholesale.
        modelBuilder.Entity<StudentAcademicRecordSubject>()
            .HasOne(s => s.Record).WithMany(r => r.Subjects)
            .HasForeignKey(s => s.StudentAcademicRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentAcademicRecordSubject>()
            .HasIndex(s => new { s.StudentAcademicRecordId, s.SubjectName });

        modelBuilder.Entity<StudentAcademicRecordSubject>().Property(s => s.SubjectName).HasMaxLength(200);
    }

    // The notice board and the shared calendar. Both are school-scoped; AuthorUserId/CreatedByUserId
    // are plain columns (resolved to a name by a join when listing) so a post outlives its author.
    private void ConfigureCommunication(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notice>()
            .HasOne<School>().WithMany()
            .HasForeignKey(n => n.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notice>().HasIndex(n => new { n.SchoolId, n.Pinned, n.CreatedAt });
        modelBuilder.Entity<Notice>().Property(n => n.Title).HasMaxLength(200);
        modelBuilder.Entity<Notice>().Property(n => n.Body).HasMaxLength(8000);
        modelBuilder.Entity<Notice>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Notice>(modelBuilder);

        // ClassId and StudentId are nullable, which makes their composite FKs optional -- a
        // School event names neither, a Class event only the class, a Student event only the
        // student. The SchoolId in each composite forces the target into the event's own school.
        modelBuilder.Entity<CalendarEvent>()
            .HasOne<School>().WithMany()
            .HasForeignKey(e => e.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CalendarEvent>()
            .HasOne<Class>().WithMany()
            .HasForeignKey(e => new { e.ClassId, e.SchoolId })
            .HasPrincipalKey(c => new { c.Id, c.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CalendarEvent>()
            .HasOne<Student>().WithMany()
            .HasForeignKey(e => new { e.StudentId, e.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CalendarEvent>().HasIndex(e => new { e.SchoolId, e.Date });
        modelBuilder.Entity<CalendarEvent>().Property(e => e.Title).HasMaxLength(200);
        modelBuilder.Entity<CalendarEvent>().Property(e => e.Description).HasMaxLength(4000);
        modelBuilder.Entity<CalendarEvent>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<CalendarEvent>(modelBuilder);

        modelBuilder.Entity<Notification>()
            .HasOne<School>().WithMany()
            .HasForeignKey(n => n.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.SchoolId, n.RecipientRole, n.ReadAt });
        modelBuilder.Entity<Notification>().Property(n => n.Message).HasMaxLength(1000);
        modelBuilder.Entity<Notification>().Property(n => n.Link).HasMaxLength(500);
        modelBuilder.Entity<Notification>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Notification>(modelBuilder);
    }


    private void ConfigureAssignments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assignment>()
            .HasAlternateKey(a => new { a.Id, a.SchoolId });

        modelBuilder.Entity<Assignment>()
            .HasOne<ClassSubjectTeacher>()
            .WithMany()
            .HasForeignKey(a => new
            {
                a.ClassSubjectTeacherId,
                a.ClassId,
                a.SubjectId,
                a.TeacherId,
                a.SchoolId
            })
            .HasPrincipalKey(cst => new
            {
                cst.Id,
                cst.ClassId,
                cst.SubjectId,
                cst.TeacherId,
                cst.SchoolId
            })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assignment>()
            .HasIndex(a => new
            {
                a.SchoolId,
                a.ClassId,
                a.SubjectId,
                a.DueDate
            });

        modelBuilder.Entity<Assignment>()
            .Property(a => a.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<Assignment>()
            .Property(a => a.Description)
            .HasMaxLength(8000);

        modelBuilder.Entity<Assignment>()
            .Property(a => a.FileName)
            .HasMaxLength(260);

        modelBuilder.Entity<Assignment>()
            .Property(a => a.ContentType)
            .HasMaxLength(150);

        modelBuilder.Entity<Assignment>()
            .Property(a => a.StorageKey)
            .HasMaxLength(400);

        modelBuilder.Entity<Assignment>()
            .Property<uint>("Version")
            .IsRowVersion();

        ScopeToSchool<Assignment>(modelBuilder);


        modelBuilder.Entity<AssignmentSubmission>()
            .HasOne<Assignment>()
            .WithMany()
            .HasForeignKey(s => new
            {
                s.AssignmentId,
                s.SchoolId
            })
            .HasPrincipalKey(a => new
            {
                a.Id,
                a.SchoolId
            })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssignmentSubmission>()
            .HasOne<Student>()
            .WithMany()
            .HasForeignKey(s => new
            {
                s.StudentId,
                s.SchoolId
            })
            .HasPrincipalKey(s => new
            {
                s.Id,
                s.SchoolId
            })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssignmentSubmission>()
            .HasIndex(s => new
            {
                s.AssignmentId,
                s.StudentId
            })
            .IsUnique();

        modelBuilder.Entity<AssignmentSubmission>()
            .HasIndex(s => new
            {
                s.SchoolId,
                s.StudentId
            });

        modelBuilder.Entity<AssignmentSubmission>()
            .Property(s => s.FileName)
            .HasMaxLength(260);

        modelBuilder.Entity<AssignmentSubmission>()
            .Property(s => s.ContentType)
            .HasMaxLength(150);

        modelBuilder.Entity<AssignmentSubmission>()
            .Property(s => s.StorageKey)
            .HasMaxLength(400);

        modelBuilder.Entity<AssignmentSubmission>()
            .Property(s => s.Feedback)
            .HasMaxLength(4000);

        modelBuilder.Entity<AssignmentSubmission>()
            .Property<uint>("Version")
            .IsRowVersion();

        ScopeToSchool<AssignmentSubmission>(modelBuilder);
    }

    private void ConfigureMessaging(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>()
            .HasAlternateKey(c => new
            {
                c.Id,
                c.SchoolId
            });

        modelBuilder.Entity<Conversation>()
            .HasOne<Student>()
            .WithMany()
            .HasForeignKey(c => new
            {
                c.StudentId,
                c.SchoolId
            })
            .HasPrincipalKey(s => new
            {
                s.Id,
                s.SchoolId
            })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Conversation>()
            .HasOne<Teacher>()
            .WithMany()
            .HasForeignKey(c => new
            {
                c.TeacherId,
                c.SchoolId
            })
            .HasPrincipalKey(t => new
            {
                t.Id,
                t.SchoolId
            })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Conversation>()
            .HasOne<Guardian>()
            .WithMany()
            .HasForeignKey(c => c.GuardianId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Conversation>()
            .HasIndex(c => new
            {
                c.SchoolId,
                c.StudentId,
                c.GuardianId,
                c.TeacherId
            })
            .IsUnique();

        modelBuilder.Entity<Conversation>()
            .HasIndex(c => new
            {
                c.SchoolId,
                c.UpdatedAt
            });

        modelBuilder.Entity<Conversation>()
            .Property<uint>("Version")
            .IsRowVersion();

        ScopeToSchool<Conversation>(modelBuilder);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .HasPrincipalKey(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .Property(m => m.Content)
            .HasMaxLength(4000);

        modelBuilder.Entity<Message>()
            .HasIndex(m => new
            {
                m.ConversationId,
                m.SentAt
            });

        modelBuilder.Entity<Message>()
            .Property<uint>("Version")
            .IsRowVersion();
    }


}
