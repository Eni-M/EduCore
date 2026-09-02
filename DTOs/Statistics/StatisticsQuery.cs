namespace EduCore.Dtos.Statistics;

public record StatisticsQuery(
    Guid? SchoolId,
    bool CrossSchool,
    DateOnly From,
    DateOnly To,
    Guid? AcademicYearId,
    Guid? GradingPeriodId,
    Guid? ClassId,
    int? Grade,
    Guid? SubjectId,
    Guid? TeacherId,
    Guid? CityId,
    Guid? ZoneId,
    Guid? StateId,
    Guid? SchoolStateId = null);
