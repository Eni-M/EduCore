using EduCore.Dtos.Dashboard;
using EduCore.Dtos.Mark;
using EduCore.Dtos.Pagination;
using EduCore.Dtos.Schedule;
using EduCore.Dtos.Statistics;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Helpers;
using EduCore.Security;
using EduCore.Services.Interfaces;
using EduCore.Services.Statistics.Interfaces;

namespace EduCore.Services;

// Builds the signed-in user's dashboard.
//
// Composed entirely from existing services rather than new queries. That is a deliberate
// constraint: every figure here is one a screen elsewhere already shows, so a dashboard and the
// page it links to can never disagree — and the aggregates keep going through the statistics
// layer, which is where privacy suppression and grading rules live.
//
// It is also one HTTP round trip. The directorate dashboard previously made six parallel calls
// from the browser, two of them requesting a single row purely to read a total off the envelope.
public class DashboardService(
    ICurrentUser currentUser,
    IStudentService students,
    IClassService classes,
    IScheduleService schedule,
    IGuardianService guardians,
    IStatisticsOverviewService overview,
    IGradingCalculator grading) : IDashboardService
{
    // Enough to see the shape of recent activity without turning the dashboard into a list page.
    private const int RecentLimit = 5;

    // A guardian with more children than this gets the first few; the rest are one click away in
    // the portal. The per-child lookups are sequential, so this bounds the work a single request
    // can cause.
    private const int MaxChildrenDetailed = 4;

    public Task<DashboardDto> GetAsync(CancellationToken ct) => currentUser.Role switch
    {
        UserRole.Student => StudentAsync(ct),
        UserRole.Teacher => TeacherAsync(ct),
        UserRole.Guardian => GuardianAsync(ct),
        UserRole.Directorate => DirectorateAsync(ct),
        UserRole.RegionalAdmin => RegionAsync(UserRole.RegionalAdmin, "Your region", ct),
        UserRole.StateAdmin => RegionAsync(UserRole.StateAdmin, "Nationwide", ct),
        _ => throw new ForbiddenException("This account has no dashboard.")
    };

    // ---- Student -------------------------------------------------------------------

    private async Task<DashboardDto> StudentAsync(CancellationToken ct)
    {
        var profile = await students.GetProfileAsync(currentUser.RequireStudentId(), ct);

        var today = profile.CurrentClassId is { } classId
            ? await TodayForClassAsync(classId, includeTeacher: true, ct)
            : [];

        var marks = profile.Marks.Where(m => m.FinalMark is not null).ToList();
        var attendance = profile.Attendances.ToList();
        var present = attendance.Count(a => a.IsPresent);

        var stats = new List<DashboardStatDto>
        {
            new("Class", profile.CurrentClassName ?? "Not enrolled", null,
                profile.CurrentClassName is null ? DashboardTone.Warning : DashboardTone.Neutral),
            new("Lessons today", today.Count.ToString(), DayLabel(), DashboardTone.Neutral),
            new("Average mark",
                DashboardFormat.Average(marks.Select(m => m.FinalMark!.Value).ToList()),
                marks.Count == 0 ? "no marks yet" : $"across {marks.Count} marks",
                AverageTone(marks)),
            new("Attendance",
                DashboardFormat.AttendancePercent(present, attendance.Count),
                attendance.Count == 0 ? "nothing recorded" : $"{attendance.Count} lessons recorded",
                DashboardFormat.AttendanceTone(present, attendance.Count))
        };

        var failing = marks
            .Where(m => !grading.IsPass(m.FinalMark!.Value))
            .Select(m => m.SubjectName)
            .Distinct()
            .ToList();

        var attention = new List<DashboardAlertDto>();

        if (failing.Count > 0)
            attention.Add(new DashboardAlertDto(
                DashboardTone.Critical,
                failing.Count == 1 ? "One subject below the pass mark" : $"{failing.Count} subjects below the pass mark",
                string.Join(", ", failing),
                "/my-marks"));

        if (attendance.Count > 0 && present * 100.0 / attendance.Count < 90)
            attention.Add(new DashboardAlertDto(
                DashboardTone.Warning,
                "Attendance below 90%",
                $"You were absent from {attendance.Count - present} of {attendance.Count} recorded lessons.",
                "/my-attendance"));

        return new DashboardDto(
            nameof(UserRole.Student),
            profile.CurrentClassName,
            stats,
            attention,
            today,
            RecentMarks(profile.Marks, "/my-marks", withStudentName: false));
    }

    // ---- Teacher -------------------------------------------------------------------

    private async Task<DashboardDto> TeacherAsync(CancellationToken ct)
    {
        var teacherId = currentUser.RequireTeacherId();

        var mine = (await classes.GetMineAsync(ct)).ToList();

        var timetable = await schedule.GetTeacherTimetableAsync(teacherId, ct);
        var today = TodayFrom(timetable, includeTeacher: false);

        var homeroom = mine.Where(c => c.HomeroomTeacherId == teacherId).ToList();

        var stats = new List<DashboardStatDto>
        {
            new("Lessons today", today.Count.ToString(), DayLabel(), DashboardTone.Neutral),
            new("My classes", mine.Count.ToString(), null, DashboardTone.Neutral),
            new("Homeroom", homeroom.Count == 0 ? "—" : string.Join(", ", homeroom.Select(c => c.Name)),
                homeroom.Count == 0 ? "not a homeroom teacher" : null, DashboardTone.Neutral)
        };

        var attention = new List<DashboardAlertDto>();

        if (mine.Count == 0)
            attention.Add(new DashboardAlertDto(
                DashboardTone.Warning,
                "No classes assigned",
                "You have no teaching assignments yet. The school office assigns these.",
                null));

        var recent = today
            .Select(l => new DashboardEntryDto(
                $"{l.SubjectName} · {l.ClassName}",
                $"Period {l.Period}, {l.Start:HH\\:mm}–{l.End:HH\\:mm}",
                "/lessons"))
            .Take(RecentLimit)
            .ToList();

        return new DashboardDto(
            nameof(UserRole.Teacher),
            homeroom.Count > 0 ? $"Homeroom {homeroom[0].Name}" : null,
            stats,
            attention,
            today,
            recent);
    }

    // ---- Guardian ------------------------------------------------------------------

    private async Task<DashboardDto> GuardianAsync(CancellationToken ct)
    {
        var children = (await guardians.GetMyChildrenAsync(ct)).ToList();

        var stats = new List<DashboardStatDto>
        {
            new("Children", children.Count.ToString(), null,
                children.Count == 0 ? DashboardTone.Warning : DashboardTone.Neutral)
        };

        var attention = new List<DashboardAlertDto>();
        var recent = new List<DashboardEntryDto>();
        var today = new List<DashboardLessonDto>();

        if (children.Count == 0)
            return new DashboardDto(
                nameof(UserRole.Guardian), null, stats,
                [new DashboardAlertDto(
                    DashboardTone.Warning,
                    "No children linked to your account",
                    "The school office links a guardian to their children.", null)],
                today, recent);

        // Sequential and bounded rather than parallel: these share nothing, but a guardian with
        // many children should not be able to fan out an unbounded number of queries per request.
        foreach (var child in children.Take(MaxChildrenDetailed))
        {
            var profile = await students.GetProfileAsync(child.StudentId, ct);
            var name = child.FirstName;

            var marks = profile.Marks.Where(m => m.FinalMark is not null).ToList();
            var attendance = profile.Attendances.ToList();
            var present = attendance.Count(a => a.IsPresent);

            stats.Add(new DashboardStatDto(
                $"{name} · average",
                DashboardFormat.Average(marks.Select(m => m.FinalMark!.Value).ToList()),
                profile.CurrentClassName ?? "not enrolled",
                AverageTone(marks)));

            if (attendance.Count > 0)
                stats.Add(new DashboardStatDto(
                    $"{name} · attendance",
                    DashboardFormat.AttendancePercent(present, attendance.Count),
                    $"{attendance.Count} lessons recorded",
                    DashboardFormat.AttendanceTone(present, attendance.Count)));

            var failing = marks
                .Where(m => !grading.IsPass(m.FinalMark!.Value))
                .Select(m => m.SubjectName)
                .Distinct()
                .ToList();

            if (failing.Count > 0)
                attention.Add(new DashboardAlertDto(
                    DashboardTone.Critical,
                    $"{name} is below the pass mark in {(failing.Count == 1 ? "one subject" : $"{failing.Count} subjects")}",
                    string.Join(", ", failing),
                    "/guardian"));

            if (attendance.Count > 0 && present * 100.0 / attendance.Count < 90)
                attention.Add(new DashboardAlertDto(
                    DashboardTone.Warning,
                    $"{name}'s attendance is below 90%",
                    $"Absent from {attendance.Count - present} of {attendance.Count} recorded lessons.",
                    "/guardian"));

            recent.AddRange(RecentMarks(profile.Marks, "/guardian", withStudentName: true));

            if (profile.CurrentClassId is { } classId && today.Count == 0 && children.Count == 1)
                today = await TodayForClassAsync(classId, includeTeacher: true, ct);
        }

        return new DashboardDto(
            nameof(UserRole.Guardian),
            children.Count == 1 ? children[0].SchoolName : $"{children.Count} children",
            stats,
            attention,
            today,
            recent.Take(RecentLimit).ToList());
    }

    // ---- Directorate ---------------------------------------------------------------

    private async Task<DashboardDto> DirectorateAsync(CancellationToken ct)
    {
        // Page size one: the summary figures are computed over the whole school regardless of
        // paging (see SchoolOverviewDto), and the class list is not what this screen shows.
        var summary = await overview.GetSchoolOverviewAsync(
            new StatisticsFilter(), new PaginationQueryDto(1, 1), ct);

        var allClasses = (await classes.GetAllAsync(null, ct)).ToList();
        var withoutHomeroom = allClasses.Count(c => c.HomeroomTeacherId is null);

        var stats = new List<DashboardStatDto>
        {
            new("Active students", summary.Students.Active.ToString(), null, DashboardTone.Neutral),
            new("Teachers", summary.Teachers.Teachers.ToString(), null, DashboardTone.Neutral),
            new("Classes", summary.ClassesTotalCount.ToString(), null, DashboardTone.Neutral),
            new("Attendance", DashboardFormat.Percent(summary.Attendance.AttendanceRate), "this period",
                DashboardFormat.RateTone(summary.Attendance.AttendanceRate, 95, 90)),
            new("Pass rate", DashboardFormat.Percent(summary.Academic.PassRate), "this period",
                DashboardFormat.RateTone(summary.Academic.PassRate, 90, 75)),
            new("At risk", DashboardFormat.Percent(summary.Academic.AtRiskRate), "of graded students",
                DashboardFormat.InverseRateTone(summary.Academic.AtRiskRate, 5, 15))
        };

        var attention = new List<DashboardAlertDto>();

        if (withoutHomeroom > 0)
            attention.Add(new DashboardAlertDto(
                DashboardTone.Warning,
                withoutHomeroom == 1 ? "One class has no homeroom teacher" : $"{withoutHomeroom} classes have no homeroom teacher",
                "A class without a homeroom teacher has nobody responsible for its register.",
                "/classes"));

        if (summary.Academic.IncompleteMarks > 0)
            attention.Add(new DashboardAlertDto(
                DashboardTone.Warning,
                $"{summary.Academic.IncompleteMarks} marks are incomplete",
                "A mark with only one of its two components does not count toward a final grade.",
                "/classes"));

        if (allClasses.Count == 0)
            attention.Add(new DashboardAlertDto(
                DashboardTone.Critical,
                "No classes exist yet",
                "Classes belong to an academic year — create the year first, then the classes.",
                "/academic-years"));

        return new DashboardDto(
            nameof(UserRole.Directorate),
            "Your school",
            stats,
            attention,
            [],
            []);
    }

    // ---- Regional and state admin ---------------------------------------------------

    // Both read the same platform overview; the scope resolver narrows it to the caller's city or
    // state from their claims, so the only difference here is what the screen is called.
    private async Task<DashboardDto> RegionAsync(UserRole role, string context, CancellationToken ct)
    {
        var summary = await overview.GetPlatformOverviewAsync(
            new StatisticsFilter(), new PaginationQueryDto(1, RecentLimit), ct);

        var stats = new List<DashboardStatDto>
        {
            new("Schools", summary.Schools.ToString(), null, DashboardTone.Neutral),
            new("Active students", summary.Students.Active.ToString(), null, DashboardTone.Neutral),
            new("Teachers", summary.Teachers.Teachers.ToString(), null, DashboardTone.Neutral),
            new("Attendance", DashboardFormat.Percent(summary.Attendance.AttendanceRate), "across all schools",
                DashboardFormat.RateTone(summary.Attendance.AttendanceRate, 95, 90)),
            new("Pass rate", DashboardFormat.Percent(summary.Academic.PassRate), "across all schools",
                DashboardFormat.RateTone(summary.Academic.PassRate, 90, 75)),
            new("New arrivals", summary.Students.NewArrivals.ToString(), "this period", DashboardTone.Neutral)
        };

        var attention = new List<DashboardAlertDto>();

        // Named rather than counted: an administrator can act on "this school", not on "three schools".
        var lagging = summary.BySchool
            .Where(s => s.AttendanceRate is { Suppressed: false, Value: not null } m && m.Value < 85)
            .Select(s => s.SchoolName)
            .ToList();

        if (lagging.Count > 0)
            attention.Add(new DashboardAlertDto(
                DashboardTone.Warning,
                lagging.Count == 1 ? "One school is below 85% attendance" : $"{lagging.Count} schools are below 85% attendance",
                string.Join(", ", lagging),
                "/statistics"));

        if (summary.Schools == 0)
            attention.Add(new DashboardAlertDto(
                DashboardTone.Critical, "No schools in scope",
                "Nothing has been assigned to this account yet.", "/schools"));

        var recent = summary.BySchool
            .Select(s => new DashboardEntryDto(
                s.SchoolName,
                $"{s.Students} students · {s.Teachers} teachers · attendance {DashboardFormat.Percent(s.AttendanceRate)}",
                "/statistics"))
            .ToList();

        return new DashboardDto(role.ToString(), context, stats, attention, [], recent);
    }

    // ---- shared helpers --------------------------------------------------------------

    private async Task<List<DashboardLessonDto>> TodayForClassAsync(
        Guid classId, bool includeTeacher, CancellationToken ct) =>
        TodayFrom(await schedule.GetClassTimetableAsync(classId, ct), includeTeacher);

    private static List<DashboardLessonDto> TodayFrom(
        IEnumerable<ScheduleEntryDto> timetable, bool includeTeacher) =>
        timetable
            .Where(e => e.DayOfWeek == DateTime.UtcNow.DayOfWeek)
            .OrderBy(e => e.PeriodNumber)
            .Select(e => new DashboardLessonDto(
                e.PeriodNumber, e.StartTime, e.EndTime, e.SubjectName, e.ClassName,
                includeTeacher ? e.TeacherName : null))
            .ToList();

    private static List<DashboardEntryDto> RecentMarks(
        IEnumerable<MarkListDto> marks, string link, bool withStudentName) =>
        marks
            .Where(m => m.FinalMark is not null)
            .Reverse()
            .Take(RecentLimit)
            .Select(m => new DashboardEntryDto(
                withStudentName ? $"{m.StudentName} · {m.SubjectName}" : m.SubjectName,
                $"{m.FinalMark} · {m.GradingPeriodName}",
                link))
            .ToList();

    private static string DayLabel() => DateTime.UtcNow.DayOfWeek switch
    {
        DayOfWeek.Saturday or DayOfWeek.Sunday => "no lessons at the weekend",
        var day => day.ToString()
    };

    private DashboardTone AverageTone(IReadOnlyCollection<MarkListDto> marks) =>
        marks.Count == 0
            ? DashboardTone.Neutral
            : marks.All(m => grading.IsPass(m.FinalMark!.Value))
                ? DashboardTone.Good
                : DashboardTone.Warning;

}
