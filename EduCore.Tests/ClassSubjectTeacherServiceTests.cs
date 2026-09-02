using AutoMapper;
using EduCore.Dtos.ClassSubjectTeacher;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Mapping;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

public class ClassSubjectTeacherServiceTests
{
    private static readonly Guid SchoolId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => UserRole.Directorate;
        public Guid? SchoolId => ClassSubjectTeacherServiceTests.SchoolId;
        public IReadOnlyCollection<Guid> SchoolIds => [ClassSubjectTeacherServiceTests.SchoolId];
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

    private sealed class FakeClassRepository(Class classEntity) : IClassRepository
    {
        public Task<Class?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Class?>(classEntity);
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

    private sealed class FakeSchoolRepository(School school) : ISchoolRepository
    {
        public Task<School?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<School?>(school);
        public Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct) => Task.FromResult<School?>(school);
        public Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<IEnumerable<School>> GetByCityAsync(Guid cityId, bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<IEnumerable<School>> GetByStateAsync(Guid stateId, bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task AddAsync(School entity) => Task.CompletedTask;
        public void Update(School entity) { }
        public void Remove(School entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeSubjectRepository : ISubjectRepository
    {
        public Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Subject?>(new Subject { Id = SubjectId, Name = "Mathematics", SchoolId = SchoolId });
        public Task<IEnumerable<Subject>> GetAllOrderedAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<Subject>>([]);
        public Task<bool> NameExistsAsync(string name, Guid? excludeSubjectId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<bool> IsInUseAsync(Guid subjectId, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(Subject entity) => Task.CompletedTask;
        public void Update(Subject entity) { }
        public void Remove(Subject entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeTeacherSubjectRepository(bool qualified) : ITeacherSubjectRepository
    {
        public Task<bool> ExistsAsync(Guid teacherId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult(qualified);
        public Task<IEnumerable<EduCore.Dtos.TeacherSubject.TeacherSubjectListDto>> GetByTeacherAsync(
            Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.TeacherSubject.TeacherSubjectListDto>>([]);
        public Task<TeacherSubject?> GetAsync(Guid teacherId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult<TeacherSubject?>(null);
        public Task<IReadOnlyList<Guid>> GetSubjectIdsForTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<Guid>> GetTeacherIdsForSubjectAsync(Guid subjectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> IsInUseAsync(Guid teacherId, Guid subjectId, CancellationToken ct) => Task.FromResult(false);
        public Task<TeacherSubject?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<TeacherSubject?>(null);
        public Task AddAsync(TeacherSubject entity) => Task.CompletedTask;
        public void Update(TeacherSubject entity) { }
        public void Remove(TeacherSubject entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeAssignmentRepository : IClassSubjectTeacherRepository
    {
        public ClassSubjectTeacher? Added;
        public Task<IEnumerable<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>> GetByTeacherAsync(
            Guid teacherId, bool includeEnded, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>>([]);
        public Task<IEnumerable<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>> GetByClassAsync(
            Guid classId, bool includeEnded, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>>([]);
        public Task<ClassSubjectTeacher?> GetActiveAsync(Guid classId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult<ClassSubjectTeacher?>(null);
        public Task<IReadOnlyList<ClassSubjectTeacher>> GetActiveForClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ClassSubjectTeacher>>([]);
        public Task<bool> TeachesClassAsync(Guid teacherId, Guid classId, CancellationToken ct) => Task.FromResult(false);
        public Task<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto?> GetDtoAsync(
            Guid assignmentId, CancellationToken ct) =>
            Task.FromResult<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto?>(
                new(assignmentId, ClassId, "10A", SubjectId, "Mathematics", TeacherId, "A Teacher", null));
        public Task<ClassSubjectTeacher?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<ClassSubjectTeacher?>(null);
        public Task AddAsync(ClassSubjectTeacher entity) { Added = entity; return Task.CompletedTask; }
        public void Update(ClassSubjectTeacher entity) { }
        public void Remove(ClassSubjectTeacher entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static IMapper Mapper() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Assigning_a_teacher_to_a_class_whose_grade_is_outside_its_school_types_band_throws()
    {
        var classEntity = new Class { Id = ClassId, SchoolId = SchoolId, Name = "6A", Grade = 6 };
        var school = new School { Id = SchoolId, Name = "Elementary School", Type = SchoolType.Elementary };

        var service = new ClassSubjectTeacherService(
            new FakeAssignmentRepository(),
            new FakeClassRepository(classEntity),
            new FakeSchoolRepository(school),
            new FakeSubjectRepository(),
            new FakeTeacherSubjectRepository(qualified: true),
            access: null!,
            new FakeCurrentUser(),
            Mapper());

        var dto = new ClassSubjectTeacherCreateDto(ClassId, SubjectId, TeacherId);

        await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task Assigning_a_teacher_to_a_class_whose_grade_matches_its_school_type_succeeds()
    {
        var classEntity = new Class { Id = ClassId, SchoolId = SchoolId, Name = "10A", Grade = 10 };
        var school = new School { Id = SchoolId, Name = "High School", Type = SchoolType.HighSchool };
        var assignments = new FakeAssignmentRepository();

        var service = new ClassSubjectTeacherService(
            assignments,
            new FakeClassRepository(classEntity),
            new FakeSchoolRepository(school),
            new FakeSubjectRepository(),
            new FakeTeacherSubjectRepository(qualified: true),
            access: null!,
            new FakeCurrentUser(),
            Mapper());

        var dto = new ClassSubjectTeacherCreateDto(ClassId, SubjectId, TeacherId);

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(assignments.Added);
        Assert.Equal(SubjectId, result.SubjectId);
    }
}
