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

    public async Task<MarkListDto> CreateAsync(MarkCreateDto dto, CancellationToken ct)
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

        var gradingPeriod = await gradingPeriods.GetByIdAsync(dto.GradingPeriodId, ct)
                            ?? throw new NotFoundException("Grading period", dto.GradingPeriodId);

        if (gradingPeriod.AcademicYearId != @class.AcademicYearId)
            throw new BusinessException("That grading period belongs to a different academic year than this class.");

        if (dto.Date < gradingPeriod.StartDate || dto.Date > gradingPeriod.EndDate)
            throw new BusinessException(
                $"The mark date must fall inside the grading period ({gradingPeriod.StartDate} - {gradingPeriod.EndDate}).");

        var mark = new Mark
        {
            EnrollmentId = enrollment.Id,
            ClassId = @class.Id,
            AcademicYearId = @class.AcademicYearId,
            SchoolId = currentUser.RequireSchoolId(),
            ClassSubjectTeacherId = assignment.Id,
            SubjectId = assignment.SubjectId,
            TeacherId = assignment.TeacherId,
            GradingPeriodId = gradingPeriod.Id,
            Type = dto.Type,
            Value = dto.Value,
            Date = dto.Date
        };

        await marks.AddAsync(mark);
        await marks.SaveChangesAsync(ct);

        return await marks.GetDtoAsync(mark.Id, ct)
               ?? throw new NotFoundException("Mark", mark.Id);
    }
}
