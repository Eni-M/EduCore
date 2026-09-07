using EduCore.Enums;

namespace EduCore.Dtos.Dashboard;

/// <summary>
/// One shape, filled differently per role, rather than six separate response types.
///
/// The alternative — a dashboard endpoint per role — would put the same rendering logic in six
/// front-end components and make "add a figure for teachers" a change in two places. Here the
/// server decides what a role should see and the client renders whatever it is handed, so a new
/// figure or a new role is a server change alone.
///
/// Every section is optional in practice: a role that has nothing to put in one returns it empty,
/// and the client omits the section rather than rendering an empty panel.
/// </summary>
public record DashboardDto(
    // A string, matching AuthResponseDto. No JsonStringEnumConverter is registered, so an enum
    // here would reach the client as a number while the role it already holds is a name.
    string Role,

    // What this dashboard is about: the school, the region, or the student's class. Shown under
    // the greeting so a user with access to several contexts can see which one they are in.
    string? Context,

    // Headline figures. Deliberately few — a dashboard that shows twenty numbers shows none.
    IReadOnlyList<DashboardStatDto> Stats,

    // The part that makes a dashboard worth opening: things that need doing, not things that
    // happened. Empty is the good state and is rendered as such.
    IReadOnlyList<DashboardAlertDto> NeedsAttention,

    // Today's lessons, for the roles that teach or attend them.
    IReadOnlyList<DashboardLessonDto> Today,

    // Recent activity relevant to this person specifically.
    IReadOnlyList<DashboardEntryDto> Recent);

/// <summary>
/// Severity, kept separate from any colour decision. The server says how serious something is;
/// the client decides what that looks like.
/// </summary>
public enum DashboardTone
{
    Neutral,
    Good,
    Warning,
    Critical
}

/// <summary>
/// Value is a string, not a number, on purpose: these are already-formatted figures — "94%",
/// "4.2", "—" for a figure that is genuinely unknown. Formatting a rate in the client would mean
/// re-deriving rounding rules that the grading and privacy layers already own, and the two would
/// eventually disagree.
/// </summary>
public record DashboardStatDto(string Label, string Value, string? Caption, DashboardTone Tone);

public record DashboardAlertDto(DashboardTone Tone, string Title, string Detail, string? Link);

public record DashboardLessonDto(
    int Period,
    TimeOnly Start,
    TimeOnly End,
    string SubjectName,
    string ClassName,
    // Null on a teacher's own dashboard, where naming the teacher is redundant.
    string? TeacherName);

public record DashboardEntryDto(string Title, string Detail, string? Link);
