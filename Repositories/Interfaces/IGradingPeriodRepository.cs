using EduCore.Dtos.GradingPeriod;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IGradingPeriodRepository : IRepository<GradingPeriod>
{
    Task<IEnumerable<GradingPeriodListDto>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken ct);
    Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, CancellationToken ct);

    Task<GradingPeriod?> GetContainingAsync(
        Guid academicYearId, DateOnly date, CancellationToken ct);

    Task<GradingPeriod?> GetByIdAcrossSchoolsAsync(Guid gradingPeriodId, CancellationToken ct);
}
