using AutoMapper;
using EduCore.Dtos.ClassSubjectTeacher;
using EduCore.Dtos.Subject;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Assignment;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class SubjectService(
    ISubjectRepository subjects,
    IClassSubjectTeacherRepository assignments,
    IEnrollmentRepository enrollments,
    IElementaryAssignmentSync elementarySync,
    ICurrentUser currentUser,
    IMapper mapper) : ISubjectService
{
    // "The subjects" means something different per role, and the endpoint policy cannot say
    // so on its own. An administrator maintains the school's catalogue and needs all of it;
    // a teacher and a student are entitled only to the subjects they are actually part of.
    public async Task<IEnumerable<SubjectListDto>> GetAllAsync(CancellationToken ct)
    {
        if (currentUser.IsAdmin || currentUser.IsSuperAdmin)
            return mapper.Map<IEnumerable<SubjectListDto>>(await subjects.GetAllOrderedAsync(ct));

        if (currentUser.IsTeacher)
            return Distinct(
                await assignments.GetByTeacherAsync(currentUser.RequireTeacherId(), false, ct));

        // A student's subjects are whatever is taught to the class they are enrolled in. Not
        // being enrolled is an empty list, not an error: an unplaced student has no subjects.
        var enrollment = await enrollments.GetActiveForStudentAsync(currentUser.RequireStudentId(), ct);

        return enrollment is null
            ? []
            : Distinct(await assignments.GetByClassAsync(enrollment.ClassId, false, ct));
    }

    // One assignment row exists per (class, subject), so a teacher across several classes --
    // or a class with a subject split between teachers -- would otherwise repeat the subject.
    private static IEnumerable<SubjectListDto> Distinct(
        IEnumerable<ClassSubjectTeacherListDto> rows) =>
        rows.GroupBy(a => a.SubjectId)
            .Select(g => new SubjectListDto(g.Key, g.First().SubjectName))
            .OrderBy(s => s.Name);

    public async Task<SubjectListDto> GetByIdAsync(Guid subjectId, CancellationToken ct) =>
        mapper.Map<SubjectListDto>(await GetOwnedAsync(subjectId, ct));

    public async Task<SubjectListDto> CreateAsync(SubjectCreateDto dto, CancellationToken ct)
    {
        if (await subjects.NameExistsAsync(dto.Name, null, ct))
            throw new ConflictException($"A subject named {dto.Name} already exists in this school.");

        var subject = mapper.Map<Subject>(dto);
        subject.SchoolId = currentUser.RequireSchoolId();

        await subjects.AddAsync(subject);

        // Elementary: every class that already has a homeroom teacher gets this subject
        // assigned to them immediately. A no-op for any other school type.
        await elementarySync.SyncNewSubjectAsync(subject.SchoolId, subject.Id, ct);

        await subjects.SaveChangesAsync(ct);

        return mapper.Map<SubjectListDto>(subject);
    }

    public async Task<SubjectListDto> UpdateAsync(Guid subjectId, SubjectEditDto dto, CancellationToken ct)
    {
        var subject = await GetOwnedAsync(subjectId, ct);

        if (await subjects.NameExistsAsync(dto.Name, subjectId, ct))
            throw new ConflictException($"A subject named {dto.Name} already exists in this school.");

        mapper.Map(dto, subject);

        subjects.Update(subject);
        await subjects.SaveChangesAsync(ct);

        return mapper.Map<SubjectListDto>(subject);
    }

    public async Task DeleteAsync(Guid subjectId, CancellationToken ct)
    {
        var subject = await GetOwnedAsync(subjectId, ct);

        if (await subjects.IsInUseAsync(subjectId, ct))
            throw new ConflictException(
                "This subject is already used by teachers, lessons or marks and can no longer be deleted.");

        subjects.Remove(subject);
        await subjects.SaveChangesAsync(ct);
    }

    private async Task<Subject> GetOwnedAsync(Guid subjectId, CancellationToken ct) =>
        await subjects.GetByIdAsync(subjectId, ct)
        ?? throw new NotFoundException("Subject", subjectId);
}
