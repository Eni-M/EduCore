using EduCore.Dtos.Class;
using EduCore.Dtos.Enrollment;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

// The point of a separate medical guard: viewing a student is not enough. A teacher must be the
// student's homeroom teacher -- merely teaching their class does not open the medical record.
public class AccessGuardMedicalTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid HomeroomTeacherId = Guid.NewGuid();
    private static readonly Guid OtherTeacherId = Guid.NewGuid();

    private sealed class FakeCurrentUser(UserRole role, Guid? teacherId = null, Guid? studentId = null) : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => role;
        public Guid? SchoolId => Guid.Empty;
        public IReadOnlyCollection<Guid> SchoolIds => [Guid.Empty];
        public Guid? TeacherId => teacherId;
        public Guid? StudentId => studentId;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => role == UserRole.StateAdmin;
        public bool IsRegionalAdmin => role == UserRole.RegionalAdmin;
        public bool IsDirectorate => role == UserRole.Directorate;
        public bool IsTeacher => role == UserRole.Teacher;
        public bool IsStudent => role == UserRole.Student;
        public bool IsGuardian => role == UserRole.Guardian;
    }

    private sealed class FakeClassRepository : IClassRepository
    {
        public Task<Class?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Class?>(new Class { Id = id, HomeroomTeacherId = HomeroomTeacherId });
        public Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ClassListDto>>([]);
        public Task<ClassListDto?> GetDtoAsync(Guid classId, CancellationToken ct) => Task.FromResult<ClassListDto?>(null);
        public Task<IEnumerable<ClassListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ClassListDto>>([]);
        public Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, Guid? excludeClassId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<IEnumerable<Class>> GetAllWithHomeroomTeacherAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<Class>>([]);
        public Task AddAsync(Class entity) => Task.CompletedTask;
        public void Update(Class entity) { }
        public void Remove(Class entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeEnrollmentRepository : IEnrollmentRepository
    {
        public Task<Enrollment?> GetActiveForStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<Enrollment?>(studentId == StudentId ? new Enrollment { ClassId = ClassId } : null);
        public Task<IEnumerable<EnrollmentListDto>> GetByClassAsync(Guid classId, bool includeLeft, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EnrollmentListDto>>([]);
        public Task<IEnumerable<EnrollmentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EnrollmentListDto>>([]);
        public Task<IEnumerable<Enrollment>> GetActiveByClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Enrollment>>([]);
        public Task<EnrollmentListDto?> GetDtoAsync(Guid enrollmentId, CancellationToken ct) =>
            Task.FromResult<EnrollmentListDto?>(null);
        public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Enrollment?>(null);
        public Task AddAsync(Enrollment entity) => Task.CompletedTask;
        public void Update(Enrollment entity) { }
        public void Remove(Enrollment entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeAssignmentRepository : IClassSubjectTeacherRepository
    {
        public Task<bool> TeachesClassAsync(Guid teacherId, Guid classId, CancellationToken ct) => Task.FromResult(true);
        public Task<IEnumerable<Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>> GetByTeacherAsync(
            Guid teacherId, bool includeEnded, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>>([]);
        public Task<IEnumerable<Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>> GetByClassAsync(
            Guid classId, bool includeEnded, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>>([]);
        public Task<ClassSubjectTeacher?> GetActiveAsync(Guid classId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult<ClassSubjectTeacher?>(null);
        public Task<IReadOnlyList<ClassSubjectTeacher>> GetActiveForClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ClassSubjectTeacher>>([]);
        public Task<Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto?> GetDtoAsync(Guid assignmentId, CancellationToken ct) =>
            Task.FromResult<Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto?>(null);
        public Task<ClassSubjectTeacher?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<ClassSubjectTeacher?>(null);
        public Task AddAsync(ClassSubjectTeacher entity) => Task.CompletedTask;
        public void Update(ClassSubjectTeacher entity) { }
        public void Remove(ClassSubjectTeacher entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeGuardianRepository : IGuardianRepository
    {
        public Task<bool> LinkExistsAsync(Guid guardianId, Guid studentId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> HasChildInClassAsync(Guid guardianId, Guid classId, CancellationToken ct) => Task.FromResult(false);
        public Task<Guardian?> GetByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task<Guardian?> GetWithChildrenAsync(Guid guardianId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task<Guardian?> GetTrackedWithChildrenAsync(Guid guardianId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task<(IReadOnlyList<Guardian> Items, int TotalCount)> GetPagedBySchoolAsync(
            Guid schoolId, EduCore.Dtos.Pagination.PaginationQueryDto pagination, string? search, CancellationToken ct) =>
            Task.FromResult<(IReadOnlyList<Guardian>, int)>((new List<Guardian>(), 0));
        public Task<IReadOnlyList<Guardian>> SearchAsync(string query, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Guardian>>([]);
        public Task<Guardian?> GetGloballyAsync(Guid guardianId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken ct) => Task.FromResult(false);
        public Task<GuardianStudent?> GetLinkAsync(Guid guardianId, Guid studentId, CancellationToken ct) =>
            Task.FromResult<GuardianStudent?>(null);
        public Task<IReadOnlyList<GuardianStudent>> GetContactsForStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<GuardianStudent>>([]);
        public Task AddLinkAsync(GuardianStudent link) => Task.CompletedTask;
        public void RemoveLink(GuardianStudent link) { }
        public Task<Guardian?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task AddAsync(Guardian entity) => Task.CompletedTask;
        public void Update(Guardian entity) { }
        public void Remove(Guardian entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static AccessGuard Guard(ICurrentUser user) =>
        new(new FakeClassRepository(), new FakeEnrollmentRepository(), new FakeAssignmentRepository(),
            new FakeGuardianRepository(), Stub.Of<ILocationRepository>(), user);

    [Fact]
    public async Task Homeroom_teacher_can_view_medical() =>
        await Guard(new FakeCurrentUser(UserRole.Teacher, teacherId: HomeroomTeacherId))
            .EnsureCanViewStudentMedicalAsync(StudentId, CancellationToken.None);

    [Fact]
    public async Task Non_homeroom_teacher_cannot_view_medical_even_though_they_teach_the_class() =>
        await Assert.ThrowsAsync<ForbiddenException>(
            () => Guard(new FakeCurrentUser(UserRole.Teacher, teacherId: OtherTeacherId))
                .EnsureCanViewStudentMedicalAsync(StudentId, CancellationToken.None));

    [Fact]
    public async Task Directorate_can_view_medical() =>
        await Guard(new FakeCurrentUser(UserRole.Directorate))
            .EnsureCanViewStudentMedicalAsync(StudentId, CancellationToken.None);

    [Fact]
    public async Task Student_can_view_their_own_medical() =>
        await Guard(new FakeCurrentUser(UserRole.Student, studentId: StudentId))
            .EnsureCanViewStudentMedicalAsync(StudentId, CancellationToken.None);

    [Fact]
    public async Task Student_cannot_view_another_students_medical() =>
        await Assert.ThrowsAsync<ForbiddenException>(
            () => Guard(new FakeCurrentUser(UserRole.Student, studentId: Guid.NewGuid()))
                .EnsureCanViewStudentMedicalAsync(StudentId, CancellationToken.None));
}
