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
        public Task<(IReadOnlyList<School> Items, int TotalCount)> GetPagedAsync(
            bool activeOnly, Guid? cityId, Guid? stateId,
            EduCore.Dtos.Pagination.PaginationQueryDto pagination, string? search, CancellationToken ct) =>
            Task.FromResult<(IReadOnlyList<School>, int)>(([], 0));
        public Task<School?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<School?>(school);
        public Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct) => Task.FromResult<School?>(school);
        public Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<EduCore.Dtos.Pagination.PagedResultDto<School>> GetOrderedPagedAsync(
            bool activeOnly, EduCore.Dtos.Pagination.PaginationQueryDto pagination, CancellationToken ct) =>
            Task.FromResult(new EduCore.Dtos.Pagination.PagedResultDto<School>([], 1, 20, 0));
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
        public Task<(IReadOnlyList<Subject> Items, int TotalCount)> GetPagedAsync(
            EduCore.Dtos.Pagination.PaginationQueryDto pagination, string? search, CancellationToken ct) =>
            Task.FromResult<(IReadOnlyList<Subject>, int)>(([], 0));
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

    // Every subject is on the syllabus here: these tests are about grade bands and
    // qualifications, not about the curriculum restriction, which has its own tests.
    private sealed class FakeGradeCurriculumRepository(bool onSyllabus = true, bool hasCurriculum = true)
        : IGradeCurriculumRepository
    {
        public Task<IReadOnlyList<Subject>> GetSubjectsForGradeAsync(int grade, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Subject>>([]);
        public Task<IReadOnlyList<GradeCurriculum>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<GradeCurriculum>>([]);
        public Task<GradeCurriculum?> GetAsync(int grade, Guid subjectId, CancellationToken ct) =>
            Task.FromResult<GradeCurriculum?>(null);
        public Task<bool> IsInCurriculumAsync(int grade, Guid subjectId, CancellationToken ct) =>
            Task.FromResult(onSyllabus);
        public Task<bool> HasCurriculumAsync(int grade, CancellationToken ct) =>
            Task.FromResult(hasCurriculum);
        public Task<GradeCurriculum?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<GradeCurriculum?>(null);
        public Task AddAsync(GradeCurriculum entity) => Task.CompletedTask;
        public void Update(GradeCurriculum entity) { }
        public void Remove(GradeCurriculum entity) { }
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
            new FakeGradeCurriculumRepository(),
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
            new FakeGradeCurriculumRepository(),
            new FakeTeacherSubjectRepository(qualified: true),
            access: null!,
            new FakeCurrentUser(),
            Mapper());

        var dto = new ClassSubjectTeacherCreateDto(ClassId, SubjectId, TeacherId);

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(assignments.Added);
        Assert.Equal(SubjectId, result.SubjectId);
    }

    [Fact]
    public async Task Assigning_a_subject_that_is_not_in_the_grades_curriculum_is_rejected()
    {
        var classEntity = new Class { Id = ClassId, SchoolId = SchoolId, Name = "10A", Grade = 10 };
        var school = new School { Id = SchoolId, Name = "High School", Type = SchoolType.HighSchool };
        var assignments = new FakeAssignmentRepository();

        var service = new ClassSubjectTeacherService(
            assignments,
            new FakeClassRepository(classEntity),
            new FakeSchoolRepository(school),
            new FakeSubjectRepository(),
            new FakeGradeCurriculumRepository(onSyllabus: false),
            new FakeTeacherSubjectRepository(qualified: true),
            access: null!,
            new FakeCurrentUser(),
            Mapper());

        var dto = new ClassSubjectTeacherCreateDto(ClassId, SubjectId, TeacherId);

        var error = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(dto, CancellationToken.None));

        // The grade has a curriculum, so the message must say the subject is not on it --
        // not that the curriculum is missing, which would send the reader somewhere useless.
        Assert.Contains("not in the grade 10 curriculum", error.Message);
        Assert.Null(assignments.Added);
    }

    [Fact]
    public async Task Assigning_a_subject_when_the_grade_has_no_curriculum_says_so_specifically()
    {
        var classEntity = new Class { Id = ClassId, SchoolId = SchoolId, Name = "10A", Grade = 10 };
        var school = new School { Id = SchoolId, Name = "High School", Type = SchoolType.HighSchool };

        var service = new ClassSubjectTeacherService(
            new FakeAssignmentRepository(),
            new FakeClassRepository(classEntity),
            new FakeSchoolRepository(school),
            new FakeSubjectRepository(),
            new FakeGradeCurriculumRepository(onSyllabus: false, hasCurriculum: false),
            new FakeTeacherSubjectRepository(qualified: true),
            access: null!,
            new FakeCurrentUser(),
            Mapper());

        var dto = new ClassSubjectTeacherCreateDto(ClassId, SubjectId, TeacherId);

        var error = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(dto, CancellationToken.None));

        Assert.Contains("no curriculum yet", error.Message);
    }

    [Fact]
    public async Task The_curriculum_check_runs_before_the_qualification_check()
    {
        // Otherwise an off-syllabus request is reported as "this teacher is not qualified",
        // which points at the wrong fix: qualifying the teacher would still not make the
        // subject part of the grade.
        var classEntity = new Class { Id = ClassId, SchoolId = SchoolId, Name = "10A", Grade = 10 };
        var school = new School { Id = SchoolId, Name = "High School", Type = SchoolType.HighSchool };

        var service = new ClassSubjectTeacherService(
            new FakeAssignmentRepository(),
            new FakeClassRepository(classEntity),
            new FakeSchoolRepository(school),
            new FakeSubjectRepository(),
            new FakeGradeCurriculumRepository(onSyllabus: false),
            new FakeTeacherSubjectRepository(qualified: false),
            access: null!,
            new FakeCurrentUser(),
            Mapper());

        var dto = new ClassSubjectTeacherCreateDto(ClassId, SubjectId, TeacherId);

        var error = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(dto, CancellationToken.None));

        Assert.DoesNotContain("not qualified", error.Message);
    }
}
