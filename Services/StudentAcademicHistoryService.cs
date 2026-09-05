using EduCore.Dtos.StudentHistory;
using EduCore.Exceptions;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

/// <summary>
/// Compiles and serves a student's academic history across schools.
///
/// The access model in two steps, and the order matters: the caller must be able to see this
/// student *today* (IAccessGuard, the same check the rest of the student surface uses), and only
/// then do they see every year on record, including years at schools they have no claim on. That
/// keeps the table's lack of school scoping from becoming a way to read arbitrary students --
/// without it, "not school-scoped" would mean any directorate could pull any child in the country.
/// </summary>
public class StudentAcademicHistoryService(
    IStudentAcademicRecordRepository records,
    IAccessGuard access,
    IGradingCalculator grading,
    ICurrentUser currentUser) : IStudentAcademicHistoryService
{
    public async Task<StudentAcademicHistoryDto> GetAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);

        var name = await records.GetStudentNameAsync(studentId, ct)
                   ?? throw new NotFoundException("Student", studentId);

        var stored = await records.GetForStudentAsync(studentId, ct);

        return new StudentAcademicHistoryDto(studentId, name, stored.Select(Project).ToList());
    }

    public async Task<int> RebuildAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);

        var enrollments = await records.GetEnrollmentYearsAsync(studentId, ct);
        var existing = await records.GetForStudentAsync(studentId, ct);

        foreach (var enrollment in enrollments)
        {
            var marks = await records.GetPeriodMarksAsync(enrollment.EnrollmentId, ct);

            // One annual mark per subject, from that subject's period finals. Rounding goes
            // through IGradingCalculator so a transcript and a report card can never disagree.
            var subjects = marks
                .GroupBy(m => new { m.SubjectId, m.SubjectName })
                .Select(g => new StudentAcademicRecordSubject
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.Key.SubjectName,
                    AnnualMark = grading.AnnualMark(
                        g.Select(m => grading.FinalMark(m.FirstMark, m.SecondMark)))
                })
                .OrderBy(s => s.SubjectName)
                .ToList();

            var record = existing.FirstOrDefault(r =>
                r.SchoolId == enrollment.SchoolId && r.AcademicYearId == enrollment.AcademicYearId);

            if (record is null)
            {
                record = new StudentAcademicRecord { StudentId = studentId };
                records.Add(record);
            }

            record.SchoolId = enrollment.SchoolId;
            record.SchoolName = enrollment.SchoolName;
            record.AcademicYearId = enrollment.AcademicYearId;
            record.AcademicYearName = enrollment.AcademicYearName;
            record.Grade = enrollment.Grade;
            record.ClassName = enrollment.ClassName;
            record.EnrolledAt = enrollment.EnrolledAt;
            record.LeftAt = enrollment.LeftAt;
            record.ExitReason = enrollment.ExitReason;
            record.CompiledAt = DateTime.UtcNow;

            // Replaced wholesale rather than merged: a subject dropped from the curriculum has to
            // disappear from the transcript, and matching line by line to achieve that is more
            // code for the same result. The cascade on the relationship deletes the old lines.
            record.Subjects.Clear();

            foreach (var subject in subjects)
                record.Subjects.Add(subject);
        }

        await records.SaveChangesAsync(ct);

        return enrollments.Count;
    }

    private StudentAcademicYearDto Project(StudentAcademicRecord record)
    {
        var subjects = record.Subjects
            .OrderBy(s => s.SubjectName)
            .Select(s => new StudentAcademicSubjectDto(
                s.SubjectName,
                s.AnnualMark,
                s.AnnualMark is null ? null : grading.IsPass(s.AnnualMark.Value)))
            .ToList();

        var graded = subjects.Where(s => s.AnnualMark is not null).ToList();

        return new StudentAcademicYearDto(
            record.SchoolId,
            record.SchoolName,
            currentUser.SchoolIds.Contains(record.SchoolId),
            record.AcademicYearName,
            record.Grade,
            record.ClassName,
            record.EnrolledAt,
            record.LeftAt,
            record.ExitReason,
            graded.Count == 0 ? null : Math.Round(graded.Average(s => s.AnnualMark!.Value), 2),
            subjects.Count,
            graded.Count,
            graded.Count(s => s.Passed == true),
            graded.Count(s => s.Passed == false),
            record.CompiledAt,
            subjects);
    }
}
