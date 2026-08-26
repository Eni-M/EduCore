using EduCore.Dtos.Mark;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IMarkRepository : IRepository<Mark>
{
    Task<IEnumerable<MarkListDto>> GetByStudentAsync(
        Guid studentId, Guid? subjectId, Guid? gradingPeriodId, CancellationToken ct);

    Task<IEnumerable<MarkListDto>> GetByClassAsync(
        Guid classId, Guid? subjectId, Guid? gradingPeriodId, CancellationToken ct);

    Task<MarkListDto?> GetDtoAsync(Guid markId, CancellationToken ct);

    Task<Mark?> GetForUpsertAsync(
        Guid enrollmentId, Guid subjectId, Guid gradingPeriodId, CancellationToken ct);

    // The grading formula as a query, owned here rather than by the statistics module: a final
    // mark is real data about a student, not a statistic. Callers pass their own already
    // filtered query and get back only marks with both components entered.
    IQueryable<ValuedMark> Value(IQueryable<Mark> marks);

    // The same rule for a single pair of marks, for anything not working with a query.
    int? FinalMark(int? firstMark, int? secondMark);
}
