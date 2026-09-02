using AutoMapper;
using EduCore.Dtos.Enrollment;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Mapping;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

public class EnrollmentServiceTests
{
    private static readonly Guid SchoolId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => UserRole.Directorate;
        public Guid? SchoolId => EnrollmentServiceTests.SchoolId;
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => false;
        public bool IsRegionalAdmin => false;
        public bool IsDirectorate => true;
        public bool IsTeacher => false;
        public bool IsStudent => false;
        public bool IsGuardian => false;
    }

    private sealed class FakeStudentRepository(Student? student) : IStudentRepository
    {
        public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(student);
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

    private sealed class FakeClassRepository(Class? classEntity) : IClassRepository
    {
        public Task<Class?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(classEntity);
        public Task<IEnumerable<EduCore.Dtos.Class.ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.Class.ClassListDto>>([]);
        public Task<EduCore.Dtos.Class.ClassListDto?> GetDtoAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<EduCore.Dtos.Class.ClassListDto?>(null);
        public Task<IEnumerable<EduCore.Dtos.Class.ClassListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.Class.ClassListDto>>([]);
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
        public Enrollment? Added;
        public Task<Enrollment?> GetActiveForStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<Enrollment?>(null);
        public Task<IEnumerable<EnrollmentListDto>> GetByClassAsync(Guid classId, bool includeLeft, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EnrollmentListDto>>([]);
        public Task<IEnumerable<EnrollmentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EnrollmentListDto>>([]);
        public Task<IEnumerable<Enrollment>> GetActiveByClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Enrollment>>([]);
        public Task<EnrollmentListDto?> GetDtoAsync(Guid enrollmentId, CancellationToken ct) =>
            Task.FromResult<EnrollmentListDto?>(
                new EnrollmentListDto(enrollmentId, StudentId, "A Student", ClassId, "10A",
                    DateOnly.FromDateTime(DateTime.UtcNow), null, null));
        public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Enrollment?>(null);
        public Task AddAsync(Enrollment entity) { Added = entity; return Task.CompletedTask; }
        public void Update(Enrollment entity) { }
        public void Remove(Enrollment entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static IMapper Mapper() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Admin_enrolling_an_unenrolled_student_sets_the_current_school_id()
    {
        var student = new Student { Id = StudentId, SchoolId = SchoolId, GraduationDate = null };
        var classEntity = new Class { Id = ClassId, SchoolId = SchoolId, Name = "10A", Grade = 10 };
        var enrollments = new FakeEnrollmentRepository();

        var service = new EnrollmentService(
            enrollments,
            new FakeStudentRepository(student),
            new FakeClassRepository(classEntity),
            access: null!,
            new FakeCurrentUser(),
            Mapper());

        var dto = new EnrollmentCreateDto(StudentId, ClassId);

        var result = await service.EnrollAsync(dto, CancellationToken.None);

        Assert.NotNull(enrollments.Added);
        Assert.Equal(SchoolId, enrollments.Added!.SchoolId);
        Assert.Equal(StudentId, result.StudentId);
    }
}
