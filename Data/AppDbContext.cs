using EduCore.Models;
using EduCore.Security;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Admin> Admins => Set<Admin>();
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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<ClassSubjectTeacher> ClassSubjectTeachers => Set<ClassSubjectTeacher>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureSchoolCalendar(modelBuilder);
        ConfigureClasses(modelBuilder);
        ConfigureLessons(modelBuilder);

        modelBuilder.Entity<AuditLog>().Property(a => a.Action).HasMaxLength(1000);
        modelBuilder.Entity<AuditLog>().Property(a => a.TraceId).HasMaxLength(200);
    }

    private void ScopeToSchool<T>(ModelBuilder modelBuilder) where T : class, ISchoolScoped =>
        modelBuilder.Entity<T>().HasQueryFilter(e => e.SchoolId == currentUser.SchoolId);

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

        modelBuilder.Entity<Parent>()
            .HasOne<Student>().WithMany(s => s.Parents)
            .HasForeignKey(p => new { p.StudentId, p.SchoolId })
            .HasPrincipalKey(s => new { s.Id, s.SchoolId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Parent>().Property(p => p.Name).HasMaxLength(200);
        modelBuilder.Entity<Parent>().Property(p => p.PhoneNumber).HasMaxLength(30);
        modelBuilder.Entity<Parent>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Parent>(modelBuilder);

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

        modelBuilder.Entity<Admin>()
            .HasOne(a => a.User).WithOne()
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Admin>().HasIndex(a => a.UserId).IsUnique();
        modelBuilder.Entity<Admin>()
            .HasOne(a => a.School).WithMany()
            .HasForeignKey(a => a.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Admin>().Property<uint>("Version").IsRowVersion();
        ScopeToSchool<Admin>(modelBuilder);
    }

    private void ConfigureSchoolCalendar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<School>().Property(s => s.Name).HasMaxLength(200);
        modelBuilder.Entity<School>().Property(s => s.City).HasMaxLength(120);
        modelBuilder.Entity<School>().Property(s => s.Zone).HasMaxLength(120);
        modelBuilder.Entity<School>().HasIndex(s => new { s.City, s.Zone });
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

        modelBuilder.Entity<Student>()
            .HasOne(s => s.City).WithMany()
            .HasForeignKey(s => s.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<School>()
            .HasOne<City>().WithMany()
            .HasForeignKey(s => s.CityId)
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
