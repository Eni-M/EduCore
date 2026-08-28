using EduCore.Dtos.AcademicYear;
using EduCore.Dtos.GradingPeriod;

namespace EduCore.Services.Interfaces;

public interface IAcademicYearService
{
    // schoolId narrows a SuperAdmin's cross-school list to one school. It is ignored for
    // everyone else: an Admin's list is their own school by query filter, and honouring the
    // parameter for them would be a way to ask about somebody else's.
    Task<IEnumerable<AcademicYearListDto>> GetAllAsync(Guid? schoolId, CancellationToken ct);
    Task<AcademicYearListDto> GetByIdAsync(Guid academicYearId, CancellationToken ct);

    // Creates the two grading periods automatically -- an academic year always has exactly
    // two, so there is no separate "add a period" step.
    Task<AcademicYearListDto> CreateAsync(AcademicYearCreateDto dto, CancellationToken ct);

    Task<IEnumerable<GradingPeriodListDto>> GetGradingPeriodsAsync(Guid academicYearId, CancellationToken ct);
    Task<GradingPeriodListDto> EditGradingPeriodAsync(
        Guid gradingPeriodId, GradingPeriodEditDto dto, CancellationToken ct);
}
