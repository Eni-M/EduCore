using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services.Email;

// loads a few whole tables per run and groups in memory. Fine for a once-a-day job at
// school scale; push the filtering into SQL if a very large deployment makes the run slow.
public class ScheduleDigestService(AppDbContext db, IEmailSender email) : IScheduleDigestService
{
    public async Task SendDailyAsync(DateOnly today, CancellationToken ct)
    {
        var tomorrow = today.AddDays(1);
        var lookups = await LoadLookupsAsync(ct);

        await SendDayScheduleAsync(tomorrow, "Your schedule for tomorrow", lookups, ct);
        await SendMeetingRemindersAsync(tomorrow, lookups, ct);
    }

    public async Task SendWeeklyAsync(DateOnly today, CancellationToken ct)
    {
        var lookups = await LoadLookupsAsync(ct);

        // The seven days starting tomorrow.
        var teacherBody = new Dictionary<Guid, List<string>>();
        var classBody = new Dictionary<Guid, List<string>>();

        for (var offset = 1; offset <= 7; offset++)
        {
            var date = today.AddDays(offset);
            var entries = await EntriesForDayAsync(date.DayOfWeek, ct);
            if (entries.Count == 0) continue;

            var header = $"{date:dddd d MMM}";

            foreach (var group in entries.GroupBy(e => e.TeacherId))
            {
                var lines = group
                    .OrderBy(e => PeriodNumber(e, lookups))
                    .Select(e => "  " + TeacherLine(e, lookups));
                Append(teacherBody, group.Key, header, lines);
            }

            foreach (var group in entries.GroupBy(e => e.ClassId))
            {
                var lines = group
                    .OrderBy(e => PeriodNumber(e, lookups))
                    .Select(e => "  " + StudentLine(e, lookups));
                Append(classBody, group.Key, header, lines);
            }
        }

        foreach (var (teacherId, lines) in teacherBody)
            await SendToTeacherAsync(teacherId, "Your schedule for the week", lines, lookups, ct);

        foreach (var (classId, lines) in classBody)
            await SendToClassAsync(classId, "Your schedule for the week", lines, lookups, ct);
    }

    // ---- per-day schedule --------------------------------------------------

    private async Task SendDayScheduleAsync(
        DateOnly date, string subject, Lookups lookups, CancellationToken ct)
    {
        var entries = await EntriesForDayAsync(date.DayOfWeek, ct);
        if (entries.Count == 0) return;

        foreach (var group in entries.GroupBy(e => e.TeacherId))
        {
            var lines = group.OrderBy(e => PeriodNumber(e, lookups)).Select(e => TeacherLine(e, lookups));
            await SendToTeacherAsync(group.Key, subject, lines, lookups, ct);
        }

        foreach (var group in entries.GroupBy(e => e.ClassId))
        {
            var lines = group.OrderBy(e => PeriodNumber(e, lookups)).Select(e => StudentLine(e, lookups));
            await SendToClassAsync(group.Key, subject, lines, lookups, ct);
        }
    }

    // ---- meetings ----------------------------------------------------------

    private async Task SendMeetingRemindersAsync(DateOnly date, Lookups lookups, CancellationToken ct)
    {
        var meetings = await db.CalendarEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.Type == CalendarEventType.Meeting && e.Date == date)
            .ToListAsync(ct);

        if (meetings.Count == 0) return;

