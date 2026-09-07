using AutoMapper;
using EduCore.Dtos.ClassSubjectTeacher;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class ClassSubjectTeacherService(
    IClassSubjectTeacherRepository assignments,
    IClassRepository classes,
    ISchoolRepository schools,
    ISubjectRepository subjects,
    IGradeCurriculumRepository curriculum,
    ITeacherSubjectRepository teacherSubjects,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : IClassSubjectTeacherService
{
    public async Task<IEnumerable<ClassSubjectTeacherListDto>> GetByClassAsync(
        Guid classId,
        bool includeEnded,
        CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);
        return await assignments.GetByClassAsync(classId, includeEnded, ct);
    }

    public async Task<IEnumerable<ClassSubjectTeacherListDto>> GetByTeacherAsync(
        Guid teacherId,
        bool includeEnded,
        CancellationToken ct)
    {
        if (currentUser.IsTeacher && teacherId != currentUser.TeacherId)
            throw new ForbiddenException("You can only view your own teaching assignments.");

        return await assignments.GetByTeacherAsync(teacherId, includeEnded, ct);
    }

    public async Task<ClassSubjectTeacherListDto> CreateAsync(
        ClassSubjectTeacherCreateDto dto,
        CancellationToken ct)
    {
        var classEntity = await classes.GetByIdAsync(dto.ClassId, ct)
                           ?? throw new NotFoundException("Class", dto.ClassId);
        var subject = await subjects.GetByIdAsync(dto.SubjectId, ct)
                      ?? throw new NotFoundException("Subject", dto.SubjectId);

        var school = await schools.GetByIdAsync(classEntity.SchoolId, ct)
                     ?? throw new NotFoundException("School", classEntity.SchoolId);
        GradeBands.Validate(school.Type, classEntity.Grade);

        // A class only teaches what its grade's curriculum says it teaches. Without this the
        // curriculum is advisory, and a class can drift onto subjects the grade never sits.
        if (!await curriculum.IsInCurriculumAsync(classEntity.Grade, dto.SubjectId, ct))
            throw new BusinessException(
                await curriculum.HasCurriculumAsync(classEntity.Grade, ct)
                    ? $"{subject.Name} is not in the grade {classEntity.Grade} curriculum. "
                      + "Add it to the curriculum first, then assign a teacher."
                    : $"Grade {classEntity.Grade} has no curriculum yet. "
                      + "Define which subjects the grade is taught before assigning teachers.");

        if (!await teacherSubjects.ExistsAsync(dto.TeacherId, dto.SubjectId, ct))
            throw new BusinessException("This teacher is not qualified to teach that subject.");

        if (await assignments.GetActiveAsync(dto.ClassId, dto.SubjectId, ct) is not null)
            throw new ConflictException(
                "This class already has an active teacher for that subject. End the current assignment first.");

        var assignment = mapper.Map<ClassSubjectTeacher>(dto);
        assignment.SchoolId = currentUser.RequireSchoolId();

        await assignments.AddAsync(assignment);
        await assignments.SaveChangesAsync(ct);

        return await assignments.GetDtoAsync(assignment.Id, ct)
               ?? throw new NotFoundException("Teaching assignment", assignment.Id);
    }

    public async Task EndAsync(Guid assignmentId, ClassSubjectTeacherEndDto dto, CancellationToken ct)
    {
        var assignment = await assignments.GetByIdAsync(assignmentId, ct)
                         ?? throw new NotFoundException("Teaching assignment", assignmentId);

        if (assignment.EndedOn is not null)
            throw new ConflictException("This teaching assignment has already been ended.");

        assignment.EndedOn = dto.EndedOn;

        assignments.Update(assignment);
        await assignments.SaveChangesAsync(ct);
    }
}
