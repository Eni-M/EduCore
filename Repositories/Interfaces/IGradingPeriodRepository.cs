using EduCore.Dtos.GradingPeriod;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IGradingPeriodRepository : IRepository<GradingPeriod>
{
    Task<IEnumerable<GradingPeriodListDto>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken ct);
    Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, CancellationToken ct);

    // The term a date falls in, within one academic year. Null when the date falls in a gap
    // between terms (a holiday) or outside the year entirely -- both are real answers the
    // caller has to handle, not errors to swallow here.
    Task<GradingPeriod?> GetContainingAsync(
        Guid academicYearId, DateOnly date, CancellationToken ct);

    // Bypasses the school filter, because a SuperAdmin has no school of their own and would
    // otherwise match nothing. Deliberately narrow: it returns the row so the caller can check
    // which school owns it, and StatisticsPeriodResolver is the only caller that does.
    Task<GradingPeriod?> GetByIdAcrossSchoolsAsync(Guid gradingPeriodId, CancellationToken ct);
}
