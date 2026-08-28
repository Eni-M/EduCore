using EduCore.Dtos.Pagination;
using EduCore.Dtos.Student;

namespace EduCore.Services.Interfaces;

public interface IStudentService
{
    Task<PagedResultDto<StudentListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, bool includeGraduated, CancellationToken ct);

    Task<IEnumerable<StudentListDto>> GetByClassAsync(Guid classId, CancellationToken ct);
    Task<StudentDetailDto> GetByIdAsync(Guid studentId, CancellationToken ct);
    Task<StudentProfileDto> GetProfileAsync(Guid studentId, CancellationToken ct);

    Task<StudentDetailDto> CreateAsync(StudentCreateDto dto, CancellationToken ct);
    Task<StudentDetailDto> UpdateAsync(Guid studentId, StudentEditDto dto, CancellationToken ct);

    // The same rule GraduateAsync enforces, as a question rather than an action, so the
    // screen can show the button in the state the API would actually answer with.
    Task<GraduationEligibilityDto> GetGraduationEligibilityAsync(Guid studentId, CancellationToken ct);

    Task GraduateAsync(Guid studentId, StudentGraduateDto dto, CancellationToken ct);
}
