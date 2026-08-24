using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    // ---------- Single-record lookups ----------
    Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Student?> GetWithUserAsync(Guid studentId, CancellationToken ct);
    Task<Student?> GetWithSchoolAsync(Guid studentId, CancellationToken ct);
    Task<Student?> GetWithParentsAsync(Guid studentId, CancellationToken ct);
    Task<Student?> GetProfileAsync(Guid studentId, CancellationToken ct);

    // ---------- Identity / uniqueness ----------
    Task<Student?> GetByPersonalNumberAsync(string personalNumber, CancellationToken ct);
    Task<bool> PersonalNumberExistsAsync(string personalNumber, Guid? excludeStudentId, CancellationToken ct);

    // ---------- Listing ----------

    Task<IEnumerable<Student>> GetAllWithUserAsync(bool includeGraduated, CancellationToken ct);
    Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool includeGraduated,
        CancellationToken ct);

    Task<IEnumerable<Student>> GetByIdsAsync(IEnumerable<Guid> studentIds, CancellationToken ct);

    // ---------- Class / enrollment ----------

    // Students with a currently-active enrollment (LeftAt == null) in the given class.
    Task<IEnumerable<Student>> GetByClassAsync(Guid classId, CancellationToken ct);

    Task<IEnumerable<Student>> GetUnassignedAsync(Guid academicYearId, CancellationToken ct);
    Task<bool> IsEnrolledInClassAsync(Guid studentId, Guid classId, CancellationToken ct);
    Task<Enrollment?> GetActiveEnrollmentAsync(Guid studentId, CancellationToken ct);
    Task<int> CountActiveInClassAsync(Guid classId, CancellationToken ct);

    // ---------- Guardians ----------

    Task<IEnumerable<Student>> GetByParentPhoneAsync(string phoneNumber, CancellationToken ct);
}
