using EduCore.Dtos.Assignment;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using EduCore.Services.Storage;

namespace EduCore.Services.Assignments;

public class AssignmentService(
    IAssignmentRepository assignments,
    IClassSubjectTeacherRepository assignmentsToClasses,
    IAccessGuard access,
    ICurrentUser currentUser,
    IAssignmentSubmissionRepository submissions,
    IFileStorage storage,
    IOptions<StorageOptions> storageOptions) : IAssignmentService
{
    private readonly StorageOptions _storageOptions = storageOptions.Value;

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

        if (request.DueDate <= DateTimeOffset.UtcNow)
            throw new BusinessException(
                "The due date must be in the future.");

        var assignment = new Assignment
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
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await assignments.AddAsync(assignment);
        await assignments.SaveChangesAsync(ct);

        return await GetDetailsAsync(assignment.Id, ct);
    }

    public async Task<AssignmentDetailsDto> UploadAssignmentFileAsync(
        Guid assignmentId,
        IFormFile file,
        CancellationToken ct)
    {
        _storageOptions.EnsureAccepted(file.Length, file.ContentType);

        var assignmentEntity = await assignments.GetByIdAsync(assignmentId, ct);

        if (assignmentEntity is null)
            throw new NotFoundException("Assignment", assignmentId);

        // Ensure the user can teach this class
        await access.EnsureCanTeachClassAsync(assignmentEntity.ClassId, ct);

        // Only the teacher who created/owns the assignment may attach or modify the
        // assignment-level file. Prevent another teacher who also teaches the same
        // class/subject from modifying it.
        var currentTeacherId = currentUser.RequireTeacherId();
        if (assignmentEntity.TeacherId != currentTeacherId)
            throw new ForbiddenException("You are not the teacher for this assignment.");

        var extension = Path.GetExtension(file.FileName) ?? string.Empty;

        await using var stream = file.OpenReadStream();
        var key = await storage.SaveAsync(stream, $"assignments/{assignmentId}", extension, ct);

        if (!string.IsNullOrEmpty(assignmentEntity.StorageKey))
            await storage.DeleteAsync(assignmentEntity.StorageKey, ct);

        assignmentEntity.FileName = file.FileName;
        assignmentEntity.ContentType = file.ContentType;
        assignmentEntity.SizeBytes = file.Length;
        assignmentEntity.StorageKey = key;
        assignmentEntity.UpdatedAt = DateTimeOffset.UtcNow;

        assignments.Update(assignmentEntity);
        await assignments.SaveChangesAsync(ct);

        return await GetDetailsAsync(assignmentId, ct);
    }

    public async Task<AssignmentSubmissionDto> SubmitAssignmentAsync(
        Guid assignmentId,
        IFormFile file,
        CancellationToken ct)
    {
        var studentId = currentUser.RequireStudentId();

        _storageOptions.EnsureAccepted(file.Length, file.ContentType);

        var assignment = await assignments.GetByIdAsync(assignmentId, ct);

        if (assignment is null)
            throw new NotFoundException("Assignment", assignmentId);

        await access.EnsureCanViewClassAsync(assignment.ClassId, ct);

        var existing = await submissions.GetByAssignmentAndStudentAsync(assignmentId, studentId, ct);

        var extension = Path.GetExtension(file.FileName) ?? string.Empty;

        await using var stream = file.OpenReadStream();
        var key = await storage.SaveAsync(stream, $"submissions/{assignmentId}/{studentId}", extension, ct);

        if (existing is not null && !string.IsNullOrEmpty(existing.StorageKey))
            await storage.DeleteAsync(existing.StorageKey, ct);

        if (existing is null)
        {
            var submission = new AssignmentSubmission
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                StudentId = studentId,
                SchoolId = currentUser.RequireSchoolId(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                StorageKey = key,
                SubmittedAt = DateTimeOffset.UtcNow
            };

            await submissions.AddAsync(submission);
        }
        else
        {
            existing.FileName = file.FileName;
            existing.ContentType = file.ContentType;
            existing.SizeBytes = file.Length;
            existing.StorageKey = key;
            existing.SubmittedAt = DateTimeOffset.UtcNow;

            submissions.Update(existing);
        }

        await submissions.SaveChangesAsync(ct);

        var dto = await submissions.GetForStudentAsync(assignmentId, studentId, ct);

        return dto ?? new AssignmentSubmissionDto(null, assignmentId, studentId, "", "Missing", null, null, null, null, null, null);
    }

    public async Task<AssignmentSubmissionDto> GetMySubmissionAsync(
        Guid assignmentId,
        CancellationToken ct)
    {
        var studentId = currentUser.RequireStudentId();

        var dto = await submissions.GetForStudentAsync(assignmentId, studentId, ct);

        if (dto is null)
            return new AssignmentSubmissionDto(null, assignmentId, studentId, string.Empty, "Missing", null, null, null, null, null, null);

        return dto;
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