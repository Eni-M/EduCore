using EduCore.Dtos.Mark;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class MarkService(
    IMarkRepository marks,
    IEnrollmentRepository enrollments,
    IClassRepository classes,
    IGradingPeriodRepository gradingPeriods,
    IClassSubjectTeacherRepository assignments,
    IAccessGuard access,
    ICurrentUser currentUser) : IMarkService
{
    public async Task<IEnumerable<MarkListDto>> GetByStudentAsync(
        Guid studentId,
        Guid? subjectId,
        Guid? gradingPeriodId,
        CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);
        return await marks.GetByStudentAsync(studentId, subjectId, gradingPeriodId, ct);
    }

    public async Task<IEnumerable<MarkListDto>> GetByClassAsync(
        Guid classId,
        Guid? subjectId,
        Guid? gradingPeriodId,
        CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);
        return await marks.GetByClassAsync(classId, subjectId, gradingPeriodId, ct);
    }

    public async Task<MarkListDto> UpsertAsync(MarkUpsertDto dto, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(dto.EnrollmentId, ct)
                         ?? throw new NotFoundException("Enrollment", dto.EnrollmentId);

        var @class = await classes.GetByIdAsync(enrollment.ClassId, ct)
                     ?? throw new NotFoundException("Class", enrollment.ClassId);

        var assignment = await assignments.GetActiveAsync(@class.Id, dto.SubjectId, ct)
                         ?? throw new BusinessException(
                             "No teacher is currently assigned to teach that subject to this class.");

        if (currentUser.IsTeacher && assignment.TeacherId != currentUser.TeacherId)
            throw new ForbiddenException("Only the teacher assigned to this class-subject can give marks for it.");

        var gradingPeriod = dto.GradingPeriodId is { } explicitPeriodId
            ? await gradingPeriods.GetByIdAsync(explicitPeriodId, ct)
              ?? throw new NotFoundException("Grading period", explicitPeriodId)
            : await gradingPeriods.GetContainingAsync(
                  @class.AcademicYearId, DateOnly.FromDateTime(DateTime.UtcNow), ct)
              ?? throw new BusinessException(
                  "Today does not fall inside any term of this class's academic year. "
                  + "Name the term explicitly, or add one covering today.");

        if (gradingPeriod.AcademicYearId != @class.AcademicYearId)
            throw new BusinessException("That grading period belongs to a different academic year than this class.");

        var mark = await marks.GetForUpsertAsync(
            enrollment.Id, dto.SubjectId, gradingPeriod.Id, ct);

        if (mark is null)
        {
            mark = new Mark
            {
                EnrollmentId = enrollment.Id,
                ClassId = @class.Id,
                AcademicYearId = @class.AcademicYearId,
                SchoolId = currentUser.RequireSchoolId(),
                ClassSubjectTeacherId = assignment.Id,
                SubjectId = assignment.SubjectId,
                TeacherId = assignment.TeacherId,
                GradingPeriodId = gradingPeriod.Id,
                FirstMark = dto.FirstMark,
                SecondMark = dto.SecondMark
            };

            await marks.AddAsync(mark);
        }
        else
        {
            mark.FirstMark = dto.FirstMark ?? mark.FirstMark;
            mark.SecondMark = dto.SecondMark ?? mark.SecondMark;
            marks.Update(mark);
        }

        await marks.SaveChangesAsync(ct);

        return await marks.GetDtoAsync(mark.Id, ct)
               ?? throw new NotFoundException("Mark", mark.Id);
    }
}
