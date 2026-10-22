using AutoMapper;
using EduCore.Dtos.Assignment;
using EduCore.Dtos.Calendar;
using EduCore.Dtos.Class;
using EduCore.Dtos.Enrollment;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Mapping;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using EduCore.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EduCore.Tests;

public class CalendarServiceTests
{
    private static readonly Guid SchoolId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();

    private sealed class FakeCurrentUser(UserRole role, Guid userId, Guid? teacherId = null, Guid? studentId = null)
        : ICurrentUser
    {
        public Guid UserId => userId;
        public UserRole? Role => role;
        public Guid? SchoolId => CalendarServiceTests.SchoolId;
        public IReadOnlyCollection<Guid> SchoolIds => [CalendarServiceTests.SchoolId];
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

    private sealed class FakeCalendarRepo : ICalendarEventRepository
    {
        public readonly List<CalendarEvent> Added = [];
        public bool? LastSeeAll;
        public Guid[] LastClassIds = [];
        public Guid[] LastStudentIds = [];

        public Task<IEnumerable<CalendarEventListDto>> GetForViewerAsync(
            bool seeAll, IReadOnlyCollection<Guid> classIds, IReadOnlyCollection<Guid> studentIds,
            Guid creatorUserId, DateOnly? from, DateOnly? to, CancellationToken ct)
        {
            LastSeeAll = seeAll;
            LastClassIds = classIds.ToArray();
            LastStudentIds = studentIds.ToArray();
            return Task.FromResult<IEnumerable<CalendarEventListDto>>(Added.Select(ToDto).ToList());
        }

        private static CalendarEventListDto ToDto(CalendarEvent e) => new(
            e.Id, e.Title, e.Description, e.Type, e.Audience, e.ClassId, null, e.StudentId, null,
            e.Date, e.StartTime, e.EndTime, e.CreatedByUserId, "");

        public Task<CalendarEventListDto?> GetDtoAsync(Guid eventId, CancellationToken ct) =>
            Task.FromResult<CalendarEventListDto?>(Added.Where(e => e.Id == eventId).Select(ToDto).FirstOrDefault());
        public Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Added.FirstOrDefault(e => e.Id == id));
        public Task AddAsync(CalendarEvent entity)
        {
            if (entity.Id == default) entity.Id = Guid.NewGuid();
            Added.Add(entity);
            return Task.CompletedTask;
        }
        public void Update(CalendarEvent entity) { }
        public void Remove(CalendarEvent entity) => Added.Remove(entity);
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeClassRepo(IReadOnlyList<Guid> taught) : IClassRepository
    {
        public Task<IEnumerable<ClassListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ClassListDto>>(
                taught.Select(id => new ClassListDto(id, "C", 1, SchoolId, "S", Guid.NewGuid(), "Y", null, null, null, null, null)));
        public Task<Class?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Class?>(new Class { Id = id, SchoolId = SchoolId });
        public Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<ClassListDto>>([]);
        public Task<ClassListDto?> GetDtoAsync(Guid classId, CancellationToken ct) => Task.FromResult<ClassListDto?>(null);
        public Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, Guid? excludeClassId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<IEnumerable<Class>> GetAllWithHomeroomTeacherAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<Class>>([]);
        public Task AddAsync(Class entity) => Task.CompletedTask;
        public void Update(Class entity) { }
        public void Remove(Class entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeEnrollmentRepo : IEnrollmentRepository
    {
        public Task<Enrollment?> GetActiveForStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<Enrollment?>(studentId == StudentId
                ? new Enrollment { StudentId = StudentId, ClassId = ClassId }
                : null);
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

    private sealed class FakeGuardianRepo : IGuardianRepository
    {
        public Task<Guardian?> GetWithChildrenAsync(Guid guardianId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
        public Task<bool> LinkExistsAsync(Guid guardianId, Guid studentId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> HasChildInClassAsync(Guid guardianId, Guid classId, CancellationToken ct) => Task.FromResult(false);
        public Task<Guardian?> GetByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Guardian?>(null);
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

    private sealed class FakeAccessGuard : IAccessGuard
    {
        public Task EnsureCanViewClassAsync(Guid classId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanTeachClassAsync(Guid classId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanViewStudentAsync(Guid studentId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanViewStudentMedicalAsync(Guid studentId, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> CanViewStudentMedicalAsync(Guid studentId, CancellationToken ct) => Task.FromResult(true);
        public Task EnsureCanManageSchoolAsync(Guid? schoolCityId, CancellationToken ct) => Task.CompletedTask;
        public void EnsureCanWriteMarks(Guid assignedTeacherId) { }
        public void EnsureConversationParticipant(Guid guardianId, Guid teacherId) { }
    }

    private sealed class FakeStudentRepo : IStudentRepository
    {
        public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Student?>(new Student { Id = id, SchoolId = SchoolId });
        public Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Student?>(null);
        public Task<Student?> GetWithGuardiansAsync(Guid studentId, CancellationToken ct) => Task.FromResult<Student?>(null);
        public Task<bool> PersonalNumberExistsAsync(string personalNumber, Guid? excludeStudentId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, bool includeGraduated, CancellationToken ct) =>
            Task.FromResult<(IReadOnlyList<Student>, int)>(([], 0));
        public Task<IEnumerable<Student>> GetByClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Student>>([]);
        public Task AddAsync(Student entity) => Task.CompletedTask;
        public void Update(Student entity) { }
        public void Remove(Student entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeAssignmentRepo : IAssignmentRepository
    {
        public Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Assignment?>(null);
        public Task<IEnumerable<AssignmentListDto>> GetByClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<AssignmentListDto>>([]);
        public Task<IEnumerable<AssignmentListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<AssignmentListDto>>([]);
        public Task<AssignmentDetailsDto?> GetDetailsAsync(Guid assignmentId, CancellationToken ct) =>
            Task.FromResult<AssignmentDetailsDto?>(null);
        public Task<IEnumerable<AssignmentCalendarDto>> GetForCalendarAsync(Guid[] classIds, DateOnly? from, DateOnly? to, CancellationToken ct) =>
            Task.FromResult<IEnumerable<AssignmentCalendarDto>>([]);
        public Task AddAsync(Assignment entity) => Task.CompletedTask;
        public void Update(Assignment entity) { }
        public void Remove(Assignment entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static IMapper Mapper() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static CalendarService Service(ICurrentUser user, FakeCalendarRepo repo, IReadOnlyList<Guid>? taught = null) =>
        new(repo, new FakeClassRepo(taught ?? []), new FakeStudentRepo(), new FakeEnrollmentRepo(),
            new FakeGuardianRepo(), new FakeAccessGuard(), user, Mapper(), new FakeAssignmentRepo());

    private static CalendarEventCreateDto Create(CalendarAudience audience, Guid? classId = null, Guid? studentId = null) =>
        new("Midterm", null, CalendarEventType.Exam, audience, classId, studentId,
            new DateOnly(2026, 3, 1), null, null);

    [Fact]
    public async Task Student_feed_asks_only_for_their_own_class_and_id()
    {
        var repo = new FakeCalendarRepo();
        var user = new FakeCurrentUser(UserRole.Student, Guid.NewGuid(), studentId: StudentId);

        await Service(user, repo).GetMineAsync(null, null, CancellationToken.None);

        Assert.False(repo.LastSeeAll);
        Assert.Contains(StudentId, repo.LastStudentIds);
        Assert.Contains(ClassId, repo.LastClassIds);
    }

    [Fact]
    public async Task Directorate_feed_sees_everything()
    {
        var repo = new FakeCalendarRepo();
        var user = new FakeCurrentUser(UserRole.Directorate, Guid.NewGuid());

        await Service(user, repo).GetMineAsync(null, null, CancellationToken.None);

        Assert.True(repo.LastSeeAll);
    }

    [Fact]
    public async Task Teacher_cannot_post_a_school_wide_event()
    {
        var repo = new FakeCalendarRepo();
        var user = new FakeCurrentUser(UserRole.Teacher, Guid.NewGuid(), teacherId: Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => Service(user, repo).CreateAsync(Create(CalendarAudience.School), CancellationToken.None));
    }

    [Fact]
    public async Task Directorate_can_post_a_school_wide_event()
    {
        var repo = new FakeCalendarRepo();
        var user = new FakeCurrentUser(UserRole.Directorate, Guid.NewGuid());

        var result = await Service(user, repo).CreateAsync(Create(CalendarAudience.School), CancellationToken.None);

        Assert.Single(repo.Added);
        Assert.Equal(CalendarAudience.School, result.Audience);
        Assert.Null(repo.Added[0].ClassId);
    }

    [Fact]
    public async Task Teacher_can_post_a_class_event_and_it_carries_the_class()
    {
        var repo = new FakeCalendarRepo();
        var user = new FakeCurrentUser(UserRole.Teacher, Guid.NewGuid(), teacherId: Guid.NewGuid());

        await Service(user, repo, [ClassId]).CreateAsync(Create(CalendarAudience.Class, classId: ClassId), CancellationToken.None);

        Assert.Single(repo.Added);
        Assert.Equal(ClassId, repo.Added[0].ClassId);
    }

    [Fact]
    public async Task A_teacher_cannot_edit_an_event_someone_else_created()
    {
        var repo = new FakeCalendarRepo();
        repo.Added.Add(new CalendarEvent { Id = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid(), Date = new DateOnly(2026, 3, 1) });
        var user = new FakeCurrentUser(UserRole.Teacher, Guid.NewGuid(), teacherId: Guid.NewGuid());

        var edit = new CalendarEventEditDto("x", null, CalendarEventType.Exam, new DateOnly(2026, 3, 2), null, null);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => Service(user, repo).UpdateAsync(repo.Added[0].Id, edit, CancellationToken.None));
    }
}
