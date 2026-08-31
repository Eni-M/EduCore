using EduCore.Dtos.AcademicYear;
using EduCore.Dtos.GradingPeriod;

namespace EduCore.Services.Interfaces;

public interface IAcademicYearService
{
    Task<IEnumerable<AcademicYearListDto>> GetAllAsync(Guid? schoolId, CancellationToken ct);
    Task<AcademicYearListDto> GetByIdAsync(Guid academicYearId, CancellationToken ct);

    Task<AcademicYearListDto> CreateAsync(AcademicYearCreateDto dto, CancellationToken ct);

    Task<IEnumerable<GradingPeriodListDto>> GetGradingPeriodsAsync(Guid academicYearId, CancellationToken ct);
    Task<GradingPeriodListDto> EditGradingPeriodAsync(
        Guid gradingPeriodId, GradingPeriodEditDto dto, CancellationToken ct);
}
