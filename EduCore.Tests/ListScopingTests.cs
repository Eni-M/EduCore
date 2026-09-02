using AutoMapper;
using EduCore.Dtos.Class;
using EduCore.Dtos.ClassSubjectTeacher;
using EduCore.Dtos.Enrollment;
using EduCore.Dtos.Subject;
using EduCore.Enums;
using EduCore.Mapping;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using EduCore.Services.Assignment;
using Xunit;

namespace EduCore.Tests;

public class ListScopingTests
{
    private static readonly Guid SchoolId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid MyClassId = Guid.NewGuid();
    private static readonly Guid OtherClassId = Guid.NewGuid();
    private static readonly Guid MySubjectId = Guid.NewGuid();
    private static readonly Guid OtherSubjectId = Guid.NewGuid();
    private static readonly Guid YearId = Guid.NewGuid();

    private sealed class FakeCurrentUser(UserRole role) : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => role;
        public Guid? SchoolId => SchoolId;
        public Guid? TeacherId => role == UserRole.Teacher ? ListScopingTests.TeacherId : null;
        public Guid? StudentId => role == UserRole.Student ? ListScopingTests.StudentId : null;
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

    private static ClassListDto Class(Guid id, string name) =>
        new(id, name, 10, SchoolId, "School", YearId, "2025/2026", null, null, 30);

    private sealed class FakeClassRepository : IClassRepository
    {
        public Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ClassListDto>>(
                [Class(MyClassId, "10A"), Class(OtherClassId, "10B")]);

        public Task<IEnumerable<ClassListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ClassListDto>>([Class(MyClassId, "10A")]);

        public Task<ClassListDto?> GetDtoAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<ClassListDto?>(Class(classId, "10A"));

        public Task<bool> NameExistsInYearAsync(
            Guid academicYearId, string name, Guid? excludeClassId, CancellationToken ct) =>
            Task.FromResult(false);

        public Task<IEnumerable<Class>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Class>>([]);

        public Task<IEnumerable<Class>> GetAllWithHomeroomTeacherAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<Class>>([]);

