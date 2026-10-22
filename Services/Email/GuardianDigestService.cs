using EduCore.Data;
using EduCore.Helpers;
using EduCore.Services.Account;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EduCore.Services.Interfaces;

namespace EduCore.Services.Email;

// The weekly summary a guardian actually wants: what changed for each of their children in the
// last seven days.
//
// Deliberately a digest rather than an email per event. A parent of three children would
// otherwise receive dozens of messages a week, and the reliable outcome of that is a filter rule
// that hides all of them -- including the one about a failing grade. One message a week is one
// somebody reads.
//
// Silence is the design: a guardian whose children had a quiet week gets nothing. A weekly mail
// that arrives saying "no changes" trains people to ignore it, which defeats the purpose of the
// weeks when there is something to say.
//
// IgnoreQueryFilters throughout, as in ScheduleDigestService and for the same reason: this runs
// from a background scope with no signed-in user, so the per-request school filter would match
// nothing. It is safe because every row is reached by walking out from a specific guardian's own
// links -- a guardian only ever sees children they are linked to, whichever school those are in.
// That last part matters here: a guardian may have children at two different schools.
public class GuardianDigestService(
    AppDbContext db,
    IEmailSender email,
    IGradingCalculator grading,
    IOptions<PasswordResetOptions> passwordReset,
    IOptions<EmailOptions> emailOptions,
    ILogger<GuardianDigestService> logger) : IGuardianDigestService
{
    private readonly string _appUrl = AccountMailTemplates.AppUrlFrom(passwordReset.Value.ResetUrlTemplate);

    private readonly int _maxConcurrentEmails = emailOptions.Value.MaxConcurrentSends;

    private sealed record ChildChanges(
        string FirstName,
        IReadOnlyList<string> Marks,
        int Absences,
        int ExcusedAbsences);

    public async Task<int> SendWeeklyAsync(DateOnly today, CancellationToken ct)
    {
        var since = today.AddDays(-7);
        var sinceUtc = since.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // One query for the whole platform's guardian-child links, rather than one per guardian.
        var links = await (
            from gs in db.GuardianStudents.IgnoreQueryFilters(QueryFilters.SchoolOnly)
            join g in db.Guardians.IgnoreQueryFilters(QueryFilters.SchoolOnly) on gs.GuardianId equals g.Id
            join gu in db.Users.IgnoreQueryFilters(QueryFilters.SchoolOnly) on g.UserId equals gu.Id
            join s in db.Students.IgnoreQueryFilters(QueryFilters.SchoolOnly) on gs.StudentId equals s.Id
            join su in db.Users.IgnoreQueryFilters(QueryFilters.SchoolOnly) on s.UserId equals su.Id
            where gu.DeactivatedAt == null
                  && !gu.EmailDigestsOptOut
                  && gu.Email != null
                  && s.GraduationDate == null
            select new
            {
                GuardianUserId = gu.Id,
                GuardianEmail = gu.Email,
                GuardianFirstName = gu.FirstName,
                StudentId = s.Id,
                ChildFirstName = su.FirstName
            })
            .ToListAsync(ct);

        if (links.Count == 0)
            return 0;

        var studentIds = links.Select(l => l.StudentId).Distinct().ToList();

        // Marks written in the window. RecordedAt is null for rows predating the column, and a
        // null is excluded rather than treated as recent -- an old mark must not resurface as
        // this week's news.
        var marks = await (
            from m in db.Marks.IgnoreQueryFilters(QueryFilters.SchoolOnly)
            join e in db.Enrollments.IgnoreQueryFilters(QueryFilters.SchoolOnly) on m.EnrollmentId equals e.Id
            join sub in db.Subjects.IgnoreQueryFilters(QueryFilters.SchoolOnly) on m.SubjectId equals sub.Id
            where studentIds.Contains(e.StudentId)
                  && m.RecordedAt != null
                  && m.RecordedAt >= sinceUtc
            select new { e.StudentId, Subject = sub.Name, m.FirstMark, m.SecondMark, m.WeightedValue })
            .ToListAsync(ct);

        // Attendance hangs off a diary entry, so an absence always refers to a lesson that was
        // actually taught on a known date.
        var absences = await (
            from a in db.Attendances.IgnoreQueryFilters(QueryFilters.SchoolOnly)
            join e in db.Enrollments.IgnoreQueryFilters(QueryFilters.SchoolOnly) on a.EnrollmentId equals e.Id
            join d in db.DiaryEntries.IgnoreQueryFilters(QueryFilters.SchoolOnly) on a.DiaryEntryId equals d.Id
            where studentIds.Contains(e.StudentId)
                  && !a.IsPresent
                  && d.Date > since
                  && d.Date <= today
            select new { e.StudentId, a.IsExcused })
            .ToListAsync(ct);

        var byStudent = studentIds.ToDictionary(
            id => id,
            id => new
            {
                Marks = marks.Where(m => m.StudentId == id).ToList(),
                Absences = absences.Where(a => a.StudentId == id).ToList()
            });

        var messages = links
            .GroupBy(l => new { l.GuardianUserId, l.GuardianEmail, l.GuardianFirstName })
            .Select(guardian =>
            {
                var children = guardian
                    .Select(link =>
                    {
                        var data = byStudent[link.StudentId];

                        return new ChildChanges(
                            link.ChildFirstName,
                            data.Marks
                                .Select(m => $"{m.Subject}: {Describe(m.WeightedValue)}")
                                .OrderBy(line => line)
                                .ToList(),
                            data.Absences.Count,
                            data.Absences.Count(a => a.IsExcused));
                    })
                    // A child with a quiet week is left out entirely rather than listed as
                    // "nothing to report", which is noise in a message about what changed.
                    .Where(c => c.Marks.Count > 0 || c.Absences > 0)
                    .OrderBy(c => c.FirstName)
                    .ToList();

                return children.Count == 0
                    ? null
                    : new
                    {
                        Email = guardian.Key.GuardianEmail!,
                        Subject = $"EduCore weekly summary, {since:d MMM} to {today:d MMM}",
                        Body = Compose(guardian.Key.GuardianFirstName, since, today, children)
                    };
            })
            .Where(m => m is not null)
            .ToList();

        using var gate = new SemaphoreSlim(_maxConcurrentEmails);

        await Task.WhenAll(messages.Select(async m =>
        {
            await gate.WaitAsync(ct);

            try
            {
                await email.SendAsync(m!.Email, m.Subject, EmailLayout.FromText(m.Subject, m.Body, _appUrl), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Per recipient, so one bad address does not cost every other guardian their
                // summary -- the same isolation ScheduleDigestService uses.
                logger.LogError(ex, "Failed to send a guardian weekly digest.");
            }
            finally
            {
                gate.Release();
            }
        }));

        return messages.Count;
    }

    // The mark as a reader recognises it: the final grade when both components exist, and an
    // explicit note when only one does, because a half-entered mark counts for nothing yet and a
    // guardian should not read it as a grade.
    private string Describe(double? weightedValue) =>
        grading.FinalMark(weightedValue) is { } final
            ? final.ToString()
            : "recorded, not yet complete";

    private static string Compose(
        string guardianName, DateOnly since, DateOnly to, IReadOnlyList<ChildChanges> children)
    {
        var sections = children.Select(child =>
        {
            var lines = new List<string> { $"{child.FirstName}" };

            if (child.Marks.Count > 0)
            {
                lines.Add("  New marks:");
                lines.AddRange(child.Marks.Select(m => $"    {m}"));
            }

            if (child.Absences > 0)
            {
                var excused = child.ExcusedAbsences > 0 ? $" ({child.ExcusedAbsences} excused)" : "";
                lines.Add($"  Absences: {child.Absences}{excused}");
            }

            return string.Join("\n", lines);
        });

        return $"""
                Hello {guardianName},

                Here is what changed for your children between {since:d MMMM} and {to:d MMMM}.

                {string.Join("\n\n", sections)}

                Sign in to see the full record. You can turn these weekly summaries off from your
                profile at any time.
                """;
    }
}
