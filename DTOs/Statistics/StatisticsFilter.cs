namespace EduCore.Dtos.Statistics;

// The one filter every statistics endpoint accepts, bound from the query string.
//
// The period can be given either as a school-defined reference (grading period or academic
// year) or as a raw calendar window. Both are supported deliberately: the domain is keyed on
// academic years, but government reports are asked for by day, week, month, year and decade,
// and an academic year straddles two calendar years. See StatisticsPeriodResolver.
public record StatisticsFilter
{
    // SuperAdmin only. An Admin supplying another school's id is refused, not ignored.
    public Guid? SchoolId { get; init; }

    public Guid? AcademicYearId { get; init; }
    public Guid? GradingPeriodId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }

    public Guid? ClassId { get; init; }
    public int? Grade { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? TeacherId { get; init; }
}