        foreach (var bySchool in meetings.GroupBy(m => m.SchoolId))
        {
            var lines = bySchool
                .OrderBy(m => m.StartTime ?? TimeOnly.MinValue)
                .Select(m => MeetingLine(m));

            var body = "Meetings tomorrow:\n\n" + string.Join("\n", lines);

            // Directorate staff of the school oversee meetings.
            foreach (var userId in DirectorateUserIds(bySchool.Key, lookups))
                await SendToUserAsync(userId, "Meetings tomorrow", body, lookups, ct);
        }
    }

    // ---- sending helpers ---------------------------------------------------

    private async Task SendToTeacherAsync(
        Guid teacherId, string subject, IEnumerable<string> lines, Lookups lookups, CancellationToken ct)
    {
        if (lookups.TeacherUserId.TryGetValue(teacherId, out var userId))
            await SendToUserAsync(userId, subject, Body(lines), lookups, ct);
    }

    private async Task SendToClassAsync(
        Guid classId, string subject, IEnumerable<string> lines, Lookups lookups, CancellationToken ct)
    {
        var body = Body(lines);

        if (!lookups.StudentUserIdsByClass.TryGetValue(classId, out var userIds)) return;

        foreach (var userId in userIds)
            await SendToUserAsync(userId, subject, body, lookups, ct);
    }

    private async Task SendToUserAsync(
        Guid userId, string subject, string body, Lookups lookups, CancellationToken ct)
    {
        if (lookups.Users.TryGetValue(userId, out var user) && !string.IsNullOrWhiteSpace(user.Email))
            await email.SendAsync(user.Email, subject, $"Hello {user.FirstName},\n\n{body}", ct);
    }

    private IEnumerable<Guid> DirectorateUserIds(Guid schoolId, Lookups lookups) =>
        lookups.DirectorateUserIdsBySchool.TryGetValue(schoolId, out var ids) ? ids : [];

    // ---- formatting --------------------------------------------------------

    private static string Body(IEnumerable<string> lines)
    {
        var list = lines.ToList();
        return list.Count == 0 ? "Nothing scheduled." : string.Join("\n", list);
    }

    private static void Append(Dictionary<Guid, List<string>> map, Guid key, string header, IEnumerable<string> lines)
    {
        if (!map.TryGetValue(key, out var list))
            map[key] = list = [];
        list.Add(header);
        list.AddRange(lines);
        list.Add("");
    }

    private int PeriodNumber(ScheduleEntry e, Lookups lookups) =>
        lookups.Periods.TryGetValue(e.ShiftPeriodId, out var p) ? p.PeriodNumber : 0;

    private string TeacherLine(ScheduleEntry e, Lookups lookups)
    {
        var period = PeriodText(e, lookups);
        var subject = lookups.SubjectNames.GetValueOrDefault(e.SubjectId, "Subject");
        var className = lookups.ClassNames.GetValueOrDefault(e.ClassId, "Class");
        return $"{period}: {subject} — {className}";
    }

    private string StudentLine(ScheduleEntry e, Lookups lookups)
    {
        var period = PeriodText(e, lookups);
        var subject = lookups.SubjectNames.GetValueOrDefault(e.SubjectId, "Subject");
        return $"{period}: {subject}";
    }

    private string PeriodText(ScheduleEntry e, Lookups lookups) =>
        lookups.Periods.TryGetValue(e.ShiftPeriodId, out var p)
            ? $"Period {p.PeriodNumber} ({p.StartTime:HH\\:mm}-{p.EndTime:HH\\:mm})"
            : "Lesson";

    private static string MeetingLine(CalendarEvent m)
    {
        var time = m.StartTime is { } start ? $" at {start:HH\\:mm}" : "";
        return $"- {m.Title}{time}";
    }

    // ---- lookups -----------------------------------------------------------

    private async Task<List<ScheduleEntry>> EntriesForDayAsync(DayOfWeek day, CancellationToken ct) =>
        await db.ScheduleEntries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.DayOfWeek == day)
            .ToListAsync(ct);

    private async Task<Lookups> LoadLookupsAsync(CancellationToken ct)
    {
        var users = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.DeactivatedAt == null)
            .ToDictionaryAsync(u => u.Id, ct);

        var teacherUserId = await db.Teachers.IgnoreQueryFilters().AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.UserId, ct);

        var directorates = await db.Directorates.IgnoreQueryFilters().AsNoTracking()
            .ToListAsync(ct);
        var directorateBySchool = directorates
            .GroupBy(d => d.SchoolId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(d => d.UserId).ToList());

        var students = await db.Students.IgnoreQueryFilters().AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.UserId, ct);

        var studentUserIdsByClass = new Dictionary<Guid, List<Guid>>();
        var activeEnrollments = await db.Enrollments.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.LeftAt == null)
            .ToListAsync(ct);
        foreach (var e in activeEnrollments)
        {
            if (!students.TryGetValue(e.StudentId, out var userId)) continue;
            if (!studentUserIdsByClass.TryGetValue(e.ClassId, out var list))
                studentUserIdsByClass[e.ClassId] = list = [];
            list.Add(userId);
        }

        var subjectNames = await db.Subjects.IgnoreQueryFilters().AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var classNames = await db.Classes.IgnoreQueryFilters().AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var periods = await db.ShiftPeriods.IgnoreQueryFilters().AsNoTracking()
            .ToDictionaryAsync(p => p.Id, ct);

        return new Lookups(
            users, teacherUserId, directorateBySchool,
            studentUserIdsByClass.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<Guid>)kv.Value),
            subjectNames, classNames, periods);
    }

    private sealed record Lookups(
        IReadOnlyDictionary<Guid, User> Users,
        IReadOnlyDictionary<Guid, Guid> TeacherUserId,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> DirectorateUserIdsBySchool,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> StudentUserIdsByClass,
        IReadOnlyDictionary<Guid, string> SubjectNames,
        IReadOnlyDictionary<Guid, string> ClassNames,
        IReadOnlyDictionary<Guid, ShiftPeriod> Periods);
}
