using EduCore.Dtos.AcademicYear;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAcademicYearRepository : IRepository<AcademicYear>
{
    Task<IEnumerable<AcademicYear>> GetAllOrderedAsync(CancellationToken ct);

    Task<IEnumerable<AcademicYearListDto>> GetAllAcrossSchoolsAsync(Guid? schoolId, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    Task<bool> OverlapsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);

    Task<AcademicYear?> GetCurrentAsync(Guid schoolId, DateOnly onDate, CancellationToken ct);

    Task<AcademicYear?> GetByIdAcrossSchoolsAsync(Guid academicYearId, CancellationToken ct);
}
