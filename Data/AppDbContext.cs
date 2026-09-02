using EduCore.Models;
using EduCore.Security;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
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

    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<ClassSubjectTeacher> ClassSubjectTeachers => Set<ClassSubjectTeacher>();

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

        modelBuilder.Entity<AuditLog>().Property(a => a.Action).HasMaxLength(1000);
        modelBuilder.Entity<AuditLog>().Property(a => a.TraceId).HasMaxLength(200);
    }

    private void ScopeToSchool<T>(ModelBuilder modelBuilder) where T : class, ISchoolScoped =>
        modelBuilder.Entity<T>().HasQueryFilter(e => currentUser.SchoolIds.Contains(e.SchoolId));

    private void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().Property(u => u.FirstName).HasMaxLength(100);
        modelBuilder.Entity<User>().Property(u => u.LastName).HasMaxLength(100);
        modelBuilder.Entity<User>().Property(u => u.Email).HasMaxLength(320);
        modelBuilder.Entity<User>().Property(u => u.PasswordHash).HasMaxLength(500);
        modelBuilder.Entity<User>().Property<uint>("Version").IsRowVersion();

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
        modelBuilder.Entity<Student>().Property(s => s.PersonalNumber).HasMaxLength(50);
        modelBuilder.Entity<Student>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Student>(modelBuilder);

        modelBuilder.Entity<Guardian>()
            .HasOne(g => g.User).WithOne()
            .HasForeignKey<Guardian>(g => g.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Guardian>().HasIndex(g => g.UserId).IsUnique();
        modelBuilder.Entity<Guardian>().Property(g => g.NationalId).HasMaxLength(50);
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

        modelBuilder.Entity<Student>()
            .HasOne(s => s.City).WithMany()
            .HasForeignKey(s => s.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<School>()
            .HasOne(s => s.City).WithMany()
            .HasForeignKey(s => s.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<School>()
            .HasOne(s => s.Zone).WithMany()
            .HasForeignKey(s => s.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }

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
}
