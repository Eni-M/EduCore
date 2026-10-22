using EduCore.Dtos.Mark;
using EduCore.Exceptions;
using EduCore.Helpers;
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
    IAcademicYearRepository academicYears,
    IClassSubjectTeacherRepository assignments,
    IAccessGuard access,
    ICurrentUser currentUser,
    IGradingCalculator grading) : IMarkService
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

    public async Task<IReadOnlyList<MarkChangeDto>> GetChangesAsync(Guid markId, CancellationToken ct)
    {
        // The school filter already keeps this to the caller's school; the class check keeps a
        // teacher to classes they can see.
        var mark = await marks.GetByIdAsync(markId, ct) ?? throw new NotFoundException("Mark", markId);
        await access.EnsureCanViewClassAsync(mark.ClassId, ct);

        return await marks.GetChangesAsync(markId, ct);
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

        access.EnsureCanWriteMarks(assignment.TeacherId);

        var gradingPeriod = dto.GradingPeriodId is { } explicitPeriodId
            ? await gradingPeriods.GetByIdAsync(explicitPeriodId, ct)
              ?? throw new NotFoundException("Grading period", explicitPeriodId)
            : await gradingPeriods.GetContainingAsync(
                  @class.AcademicYearId, DateOnly.FromDateTime(DateTime.UtcNow), ct)
              ?? throw new BusinessException(await NoTermCoversTodayAsync(@class.AcademicYearId, ct));

        if (gradingPeriod.AcademicYearId != @class.AcademicYearId)
            throw new BusinessException("That grading period belongs to a different academic year than this class.");

        // A closed term's marks are the record. Teachers cannot touch them; the office can correct
        // one, but must say why -- the reason is kept with the change.
        if (gradingPeriod.IsClosed)
        {
            if (!currentUser.IsDirectorate)
                throw new ForbiddenException(
                    "This term is closed. Ask the school office to correct the mark.");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new BusinessException("Give a reason for changing a mark in a closed term.");
        }

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
                SecondMark = dto.SecondMark,
                // Weighted now, under today's weights, and never again -- see Mark.WeightedValue.
                WeightedValue = grading.WeightedValue(dto.FirstMark, dto.SecondMark),
                RecordedAt = DateTime.UtcNow
            };

            await marks.AddAsync(mark);
        }
        else
        {
            if (dto.Version is { } version)
                marks.ExpectVersion(mark, version);

            var (oldFirst, oldSecond) = (mark.FirstMark, mark.SecondMark);

            mark.FirstMark = dto.FirstMark ?? mark.FirstMark;
            mark.SecondMark = dto.SecondMark ?? mark.SecondMark;

            // Only the components that moved are reweighted; an untouched mark keeps the value it
            // was recorded with, even if the weights have changed since.
            if (mark.FirstMark != oldFirst || mark.SecondMark != oldSecond)
            {
                mark.WeightedValue = grading.WeightedValue(mark.FirstMark, mark.SecondMark);
                mark.RecordedAt = DateTime.UtcNow;

                marks.AddChange(new MarkChange
                {
                    MarkId = mark.Id,
                    SchoolId = mark.SchoolId,
                    ChangedByUserId = currentUser.UserId,
                    ChangedAt = DateTime.UtcNow,
                    OldFirstMark = oldFirst,
                    OldSecondMark = oldSecond,
                    NewFirstMark = mark.FirstMark,
                    NewSecondMark = mark.SecondMark,
                    Reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),
                    AfterClose = gradingPeriod.IsClosed,
                });
            }

            marks.Update(mark);
        }

        await marks.SaveChangesAsync(ct);

        return await marks.GetDtoAsync(mark.Id, ct)
               ?? throw new NotFoundException("Mark", mark.Id);
    }

    // The old wording said only that today was outside every term, which left a teacher with
    // nothing to act on. Naming the year and its range makes the usual cause -- a year whose
    // dates do not match its name -- visible from the message itself.
    private async Task<string> NoTermCoversTodayAsync(Guid academicYearId, CancellationToken ct)
    {
        var year = await academicYears.GetByIdAcrossSchoolsAsync(academicYearId, ct);

        var which = year is null
            ? "this class's academic year"
            : $"academic year {year.Name} ({year.StartDate:yyyy-MM-dd} to {year.EndDate:yyyy-MM-dd})";

        return $"Today is not inside any term of {which}. "
               + "Name the term explicitly, or correct the year's dates.";
    }
}
