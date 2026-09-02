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

public class AccessGuardGuardianTests
{
    private static readonly Guid GuardianId = Guid.NewGuid();
    private static readonly Guid MyChildId = Guid.NewGuid();
    private static readonly Guid OtherChildId = Guid.NewGuid();
    private static readonly Guid MyChildClassId = Guid.NewGuid();

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => UserRole.Guardian;
        public Guid? SchoolId => null;
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => AccessGuardGuardianTests.GuardianId;
        public bool IsStateAdmin => false;
        public bool IsRegionalAdmin => false;
        public bool IsDirectorate => false;
        public bool IsTeacher => false;
        public bool IsStudent => false;
        public bool IsGuardian => true;
    }

    private sealed class FakeGuardianRepository : IGuardianRepository
    {
        public Task<bool> LinkExistsAsync(Guid guardianId, Guid studentId, CancellationToken ct) =>
            Task.FromResult(guardianId == GuardianId && studentId == MyChildId);

        public Task<bool> HasChildInClassAsync(Guid guardianId, Guid classId, CancellationToken ct) =>
            Task.FromResult(guardianId == GuardianId && classId == MyChildClassId);

        public Task<Guardian?> GetByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task<Guardian?> GetWithChildrenAsync(Guid guardianId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task<Guardian?> GetTrackedWithChildrenAsync(Guid guardianId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task<IEnumerable<Guardian>> GetBySchoolAsync(Guid schoolId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Guardian>>([]);
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

    private sealed class FakeClassRepository : IClassRepository
    {
        public Task<Class?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Class?>(new Class { Id = id });
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
            Task.FromResult<Enrollment?>(null);
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
        public Task<bool> TeachesClassAsync(Guid teacherId, Guid classId, CancellationToken ct) => Task.FromResult(false);
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

    private static AccessGuard Guard() =>
        new(new FakeClassRepository(), new FakeEnrollmentRepository(), new FakeAssignmentRepository(),
            new FakeGuardianRepository(), new FakeCurrentUser());

    [Fact]
    public async Task Guardian_can_view_a_linked_child() =>
        await Guard().EnsureCanViewStudentAsync(MyChildId, CancellationToken.None);

    [Fact]
    public async Task Guardian_cannot_view_an_unrelated_student() =>
        await Assert.ThrowsAsync<ForbiddenException>(
            () => Guard().EnsureCanViewStudentAsync(OtherChildId, CancellationToken.None));

    [Fact]
    public async Task Guardian_can_view_a_childs_class() =>
        await Guard().EnsureCanViewClassAsync(MyChildClassId, CancellationToken.None);

    [Fact]
    public async Task Guardian_cannot_view_an_unrelated_class() =>
        await Assert.ThrowsAsync<ForbiddenException>(
            () => Guard().EnsureCanViewClassAsync(Guid.NewGuid(), CancellationToken.None));
}
