using EduCore.Dtos.Curriculum;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using EduCore.Services.Assignment;
using Xunit;

namespace EduCore.Tests;

public class CurriculumServiceTests
{
    private static readonly Guid SchoolId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => UserRole.Directorate;
        public Guid? SchoolId => CurriculumServiceTests.SchoolId;
        public IReadOnlyCollection<Guid> SchoolIds => [CurriculumServiceTests.SchoolId];
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

    private sealed class FakeSchoolRepository(SchoolType type) : ISchoolRepository
    {
        public Task<(IReadOnlyList<School> Items, int TotalCount)> GetPagedAsync(
            bool activeOnly, Guid? cityId, Guid? stateId,
            EduCore.Dtos.Pagination.PaginationQueryDto pagination, string? search, CancellationToken ct) =>
            Task.FromResult<(IReadOnlyList<School>, int)>(([], 0));
        public Task<School?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<School?>(new School { Id = SchoolId, Name = "S", Type = type });
        public Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct) => Task.FromResult<School?>(null);
        public Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<EduCore.Dtos.Pagination.PagedResultDto<School>> GetOrderedPagedAsync(
            bool activeOnly, EduCore.Dtos.Pagination.PaginationQueryDto pagination, CancellationToken ct) =>
            Task.FromResult(new EduCore.Dtos.Pagination.PagedResultDto<School>([], 1, 20, 0));
        public Task<IEnumerable<School>> GetByCityAsync(Guid cityId, bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<IEnumerable<School>> GetByStateAsync(Guid stateId, bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct) => Task.FromResult(false);
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
        public Task<bool> NameExistsAsync(string name, Guid? excludeSubjectId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> IsInUseAsync(Guid subjectId, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(Subject entity) => Task.CompletedTask;
        public void Update(Subject entity) { }
        public void Remove(Subject entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeCurriculumRepository(GradeCurriculum? existing) : IGradeCurriculumRepository
    {
        public GradeCurriculum? Added;
        public Task<IReadOnlyList<Subject>> GetSubjectsForGradeAsync(int grade, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Subject>>([]);
        public Task<IReadOnlyList<GradeCurriculum>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<GradeCurriculum>>([]);
        public Task<GradeCurriculum?> GetAsync(int grade, Guid subjectId, CancellationToken ct) =>
            Task.FromResult(existing);
        public Task<bool> IsInCurriculumAsync(int grade, Guid subjectId, CancellationToken ct) =>
            Task.FromResult(true);
        public Task<bool> HasCurriculumAsync(int grade, CancellationToken ct) =>
            Task.FromResult(true);
        public Task<GradeCurriculum?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<GradeCurriculum?>(null);
        public Task AddAsync(GradeCurriculum entity) { Added = entity; return Task.CompletedTask; }
        public void Update(GradeCurriculum entity) { }
        public void Remove(GradeCurriculum entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class SpyElementarySync : IElementaryAssignmentSync
    {
        public bool SubjectAddedCalled;
        public Task SyncHomeroomChangeAsync(
            Guid schoolId, Guid classId, int grade, Guid? oldTeacherId, Guid? newTeacherId, CancellationToken ct) =>
            Task.CompletedTask;
        public Task SyncCurriculumSubjectAddedAsync(Guid schoolId, int grade, Guid subjectId, CancellationToken ct)
        {
            SubjectAddedCalled = true;
            return Task.CompletedTask;
        }
    }

    private static CurriculumService Service(
        SchoolType type, GradeCurriculum? existing, SpyElementarySync sync, FakeCurriculumRepository? repo = null) =>
        new(repo ?? new FakeCurriculumRepository(existing),
            new FakeSubjectRepository(),
            new FakeSchoolRepository(type),
            sync,
            new FakeCurrentUser());

    [Fact]
    public async Task Adding_a_grade_outside_the_school_band_throws()
    {
        var service = Service(SchoolType.Elementary, existing: null, new SpyElementarySync());

        // Grade 6 is junior-high, not elementary.
        await Assert.ThrowsAsync<BusinessException>(
            () => service.AddAsync(new GradeCurriculumAddDto(6, SubjectId), CancellationToken.None));
    }

    [Fact]
    public async Task Adding_a_subject_already_in_the_grade_conflicts()
    {
        var existing = new GradeCurriculum { Id = Guid.NewGuid(), Grade = 3, SubjectId = SubjectId, SchoolId = SchoolId };
        var service = Service(SchoolType.Elementary, existing, new SpyElementarySync());

        await Assert.ThrowsAsync<ConflictException>(
            () => service.AddAsync(new GradeCurriculumAddDto(3, SubjectId), CancellationToken.None));
    }

    [Fact]
    public async Task Adding_a_valid_subject_saves_and_triggers_elementary_sync()
    {
        var repo = new FakeCurriculumRepository(existing: null);
        var sync = new SpyElementarySync();
        var service = Service(SchoolType.Elementary, existing: null, sync, repo);

        var result = await service.AddAsync(new GradeCurriculumAddDto(3, SubjectId), CancellationToken.None);

        Assert.NotNull(repo.Added);
        Assert.Equal(3, result.Grade);
        Assert.Equal("Mathematics", result.SubjectName);
        Assert.True(sync.SubjectAddedCalled);
    }
}
