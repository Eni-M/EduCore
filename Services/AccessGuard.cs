using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class AccessGuard(
    IClassRepository classes,
    IEnrollmentRepository enrollments,
    IClassSubjectTeacherRepository assignments,
    IGuardianRepository guardians,
    ICurrentUser currentUser) : IAccessGuard
{
    public async Task EnsureCanViewClassAsync(Guid classId, CancellationToken ct)
    {
        var @class = await classes.GetByIdAsync(classId, ct)
                     ?? throw new NotFoundException("Class", classId);

        if (currentUser.IsDirectorate)
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

        if (currentUser.IsGuardian
            && await guardians.HasChildInClassAsync(currentUser.RequireGuardianId(), classId, ct))
            return;

        throw new ForbiddenException("You do not have access to this class.");
    }

    public async Task EnsureCanTeachClassAsync(Guid classId, CancellationToken ct)
    {
        _ = await classes.GetByIdAsync(classId, ct) ?? throw new NotFoundException("Class", classId);

        if (currentUser.IsDirectorate)
            return;

        if (currentUser.IsTeacher
            && await assignments.TeachesClassAsync(currentUser.RequireTeacherId(), classId, ct))
            return;

        throw new ForbiddenException("You are not assigned to teach this class.");
    }

    public async Task EnsureCanViewStudentAsync(Guid studentId, CancellationToken ct)
    {
        if (currentUser.IsDirectorate)
            return;

        if (currentUser.IsStudent)
        {
            if (currentUser.StudentId == studentId)
                return;

            throw new ForbiddenException("You can only view your own record.");
        }

        if (currentUser.IsGuardian)
        {
            if (await guardians.LinkExistsAsync(currentUser.RequireGuardianId(), studentId, ct))
                return;

            throw new ForbiddenException("You can only view the record of a student in your care.");
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

    public async Task EnsureCanViewStudentMedicalAsync(Guid studentId, CancellationToken ct)
    {
        if (currentUser.IsDirectorate)
            return;

        if (currentUser.IsStudent)
        {
            if (currentUser.StudentId == studentId)
                return;

            throw new ForbiddenException("You can only view your own medical record.");
        }

        if (currentUser.IsGuardian)
        {
            if (await guardians.LinkExistsAsync(currentUser.RequireGuardianId(), studentId, ct))
                return;

            throw new ForbiddenException("You can only view the medical record of a student in your care.");
        }

        if (currentUser.IsTeacher)
        {
            // Deliberately narrower than viewing the student at large: teaching the class is not
            // enough, only being its homeroom teacher -- the one person responsible for the child.
            var enrollment = await enrollments.GetActiveForStudentAsync(studentId, ct);

            if (enrollment is not null)
            {
                var @class = await classes.GetByIdAsync(enrollment.ClassId, ct);

                if (@class?.HomeroomTeacherId == currentUser.RequireTeacherId())
                    return;
            }

            throw new ForbiddenException("Only the student's homeroom teacher may view their medical record.");
        }

        throw new ForbiddenException("You do not have access to this student's medical record.");
    }
}
