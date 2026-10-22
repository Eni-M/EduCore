using EduCore.Dtos.Statistics.Rows;
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

    IQueryable<ValuedMark> Value(IQueryable<Mark> marks);

    IQueryable<double> WeightedValues(IQueryable<Mark> marks);
    void ExpectVersion(Mark mark, uint version);
    void AddChange(MarkChange change);
    Task<IReadOnlyList<MarkChangeDto>> GetChangesAsync(Guid markId, CancellationToken ct);

    int? FinalMark(int? firstMark, int? secondMark);
}
