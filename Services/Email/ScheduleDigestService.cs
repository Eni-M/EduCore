using EduCore.Data;
using EduCore.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services.Email;

// Builds the scheduled digests with targeted EF projections: only the rows and columns a digest
// needs leave PostgreSQL, joins run in SQL, and grouping/formatting happens on small records.
//
// IgnoreQueryFilters() is deliberate and required: this runs from a background scope with no
// signed-in user, so the per-request school filter would otherwise match nothing. It is safe
// because every email is grouped by its owning teacher/class/school -- a recipient only ever sees
// their own school's data. Deactivated users are excluded explicitly instead.
public class ScheduleDigestService(AppDbContext db, IEmailSender email) : IScheduleDigestService
{
    // Bounded so a real SMTP/provider transport is never hit with unlimited parallel sends.
    private const int MaxConcurrentEmails = 5;

    private sealed record LessonRow(
        Guid SchoolId, DayOfWeek Day, int PeriodNumber, TimeOnly Start, TimeOnly End,
        string Subject, Guid ClassId, string ClassName,
        Guid TeacherId, string TeacherEmail, string TeacherFirstName);

    private sealed record Recipient(Guid ClassId, string Email, string FirstName);
    private sealed record MeetingRow(Guid SchoolId, string Title, TimeOnly? Start);
    private sealed record Contact(Guid SchoolId, string Email, string FirstName);
    private sealed record Mail(string Email, string Subject, string Body);

    public async Task SendDailyAsync(DateOnly today, CancellationToken ct)
    {
        var tomorrow = today.AddDays(1);
        var holidays = await HolidaysAsync([tomorrow], ct);

        var lessons = (await LessonsForDaysAsync([tomorrow.DayOfWeek], ct))
            .Where(l => !holidays.Contains((l.SchoolId, tomorrow)))
            .ToList();

        var mails = new List<Mail>();
        const string subject = "Your schedule for tomorrow";

        foreach (var teacher in lessons.GroupBy(l => new { l.TeacherId, l.TeacherEmail, l.TeacherFirstName }))
        {
            var body = Format(teacher.OrderBy(l => l.PeriodNumber).Select(TeacherLine));
            mails.Add(new Mail(teacher.Key.TeacherEmail, subject, Greeting(teacher.Key.TeacherFirstName, body)));
        }

        var byClass = lessons
            .GroupBy(l => l.ClassId)
            .ToDictionary(g => g.Key, g => Format(g.OrderBy(l => l.PeriodNumber).Select(StudentLine)));

        foreach (var student in await StudentsForClassesAsync(byClass.Keys, ct))
            if (byClass.TryGetValue(student.ClassId, out var body))
                mails.Add(new Mail(student.Email, subject, Greeting(student.FirstName, body)));

        mails.AddRange(await MeetingMailsAsync(tomorrow, ct));

        await SendAllAsync(mails, ct);
    }

    public async Task SendWeeklyAsync(DateOnly today, CancellationToken ct)
    {
        var dates = Enumerable.Range(1, 7).Select(today.AddDays).ToList();
        var days = dates.Select(d => d.DayOfWeek).Distinct().ToList();
        var holidays = await HolidaysAsync(dates, ct);

        // One query for the whole week, grouped by day in memory afterwards.
        var lessons = await LessonsForDaysAsync(days, ct);
        var students = (await StudentsForClassesAsync(lessons.Select(l => l.ClassId).Distinct().ToList(), ct))
            .GroupBy(s => s.ClassId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var mails = new List<Mail>();
        const string subject = "Your schedule for the week";

        foreach (var teacher in lessons.GroupBy(l => new { l.TeacherId, l.TeacherEmail, l.TeacherFirstName }))
        {
            var body = WeekBody(dates, holidays, teacher, TeacherLine);
            if (body is not null)
                mails.Add(new Mail(teacher.Key.TeacherEmail, subject, Greeting(teacher.Key.TeacherFirstName, body)));
        }

        foreach (var classGroup in lessons.GroupBy(l => l.ClassId))
        {
            var body = WeekBody(dates, holidays, classGroup, StudentLine);
            if (body is null || !students.TryGetValue(classGroup.Key, out var recipients)) continue;

            foreach (var student in recipients)
                mails.Add(new Mail(student.Email, subject, Greeting(student.FirstName, body)));
        }

        await SendAllAsync(mails, ct);
    }

    // ---- queries (projected, filtered, joined in SQL) ----------------------

    private async Task<List<LessonRow>> LessonsForDaysAsync(
        IReadOnlyCollection<DayOfWeek> days, CancellationToken ct)
    {
        var query =
            from e in db.ScheduleEntries.AsNoTracking()
            where days.Contains(e.DayOfWeek)
            join c in db.Classes on e.ClassId equals c.Id
            join s in db.Subjects on e.SubjectId equals s.Id
            join p in db.ShiftPeriods on e.ShiftPeriodId equals p.Id
            join t in db.Teachers on e.TeacherId equals t.Id
            join u in db.Users on t.UserId equals u.Id
            where u.DeactivatedAt == null
            select new LessonRow(
                e.SchoolId, e.DayOfWeek, p.PeriodNumber, p.StartTime, p.EndTime,
                s.Name, e.ClassId, c.Name, e.TeacherId, u.Email, u.FirstName);

        return await query.IgnoreQueryFilters().ToListAsync(ct);
    }

    private async Task<List<Recipient>> StudentsForClassesAsync(
        IReadOnlyCollection<Guid> classIds, CancellationToken ct)
    {
        if (classIds.Count == 0) return [];

        var query =
            from en in db.Enrollments.AsNoTracking()
            where en.LeftAt == null && classIds.Contains(en.ClassId)
            join st in db.Students on en.StudentId equals st.Id
            join u in db.Users on st.UserId equals u.Id
            where u.DeactivatedAt == null
            select new Recipient(en.ClassId, u.Email, u.FirstName);

        return await query.IgnoreQueryFilters().ToListAsync(ct);
    }

    private async Task<List<Mail>> MeetingMailsAsync(DateOnly date, CancellationToken ct)
    {
        var meetings = await db.CalendarEvents.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Type == CalendarEventType.Meeting && e.Date == date)
            .Select(e => new MeetingRow(e.SchoolId, e.Title, e.StartTime))
            .ToListAsync(ct);

        if (meetings.Count == 0) return [];

        var bodyBySchool = meetings
            .GroupBy(m => m.SchoolId)
            .ToDictionary(
                g => g.Key,
                g => "Meetings tomorrow:\n\n" +
                     string.Join("\n", g.OrderBy(m => m.Start ?? TimeOnly.MinValue).Select(MeetingLine)));

        var directorates = await DirectoratesForSchoolsAsync(bodyBySchool.Keys, ct);

        return directorates
            .Where(d => bodyBySchool.ContainsKey(d.SchoolId))
            .Select(d => new Mail(d.Email, "Meetings tomorrow", Greeting(d.FirstName, bodyBySchool[d.SchoolId])))
            .ToList();
    }

