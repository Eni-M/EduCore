namespace EduCore.Dtos.Statistics;

// A StatisticsFilter after it has been checked and resolved: the caller's access verified, and
// the period turned into a concrete calendar window. Services and repositories receive this,
// never the raw filter, so no query can run against unvalidated input.
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
    Guid? TeacherId);
