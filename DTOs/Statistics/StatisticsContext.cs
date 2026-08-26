namespace EduCore.Dtos.Statistics;

// A StatisticsFilter after it has been checked and resolved: the caller's access has been
// verified and the period has become a concrete calendar window. Services receive this, never
// the raw filter, so no query can accidentally run against unvalidated input.
public record StatisticsContext(
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
