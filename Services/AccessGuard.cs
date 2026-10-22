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
    ILocationRepository locations,
    ICurrentUser currentUser) : IAccessGuard
{
    public async Task EnsureCanManageSchoolAsync(Guid? schoolCityId, CancellationToken ct)
    {
        if (currentUser.IsRegionalAdmin)
        {
            if (schoolCityId is null || schoolCityId != currentUser.RequireCityId())
                throw new ForbiddenException("This school is not in your city.");

            return;
        }

        if (currentUser.IsStateAdmin)
        {
            var city = schoolCityId is { } cityId ? await locations.GetCityAsync(cityId, ct) : null;
            if (city?.StateId != currentUser.RequireStateId())
                throw new ForbiddenException("This school is not in your state.");

            return;
        }

        throw new ForbiddenException("You are not allowed to manage this school.");
    }

    public void EnsureCanWriteMarks(Guid assignedTeacherId)
    {
        if (currentUser.IsDirectorate)
            return;

        if (currentUser.IsTeacher && currentUser.TeacherId == assignedTeacherId)
            return;

        throw new ForbiddenException("Only the teacher assigned to this class-subject can give marks for it.");
    }

    public void EnsureConversationParticipant(Guid guardianId, Guid teacherId)
    {
        if ((currentUser.IsGuardian && currentUser.GuardianId == guardianId)
            || (currentUser.IsTeacher && currentUser.TeacherId == teacherId))
            return;

        throw new ForbiddenException("You do not have access to this conversation.");
    }

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

    public async Task<bool> CanViewStudentMedicalAsync(Guid studentId, CancellationToken ct)
    {
        try
        {
            await EnsureCanViewStudentMedicalAsync(studentId, ct);
            return true;
        }
        catch (ForbiddenException)
        {
            return false;
        }
    }
}
