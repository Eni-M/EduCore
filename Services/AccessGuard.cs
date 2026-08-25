using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class AccessGuard(
    IClassRepository classes,
    IEnrollmentRepository enrollments,
    IClassSubjectTeacherRepository assignments,
    ICurrentUser currentUser) : IAccessGuard
{
    public async Task EnsureCanViewClassAsync(Guid classId, CancellationToken ct)
    {
        var @class = await classes.GetByIdAsync(classId, ct)
                     ?? throw new NotFoundException("Class", classId);

        if (currentUser.IsAdmin || currentUser.IsSuperAdmin)
            return;

        if (currentUser.IsTeacher)
        {
            var teacherId = currentUser.RequireTeacherId();

            if (@class.HomeroomTeacherId == teacherId
                || await assignments.TeachesClassAsync(teacherId, classId, ct))
                return;
        }

        if (currentUser.IsStudent)
        {
            var enrollment = await enrollments.GetActiveForStudentAsync(currentUser.RequireStudentId(), ct);

            if (enrollment?.ClassId == classId)
                return;
        }

        throw new ForbiddenException("You do not have access to this class.");
    }

    public async Task EnsureCanTeachClassAsync(Guid classId, CancellationToken ct)
    {
        _ = await classes.GetByIdAsync(classId, ct) ?? throw new NotFoundException("Class", classId);

        if (currentUser.IsAdmin || currentUser.IsSuperAdmin)
            return;

        if (currentUser.IsTeacher
            && await assignments.TeachesClassAsync(currentUser.RequireTeacherId(), classId, ct))
            return;

        throw new ForbiddenException("You are not assigned to teach this class.");
    }

    public async Task EnsureCanViewStudentAsync(Guid studentId, CancellationToken ct)
    {
        if (currentUser.IsAdmin || currentUser.IsSuperAdmin)
            return;

        if (currentUser.IsStudent)
        {
            if (currentUser.StudentId == studentId)
                return;

            throw new ForbiddenException("You can only view your own record.");
        }

        if (currentUser.IsTeacher)
        {
            var enrollment = await enrollments.GetActiveForStudentAsync(studentId, ct);

            if (enrollment is not null)
            {
                await EnsureCanViewClassAsync(enrollment.ClassId, ct);
                return;
            }
        }

        throw new ForbiddenException("You do not have access to this student.");
    }
}