        public Task<Class?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Class?>(null);
        public Task AddAsync(Class entity) => Task.CompletedTask;
        public void Update(Class entity) { }
        public void Remove(Class entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static ClassSubjectTeacherListDto Assignment(Guid classId, Guid subjectId, string subjectName) =>
        new(Guid.NewGuid(), classId, "10A", subjectId, subjectName, TeacherId, "A Teacher", null);

    private sealed class FakeAssignmentRepository : IClassSubjectTeacherRepository
    {
        public Task<IEnumerable<ClassSubjectTeacherListDto>> GetByTeacherAsync(
            Guid teacherId, bool includeEnded, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ClassSubjectTeacherListDto>>(
            [
                Assignment(MyClassId, MySubjectId, "Mathematics"),
                Assignment(OtherClassId, MySubjectId, "Mathematics"),
            ]);

        public Task<IEnumerable<ClassSubjectTeacherListDto>> GetByClassAsync(
            Guid classId, bool includeEnded, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ClassSubjectTeacherListDto>>(
                classId == MyClassId ? [Assignment(MyClassId, MySubjectId, "Mathematics")] : []);

        public Task<ClassSubjectTeacher?> GetActiveAsync(Guid classId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult<ClassSubjectTeacher?>(null);

        public Task<IReadOnlyList<ClassSubjectTeacher>> GetActiveForClassAsync(
            Guid classId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ClassSubjectTeacher>>([]);

        public Task<bool> TeachesClassAsync(Guid teacherId, Guid classId, CancellationToken ct) =>
            Task.FromResult(classId == MyClassId);

        public Task<ClassSubjectTeacherListDto?> GetDtoAsync(Guid assignmentId, CancellationToken ct) =>
            Task.FromResult<ClassSubjectTeacherListDto?>(null);

        public Task<ClassSubjectTeacher?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<ClassSubjectTeacher?>(null);
        public Task AddAsync(ClassSubjectTeacher entity) => Task.CompletedTask;
        public void Update(ClassSubjectTeacher entity) { }
        public void Remove(ClassSubjectTeacher entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeEnrollmentRepository(bool enrolled) : IEnrollmentRepository
    {
        public Task<Enrollment?> GetActiveForStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult(enrolled
                ? new Enrollment { Id = Guid.NewGuid(), StudentId = studentId, ClassId = MyClassId }
                : null);

        public Task<IEnumerable<EnrollmentListDto>> GetByClassAsync(
            Guid classId, bool includeLeft, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EnrollmentListDto>>([]);
        public Task<IEnumerable<EnrollmentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EnrollmentListDto>>([]);
        public Task<IEnumerable<Enrollment>> GetActiveByClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Enrollment>>([]);
        public Task<EnrollmentListDto?> GetDtoAsync(Guid enrollmentId, CancellationToken ct) =>
            Task.FromResult<EnrollmentListDto?>(null);
        public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Enrollment?>(null);
        public Task AddAsync(Enrollment entity) => Task.CompletedTask;
        public void Update(Enrollment entity) { }
        public void Remove(Enrollment entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class NoopElementarySync : IElementaryAssignmentSync
    {
        public Task SyncNewSubjectAsync(Guid schoolId, Guid subjectId, CancellationToken ct) =>
            Task.CompletedTask;
        public Task SyncHomeroomChangeAsync(
            Guid schoolId, Guid classId, Guid? previousTeacherId, Guid? newTeacherId,
            CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeSubjectRepository : ISubjectRepository
    {
        public Task<IEnumerable<Subject>> GetAllOrderedAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<Subject>>(
            [
                new Subject { Id = MySubjectId, Name = "Mathematics", SchoolId = SchoolId },
                new Subject { Id = OtherSubjectId, Name = "Physics", SchoolId = SchoolId },
            ]);

        public Task<bool> NameExistsAsync(string name, Guid? excludeSubjectId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<bool> IsInUseAsync(Guid subjectId, CancellationToken ct) => Task.FromResult(false);
        public Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Subject?>(null);
        public Task AddAsync(Subject entity) => Task.CompletedTask;
        public void Update(Subject entity) { }
        public void Remove(Subject entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static IMapper Mapper() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory()).CreateMapper();

    private static Microsoft.Extensions.Logging.ILoggerFactory NullLoggerFactory() =>
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

    private static SubjectService Subjects(UserRole role, bool enrolled = true) =>
        new(new FakeSubjectRepository(),
            new FakeAssignmentRepository(),
            new FakeEnrollmentRepository(enrolled),
            new NoopElementarySync(),
            new FakeCurrentUser(role),
            Mapper());

    [Fact]
    public async Task Admin_listing_classes_sees_the_whole_school()
    {
        var service = new ClassService(
            new FakeClassRepository(), null!, null!, null!, null!, new NoopElementarySync(),
            new FakeCurrentUser(UserRole.Directorate), Mapper());

        var result = await service.GetAllAsync(null, CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Teacher_listing_classes_sees_only_their_own()
    {
        var service = new ClassService(
            new FakeClassRepository(), null!, null!, null!, null!, new NoopElementarySync(),
            new FakeCurrentUser(UserRole.Teacher), Mapper());

        var result = await service.GetAllAsync(null, CancellationToken.None);

        Assert.Equal(MyClassId, Assert.Single(result).Id);
    }

    [Fact]
    public async Task Teacher_listing_classes_still_honours_the_year_filter()
    {
        var service = new ClassService(
            new FakeClassRepository(), null!, null!, null!, null!, new NoopElementarySync(),
            new FakeCurrentUser(UserRole.Teacher), Mapper());

        Assert.Empty(await service.GetAllAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Single(await service.GetAllAsync(YearId, CancellationToken.None));
    }

    [Fact]
    public async Task Admin_listing_subjects_sees_the_whole_catalogue()
    {
        Assert.Equal(2, (await Subjects(UserRole.Directorate).GetAllAsync(CancellationToken.None)).Count());
    }

    [Fact]
    public async Task Teacher_listing_subjects_sees_only_what_they_teach_once_each()
    {
        var result = await Subjects(UserRole.Teacher).GetAllAsync(CancellationToken.None);

        Assert.Equal(MySubjectId, Assert.Single(result).Id);
    }

    [Fact]
    public async Task Student_listing_subjects_sees_only_their_own_class_subjects()
    {
        var result = await Subjects(UserRole.Student).GetAllAsync(CancellationToken.None);

        Assert.Equal(MySubjectId, Assert.Single(result).Id);
    }

    [Fact]
    public async Task Unplaced_student_listing_subjects_gets_nothing_rather_than_everything()
    {
        Assert.Empty(await Subjects(UserRole.Student, enrolled: false).GetAllAsync(CancellationToken.None));
    }
}
