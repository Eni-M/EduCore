using EduCore.Dtos.AcademicYear;
using EduCore.Dtos.GradingPeriod;

namespace EduCore.Services.Interfaces;

public interface IAcademicYearService
{
    Task<IEnumerable<AcademicYearListDto>> GetAllAsync(CancellationToken ct);
    Task<AcademicYearListDto> GetByIdAsync(Guid academicYearId, CancellationToken ct);

    // Creates the two grading periods automatically -- an academic year always has exactly
    // two, so there is no separate "add a period" step.
    Task<AcademicYearListDto> CreateAsync(AcademicYearCreateDto dto, CancellationToken ct);

    Task<IEnumerable<GradingPeriodListDto>> GetGradingPeriodsAsync(Guid academicYearId, CancellationToken ct);
    Task<GradingPeriodListDto> EditGradingPeriodAsync(
        Guid gradingPeriodId, GradingPeriodEditDto dto, CancellationToken ct);
}