    private async Task<List<Contact>> DirectoratesForSchoolsAsync(
        IReadOnlyCollection<Guid> schoolIds, CancellationToken ct)
    {
        if (schoolIds.Count == 0) return [];

        var query =
            from d in db.Directorates.AsNoTracking()
            where schoolIds.Contains(d.SchoolId)
            join u in db.Users on d.UserId equals u.Id
            where u.DeactivatedAt == null
            select new Contact(d.SchoolId, u.Email, u.FirstName);

        return await query.IgnoreQueryFilters().ToListAsync(ct);
    }

    // School holidays on the given dates, so a schedule digest is not sent for a closed day.
    // Only Holiday concept exists in the model; per-class overrides / substitutions / cancellations
    // do not, so those are not applied (see the architectural note in the change summary).
    private async Task<HashSet<(Guid School, DateOnly Date)>> HolidaysAsync(
        IReadOnlyCollection<DateOnly> dates, CancellationToken ct)
    {
        var rows = await db.CalendarEvents.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Type == CalendarEventType.Holiday && dates.Contains(e.Date))
            .Select(e => new { e.SchoolId, e.Date })
            .ToListAsync(ct);

        return rows.Select(r => (r.SchoolId, r.Date)).ToHashSet();
    }

    // ---- formatting --------------------------------------------------------

    private static string? WeekBody(
        IReadOnlyList<DateOnly> dates,
        HashSet<(Guid, DateOnly)> holidays,
        IEnumerable<LessonRow> lessons,
        Func<LessonRow, string> line)
    {
        var byDay = lessons.ToLookup(l => l.Day);
        var sections = new List<string>();

        foreach (var date in dates)
        {
            var forDay = byDay[date.DayOfWeek]
                .Where(l => !holidays.Contains((l.SchoolId, date)))
                .OrderBy(l => l.PeriodNumber)
                .ToList();

            if (forDay.Count == 0) continue;

            sections.Add($"{date:dddd d MMM}");
            sections.AddRange(forDay.Select(l => "  " + line(l)));
            sections.Add("");
        }

        return sections.Count == 0 ? null : string.Join("\n", sections);
    }

    private static string Format(IEnumerable<string> lines)
    {
        var list = lines.ToList();
        return list.Count == 0 ? "Nothing scheduled." : string.Join("\n", list);
    }

    private static string Greeting(string firstName, string body) => $"Hello {firstName},\n\n{body}";

    private static string TeacherLine(LessonRow l) => $"{PeriodText(l)}: {l.Subject} — {l.ClassName}";
    private static string StudentLine(LessonRow l) => $"{PeriodText(l)}: {l.Subject}";
    private static string PeriodText(LessonRow l) =>
        $"Period {l.PeriodNumber} ({l.Start:HH\\:mm}-{l.End:HH\\:mm})";
    private static string MeetingLine(MeetingRow m) =>
        m.Start is { } start ? $"- {m.Title} at {start:HH\\:mm}" : $"- {m.Title}";

    // ---- delivery (bounded concurrency) ------------------------------------

    private async Task SendAllAsync(IReadOnlyList<Mail> mails, CancellationToken ct)
    {
        if (mails.Count == 0) return;

        using var gate = new SemaphoreSlim(MaxConcurrentEmails);

        var sends = mails.Select(async m =>
        {
            await gate.WaitAsync(ct);
            try { await email.SendAsync(m.Email, m.Subject, m.Body, ct); }
            finally { gate.Release(); }
        });

        await Task.WhenAll(sends);
    }
}
