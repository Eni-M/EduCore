using EduCore.Enums;

namespace EduCore.Dtos.Dashboard;

// One shape filled differently per role, so the client renders what it is handed instead of
// branching six ways. Sections a role has nothing for come back empty.
public record DashboardDto(
    // A string, matching AuthResponseDto: no JsonStringEnumConverter is registered, so an enum
    // would reach the client as a number.
    string Role,
    string? Context,
    IReadOnlyList<DashboardStatDto> Stats,

    // Things that need doing, not things that happened. Empty is the good state.
    IReadOnlyList<DashboardAlertDto> NeedsAttention,
    IReadOnlyList<DashboardLessonDto> Today,
    IReadOnlyList<DashboardEntryDto> Recent);

// Severity only; the client decides what it looks like.
public enum DashboardTone
{
    Neutral,
    Good,
    Warning,
    Critical
}

// Value is already-formatted text ("94%", "4.2", "—"). Formatting in the client would re-derive
// rounding the grading and privacy layers already own.
public record DashboardStatDto(string Label, string Value, string? Caption, DashboardTone Tone);

public record DashboardAlertDto(DashboardTone Tone, string Title, string Detail, string? Link);

public record DashboardLessonDto(
    int Period,
    TimeOnly Start,
    TimeOnly End,
    string SubjectName,
    string ClassName,
    // Null on a teacher's own dashboard.
    string? TeacherName);

public record DashboardEntryDto(string Title, string Detail, string? Link);
