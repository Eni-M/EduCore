using EduCore.Dtos.Assignment;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services.Assignments;

public class AssignmentService(
    IAssignmentRepository assignments,
    IClassSubjectTeacherRepository assignmentsToClasses,
    IAccessGuard access,
    ICurrentUser currentUser) : IAssignmentService
{
    public async Task<AssignmentDetailsDto> CreateAsync(
        CreateAssignmentRequest request,
        CancellationToken ct)
    {
        var teacherId = currentUser.RequireTeacherId();

        await access.EnsureCanTeachClassAsync(request.ClassId, ct);

        var classSubjectTeacher =
            await assignmentsToClasses.GetActiveAsync(
                request.ClassId,
                request.SubjectId,
                ct);

        if (classSubjectTeacher is null)
            throw new BusinessException(
                "The teacher is not assigned to this subject in this class.");

        if (classSubjectTeacher.TeacherId != teacherId)
            throw new ForbiddenException("The current user is not the teacher for this class subject.");

        if (request.DueDate <= DateTime.UtcNow)
            throw new BusinessException(
                "The due date must be in the future.");

        var assignment = new global::EduCore.Models.Assignment
        {
            Id = Guid.NewGuid(),
            SchoolId = currentUser.RequireSchoolId(),
            ClassSubjectTeacherId = classSubjectTeacher.Id,
            ClassId = classSubjectTeacher.ClassId,
            SubjectId = classSubjectTeacher.SubjectId,
            TeacherId = classSubjectTeacher.TeacherId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await assignments.AddAsync(assignment);
        await assignments.SaveChangesAsync(ct);

        return await GetDetailsAsync(assignment.Id, ct);
    }

    public async Task<IEnumerable<AssignmentListDto>> GetByClassAsync(
        Guid classId,
        CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);

        return await assignments.GetByClassAsync(classId, ct);
    }

    public async Task<IEnumerable<AssignmentListDto>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken ct)
    {
        var currentTeacherId = currentUser.RequireTeacherId();

        if (currentTeacherId != teacherId)
            throw new ForbiddenException("The current user is not the requested teacher.");

        return await assignments.GetByTeacherAsync(
            teacherId,
            ct);
    }

    public async Task<AssignmentDetailsDto> GetDetailsAsync(
        Guid assignmentId,
        CancellationToken ct)
    {
        var assignment = await assignments.GetDetailsAsync(
            assignmentId,
            ct);

        if (assignment is null)
            throw new NotFoundException(
                "Assignment",
                assignmentId);

        await access.EnsureCanViewClassAsync(
            assignment.ClassId,
            ct);

        return assignment;
    }
}