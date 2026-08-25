using EduCore.Dtos.Class;

namespace EduCore.Services.Interfaces;

public interface IClassService
{
    Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct);
    Task<ClassListDto> GetByIdAsync(Guid classId, CancellationToken ct);

    Task<IEnumerable<ClassListDto>> GetMineAsync(CancellationToken ct);

    Task<ClassListDto> CreateAsync(ClassCreateDto dto, CancellationToken ct);
    Task<ClassListDto> UpdateAsync(Guid classId, ClassEditDto dto, CancellationToken ct);
    Task<ClassListDto> AssignHomeroomTeacherAsync(
        Guid classId, ClassAssignHomeroomTeacherDto dto, CancellationToken ct);
}
