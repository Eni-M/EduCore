namespace EduCore.Dtos.Statistics;

public record StatisticsFilter
{
    public Guid? SchoolId { get; init; }
    public Guid? CityId { get; init; }
    public Guid? ZoneId { get; init; }
    public Guid? StateId { get; init; }

    public Guid? AcademicYearId { get; init; }
    public Guid? GradingPeriodId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }

    public Guid? ClassId { get; init; }
    public int? Grade { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? TeacherId { get; init; }
}
