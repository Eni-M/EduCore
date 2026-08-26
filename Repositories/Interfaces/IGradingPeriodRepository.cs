using EduCore.Dtos.GradingPeriod;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IGradingPeriodRepository : IRepository<GradingPeriod>
{
    Task<IEnumerable<GradingPeriodListDto>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken ct);
    Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, CancellationToken ct);

    // Bypasses the school filter, because a SuperAdmin has no school of their own and would
    // otherwise match nothing. Deliberately narrow: it returns the row so the caller can check
    // which school owns it, and StatisticsPeriodResolver is the only caller that does.
    Task<GradingPeriod?> GetByIdAcrossSchoolsAsync(Guid gradingPeriodId, CancellationToken ct);
}
