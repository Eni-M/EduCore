using EduCore.Dtos.AcademicYear;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAcademicYearRepository : IRepository<AcademicYear>
{
    // The scoped list: the global filter narrows this to the caller's own school.
    Task<IEnumerable<AcademicYear>> GetAllOrderedAsync(CancellationToken ct);

    // Every school's years, newest first, optionally narrowed to one school. Bypasses the
    // global filter for the same reason GetCurrentAsync does: a SuperAdmin has no school of
    // their own, so the filter would match nothing and hand them an empty list.
    Task<IEnumerable<AcademicYearListDto>> GetAllAcrossSchoolsAsync(Guid? schoolId, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    Task<bool> OverlapsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);

    // Takes schoolId explicitly and bypasses the global filter, because a SuperAdmin has no
    // school of their own and would otherwise match nothing. Callers must have checked the
    // caller's access to that school first -- see StatisticsScopeResolver.
    Task<AcademicYear?> GetCurrentAsync(Guid schoolId, DateOnly onDate, CancellationToken ct);

    Task<AcademicYear?> GetByIdAcrossSchoolsAsync(Guid academicYearId, CancellationToken ct);
}
