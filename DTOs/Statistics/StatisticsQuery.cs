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
    // Student origin (where the family comes from).
    Guid? CountryId,
    Guid? OriginCityId,
    // School location state (the ministry can narrow to a region).
    Guid? SchoolStateId = null);
