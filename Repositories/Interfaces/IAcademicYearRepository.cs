using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAcademicYearRepository : IRepository<AcademicYear>
{
    Task<IEnumerable<AcademicYear>> GetAllOrderedAsync(CancellationToken ct);
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    Task<bool> OverlapsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);

    // Takes schoolId explicitly and bypasses the global filter, because a SuperAdmin has no
    // school of their own and would otherwise match nothing. Callers must have checked the
    // caller's access to that school first -- see StatisticsScopeResolver.
    Task<AcademicYear?> GetCurrentAsync(Guid schoolId, DateOnly onDate, CancellationToken ct);

    Task<AcademicYear?> GetByIdAcrossSchoolsAsync(Guid academicYearId, CancellationToken ct);
}
