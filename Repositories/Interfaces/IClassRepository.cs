using EduCore.Dtos.Class;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IClassRepository : IRepository<Class>
{
    Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct);
    Task<ClassListDto?> GetDtoAsync(Guid classId, CancellationToken ct);
    Task<IEnumerable<ClassListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, Guid? excludeClassId, CancellationToken ct);

    // Entities, not DTOs: SyncNewSubjectAsync needs each class's id and homeroom teacher id
    // to create assignments, not a display projection.
    Task<IEnumerable<Class>> GetAllWithHomeroomTeacherAsync(CancellationToken ct);
}
