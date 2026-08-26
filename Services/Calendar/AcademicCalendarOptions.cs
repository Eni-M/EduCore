namespace EduCore.Services.Calendar;

// The default split of an academic year into its two grading periods. An academic year
// always has exactly two -- there is no add/remove; only the dates are ever editable, and
// these are only the starting point for a newly created year.
public class AcademicCalendarOptions
{
    public const string SectionName = "AcademicCalendar";

    // MM-dd. Assumed to fall in the same calendar year the academic year starts.
    public string FirstPeriodEnd { get; set; } = "12-26";

    // MM-dd. Assumed to fall in the calendar year after the one the academic year starts,
    // since a school year that begins in autumn resumes in January.
    public string SecondPeriodStart { get; set; } = "01-07";
}
