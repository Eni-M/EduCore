using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IGradeCurriculumRepository : IRepository<GradeCurriculum>
{
    // Subjects taught in a grade, ordered by name.
    Task<IReadOnlyList<Subject>> GetSubjectsForGradeAsync(int grade, CancellationToken ct);

    // The whole curriculum for the school (every grade), for the management view.
    Task<IReadOnlyList<GradeCurriculum>> GetAllAsync(CancellationToken ct);

    Task<GradeCurriculum?> GetAsync(int grade, Guid subjectId, CancellationToken ct);
}
