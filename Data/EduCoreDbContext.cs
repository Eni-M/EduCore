using EduCore.Models;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Data;

public class EduCoreDbContext(DbContextOptions<EduCoreDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<ClassSubjectTeacher> ClassSubjectTeachers => Set<ClassSubjectTeacher>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.UserId)
            .IsUnique();

        modelBuilder.Entity<Parent>()
            .HasOne<Student>()
            .WithMany(s => s.Parents)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Teacher>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Teacher>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Teacher>()
            .HasIndex(t => t.UserId)
            .IsUnique();

        modelBuilder.Entity<Admin>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Admin>()
            .HasIndex(a => a.UserId)
            .IsUnique();
        modelBuilder.Entity<Admin>()
            .HasOne<School>()
            .WithMany()
            .HasForeignKey(a => a.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        // Classes hold irreplaceable historical data (enrollments, grades, attendance),
        // so a School can't be deleted out from under them.
        modelBuilder.Entity<Class>()
            .HasOne<School>()
            .WithMany()
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Class>()
            .HasOne<Teacher>()
            .WithMany()
            .HasForeignKey(c => c.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        // Students are never hard-deleted (see Student.Graduate()), but Restrict is kept
        // as defense in depth against accidental deletion wiping enrollment history.
        modelBuilder.Entity<Enrollment>()
            .HasOne<Student>()
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Enrollment>()
            .HasOne<Class>()
            .WithMany()
            .HasForeignKey(e => e.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne<Enrollment>()
            .WithMany()
            .HasForeignKey(a => a.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Marks must survive independently of a student's enrollment/class history
        // (report cards are kept for years after graduation).
        modelBuilder.Entity<Mark>()
            .HasOne<Student>()
            .WithMany()
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Mark>()
            .HasOne<Subject>()
            .WithMany()
            .HasForeignKey(m => m.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Mark>()
            .HasOne<Teacher>()
            .WithMany()
            .HasForeignKey(m => m.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subject>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<TeacherSubject>()
            .HasOne<Teacher>()
            .WithMany(t => t.TeacherSubjects)
            .HasForeignKey(ts => ts.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TeacherSubject>()
            .HasOne<Subject>()
            .WithMany()
            .HasForeignKey(ts => ts.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TeacherSubject>()
            .HasIndex(ts => new { ts.TeacherId, ts.SubjectId })
            .IsUnique();

        modelBuilder.Entity<ClassSubjectTeacher>()
            .HasOne<Class>()
            .WithMany()
            .HasForeignKey(cst => cst.ClassId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ClassSubjectTeacher>()
            .HasOne<Subject>()
            .WithMany()
            .HasForeignKey(cst => cst.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClassSubjectTeacher>()
            .HasOne<Teacher>()
            .WithMany()
            .HasForeignKey(cst => cst.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClassSubjectTeacher>()
            .HasIndex(cst => new { cst.ClassId, cst.SubjectId })
            .IsUnique();
    }
}
