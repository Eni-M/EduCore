using EduCore.Dtos.Curriculum;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Assignment;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class CurriculumService(
    IGradeCurriculumRepository curriculum,
    ISubjectRepository subjects,
    ISchoolRepository schools,
    IElementaryAssignmentSync elementarySync,
    ICurrentUser currentUser) : ICurriculumService
{
    public async Task<IEnumerable<GradeCurriculumEntryDto>> GetAllAsync(CancellationToken ct) =>
        (await curriculum.GetAllAsync(ct))
        .Select(gc => new GradeCurriculumEntryDto(gc.Id, gc.Grade, gc.SubjectId, gc.Subject.Name));

    public async Task<GradeCurriculumEntryDto> AddAsync(GradeCurriculumAddDto dto, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();
        var school = await schools.GetByIdAsync(schoolId, ct)
                     ?? throw new NotFoundException("School", schoolId);
        GradeBands.Validate(school.Type, dto.Grade);

        var subject = await subjects.GetByIdAsync(dto.SubjectId, ct)
                      ?? throw new NotFoundException("Subject", dto.SubjectId);

        if (await curriculum.GetAsync(dto.Grade, dto.SubjectId, ct) is not null)
            throw new ConflictException($"{subject.Name} is already in the grade {dto.Grade} curriculum.");

        var entry = new GradeCurriculum
        {
            SchoolId = schoolId,
            Grade = dto.Grade,
            SubjectId = dto.SubjectId,
        };

        await curriculum.AddAsync(entry);

        // In an elementary school, existing classes of this grade should immediately gain the
        // subject under their homeroom teacher. No-op for junior-high / high school.
        await elementarySync.SyncCurriculumSubjectAddedAsync(schoolId, dto.Grade, dto.SubjectId, ct);

        await curriculum.SaveChangesAsync(ct);

        return new GradeCurriculumEntryDto(entry.Id, entry.Grade, entry.SubjectId, subject.Name);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct)
    {
        var entry = await curriculum.GetByIdAsync(id, ct)
                    ?? throw new NotFoundException("Curriculum entry", id);

        // Removing a subject from a grade stops future auto-assignment. Existing teaching
        // assignments are left as they are; end them from the class page if needed.
        curriculum.Remove(entry);
        await curriculum.SaveChangesAsync(ct);
    }
}
