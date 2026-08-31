using EduCore.Dtos.Class;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IClassRepository : IRepository<Class>
{
    Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct);
    Task<ClassListDto?> GetDtoAsync(Guid classId, CancellationToken ct);
    Task<IEnumerable<ClassListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, Guid? excludeClassId, CancellationToken ct);

    Task<IEnumerable<Class>> GetAllWithHomeroomTeacherAsync(CancellationToken ct);
}
