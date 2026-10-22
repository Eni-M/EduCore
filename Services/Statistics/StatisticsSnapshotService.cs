using EduCore.Dtos.Statistics;
using EduCore.Models;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Statistics;

// Reads school statistics from SchoolStatisticsSnapshot, recomputing a row when
// enough has changed underneath it to be worth the work.
//
// Recomputation happens on the read that finds the row stale. That keeps the numbers correct
// by construction — there is no window in which a caller can be handed figures the system
// knows are out of date — at the cost of one slow request per threshold crossing.
public class StatisticsSnapshotService(
    IStatisticsSnapshotRepository snapshots,
    IStudentStatisticsRepository students,
    IEnrollmentStatisticsRepository enrollments,
    IAttendanceStatisticsRepository attendance,
    IAcademicStatisticsRepository academic,
    ITeacherStatisticsRepository teachers,
    IOptions<StatisticsSnapshotOptions> options,
    ILogger<StatisticsSnapshotService> logger) : IStatisticsSnapshotService
{
    private readonly StatisticsSnapshotOptions _options = options.Value;

    public bool IsSnapshotable(StatisticsQuery ctx) =>
        _options.Enabled
        && ctx.SchoolId is not null
        && !ctx.CrossSchool
        // Every one of these narrows the row set. Storing a snapshot per combination would
        // multiply out to more rows than the data being summarised.
        && ctx.ClassId is null
        && ctx.Grade is null
        && ctx.SubjectId is null
        && ctx.TeacherId is null
        && ctx.CountryId is null
        && ctx.OriginCityId is null
        && ctx.CityId is null
        && ctx.ZoneId is null
        && ctx.SchoolStateId is null;

    public async Task<SchoolStatisticsSnapshot?> GetAsync(StatisticsQuery ctx, CancellationToken ct)
    {
        if (!IsSnapshotable(ctx))
            return null;

        var key = KeyFor(ctx, ctx.SchoolId!.Value);
        var existing = await snapshots.GetAsync(key, ct);

        if (existing is not null && !IsStale(existing))
            return existing;

        return await RefreshAsync(ctx, key, existing, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, SchoolStatisticsSnapshot>> GetManyAsync(
        StatisticsQuery ctx, IReadOnlyCollection<Guid> schoolIds, CancellationToken ct)
    {
        var result = new Dictionary<Guid, SchoolStatisticsSnapshot>();

        if (!_options.Enabled || schoolIds.Count == 0)
            return result;

        // One statement for every school, instead of one request's worth of aggregates each.
        var existing = (await snapshots.GetManyAsync(
                schoolIds, ctx.From, ctx.To, ctx.AcademicYearId, ctx.GradingPeriodId, ct))
            .ToDictionary(s => s.SchoolId);

        var refreshesLeft = _options.MaximumRefreshesPerRequest;

        foreach (var schoolId in schoolIds)
        {
            existing.TryGetValue(schoolId, out var snapshot);

            // Fresh, or stale with this request's refresh budget spent: serve what is stored.
            if (snapshot is not null && (!IsStale(snapshot) || refreshesLeft == 0))
            {
                result[schoolId] = snapshot;
                continue;
            }

            if (snapshot is not null)
                refreshesLeft--;

            // Narrow the query to this school so the aggregates are computed for it alone.
            var scoped = ctx with { SchoolId = schoolId, CrossSchool = false };
            var refreshed = await RefreshAsync(scoped, KeyFor(scoped, schoolId), snapshot, ct);

            if (refreshed is not null)
                result[schoolId] = refreshed;
        }

        return result;
    }

    private static SnapshotKey KeyFor(StatisticsQuery ctx, Guid schoolId) =>
        new(schoolId, ctx.From, ctx.To, ctx.AcademicYearId, ctx.GradingPeriodId);

    private bool IsStale(SchoolStatisticsSnapshot snapshot) =>
        SnapshotStaleness.IsStale(snapshot, _options, DateTime.UtcNow);

    private async Task<SchoolStatisticsSnapshot?> RefreshAsync(
        StatisticsQuery ctx, SnapshotKey key, SchoolStatisticsSnapshot? existing, CancellationToken ct)
    {
        // The figures come from the same repository methods the live path calls, so a snapshot
        // and a freshly computed answer cannot disagree about anything but their age.
        var studentTotals = await students.GetTotalsAsync(ctx, ct);
        var attendanceTotals = await attendance.GetTotalsAsync(ctx, ct);
        var academicTotals = await academic.GetTotalsAsync(ctx, ct);
        var workload = await teachers.GetWorkloadAsync(ctx, ct);

        var snapshot = existing ?? new SchoolStatisticsSnapshot { SchoolId = key.SchoolId };

        snapshot.From = key.From;
        snapshot.To = key.To;
        snapshot.AcademicYearId = key.AcademicYearId;
        snapshot.GradingPeriodId = key.GradingPeriodId;

        snapshot.StudentsTotal = studentTotals.Total;
        snapshot.StudentsActive = studentTotals.Active;
        snapshot.StudentsGraduated = studentTotals.Graduated;
        snapshot.StudentsNewArrivals = studentTotals.NewArrivals;
        snapshot.StudentsFemale = studentTotals.Female;
        snapshot.StudentsMale = studentTotals.Male;
        snapshot.StudentsGenderNotRecorded = studentTotals.GenderNotRecorded;

        snapshot.EnrollmentsActive = await enrollments.CountActiveAsync(ctx, ct);
        snapshot.EnrollmentsStarted = await enrollments.CountStartedAsync(ctx, ct);
        snapshot.EnrollmentsExited = await enrollments.CountExitedAsync(ctx, ct);
        snapshot.EnrollmentsActiveYearAgo =
            await enrollments.CountActiveOnAsync(ctx, ctx.To.AddYears(-1), ct);

        snapshot.AttendanceRecords = attendanceTotals.Records;
        snapshot.AttendancePresent = attendanceTotals.Present;
        snapshot.AttendanceAbsent = attendanceTotals.Absent;
        snapshot.AttendanceExcusedAbsences = attendanceTotals.ExcusedAbsences;
        snapshot.AttendanceAverageBehaviorMark = attendanceTotals.AverageBehaviorMark;
        snapshot.AttendanceBehaviorMarkCount = attendanceTotals.BehaviorMarkCount;

        snapshot.AcademicGradedMarks = academicTotals.GradedMarks;
        snapshot.AcademicIncompleteMarks = academicTotals.IncompleteMarks;
        snapshot.AcademicAverageFinalMark = academicTotals.AverageFinalMark;
        snapshot.AcademicPassed = academicTotals.Passed;
        snapshot.AcademicFailed = academicTotals.Failed;
        snapshot.AcademicAtRisk = academicTotals.AtRisk;

        snapshot.Teachers = await teachers.CountTeachersAsync(ctx, ct);
        snapshot.Classes = await snapshots.CountClassesAsync(ctx, ct);
        snapshot.TeachersWithWorkload = workload.Count;
        snapshot.WorkloadClassesSum = workload.Sum(w => w.Classes);

        snapshot.ComputedAt = DateTime.UtcNow;
        snapshot.ChangesSinceComputed = 0;

        await snapshots.SaveAsync(snapshot, ct);

        logger.LogDebug(
            "Recomputed statistics snapshot for school {SchoolId} over {From}..{To}.",
            key.SchoolId, key.From, key.To);

        return snapshot;
    }
}
