using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IGradeCurriculumRepository : IRepository<GradeCurriculum>
{
    // Subjects taught in a grade, ordered by name.
    Task<IReadOnlyList<Subject>> GetSubjectsForGradeAsync(int grade, CancellationToken ct);

    // The whole curriculum for the school (every grade), for the management view.
    Task<IReadOnlyList<GradeCurriculum>> GetAllAsync(CancellationToken ct);

    Task<GradeCurriculum?> GetAsync(int grade, Guid subjectId, CancellationToken ct);

    // Is this subject on the grade's syllabus? Gates teaching assignments without loading
    // the subject rows themselves.
    Task<bool> IsInCurriculumAsync(int grade, Guid subjectId, CancellationToken ct);

    // Does this grade have a syllabus at all? Separates "not taught in this grade" from
    // "nobody has defined the syllabus yet" — the two need different error messages.
    Task<bool> HasCurriculumAsync(int grade, CancellationToken ct);
}
