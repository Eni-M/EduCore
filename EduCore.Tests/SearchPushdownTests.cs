using AutoMapper;
using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Mapping;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

// These services used to load a whole table and filter it in memory, so a search for one row
// still read every row. The repository now does both, and what these tests hold in place is
// that the service hands the search term down rather than doing the work itself.
public class SearchPushdownTests
{
    private static readonly Guid CityId = Guid.NewGuid();
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid SchoolId = Guid.NewGuid();

    private sealed class FakeCurrentUser(UserRole role) : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => role;
        public Guid? SchoolId => SearchPushdownTests.SchoolId;
        public IReadOnlyCollection<Guid> SchoolIds => [SearchPushdownTests.SchoolId];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => SearchPushdownTests.CityId;
        public Guid? StateId => SearchPushdownTests.StateId;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => role == UserRole.StateAdmin;
        public bool IsRegionalAdmin => role == UserRole.RegionalAdmin;
        public bool IsDirectorate => role == UserRole.Directorate;
        public bool IsTeacher => role == UserRole.Teacher;
        public bool IsStudent => role == UserRole.Student;
        public bool IsGuardian => role == UserRole.Guardian;
    }

    private static IMapper Mapper() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory).CreateMapper();

    private static readonly Microsoft.Extensions.Logging.ILoggerFactory NullLoggerFactory =
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

    // ---- Subjects ---------------------------------------------------------

    private sealed class RecordingSubjectRepository : ISubjectRepository
    {
        public string? SawSearch;
        public bool LoadedEverything;

        public Task<(IReadOnlyList<Subject> Items, int TotalCount)> GetPagedAsync(
            PaginationQueryDto pagination, string? search, CancellationToken ct)
        {
            SawSearch = search;
            return Task.FromResult<(IReadOnlyList<Subject>, int)>(
                ([new Subject { Id = Guid.NewGuid(), Name = "Mathematics", SchoolId = SchoolId }], 1));
        }

        public Task<IEnumerable<Subject>> GetAllOrderedAsync(CancellationToken ct)
        {
            LoadedEverything = true;
            return Task.FromResult<IEnumerable<Subject>>([]);
        }

        public Task<bool> NameExistsAsync(string name, Guid? excludeSubjectId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<bool> IsInUseAsync(Guid subjectId, CancellationToken ct) => Task.FromResult(false);
        public Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Subject?>(null);
        public Task<IEnumerable<Subject>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<Subject>>([]);
        public Task AddAsync(Subject entity) => Task.CompletedTask;
        public void Update(Subject entity) { }
        public void Remove(Subject entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    [Fact]
    public async Task Subject_search_goes_to_the_repository_and_not_through_the_whole_catalogue()
    {
        var repo = new RecordingSubjectRepository();
        var service = new SubjectService(
            repo, null!, null!, new FakeCurrentUser(UserRole.Directorate), Mapper());

        var result = await service.GetPagedAsync(
            new PaginationQueryDto(1, 20), "math", TestContext.Current.CancellationToken);

        Assert.Equal("math", repo.SawSearch);
        Assert.False(repo.LoadedEverything);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    // ---- Regional admins --------------------------------------------------

    private sealed class RecordingRegionalAdminRepository : IRegionalAdminRepository
    {
        public string? SawSearch;
        public Guid? SawStateId;
        public bool LoadedEverything;

        public Task<(IReadOnlyList<RegionalAdmin> Items, int TotalCount)> GetPagedAsync(
            Guid stateId, PaginationQueryDto pagination, string? search, CancellationToken ct)
        {
            SawSearch = search;
            SawStateId = stateId;
            return Task.FromResult<(IReadOnlyList<RegionalAdmin>, int)>(([], 0));
        }

        public Task<IEnumerable<RegionalAdmin>> GetByStateAsync(Guid stateId, CancellationToken ct)
        {
            LoadedEverything = true;
            return Task.FromResult<IEnumerable<RegionalAdmin>>([]);
        }

        public Task<RegionalAdmin?> GetWithUserAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<RegionalAdmin?>(null);
        public Task<RegionalAdmin?> GetTrackedAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<RegionalAdmin?>(null);
        public Task<RegionalAdmin?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<RegionalAdmin?>(null);
        public Task<RegionalAdmin?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<RegionalAdmin?>(null);
        public Task<IEnumerable<RegionalAdmin>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<RegionalAdmin>>([]);
        public Task AddAsync(RegionalAdmin entity) => Task.CompletedTask;
        public void Update(RegionalAdmin entity) { }
        public void Remove(RegionalAdmin entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    [Fact]
    public async Task Regional_admin_search_is_scoped_to_the_callers_state_in_the_query()
    {
        var repo = new RecordingRegionalAdminRepository();
        var service = new RegionalAdminService(
            repo, null!, null!, null!, new FakeCurrentUser(UserRole.StateAdmin), Mapper());

        await service.GetPagedAsync(
            new PaginationQueryDto(1, 20), "ana", TestContext.Current.CancellationToken);

        Assert.Equal("ana", repo.SawSearch);
        Assert.Equal(StateId, repo.SawStateId);
        Assert.False(repo.LoadedEverything);
    }
}
