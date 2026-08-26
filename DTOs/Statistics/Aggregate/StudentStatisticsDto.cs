namespace EduCore.Dtos.Statistics.Aggregate;

// GenderNotRecorded counts students created before gender was captured. Reported rather than
// folded into either group, so the gap is visible instead of silently distorting the split.
//
// These are exact headcounts, not Measures, so they are not cell-suppressed: ?grade=12 can
// legitimately answer "Female: 1". That is safe only because gender cannot currently be
// crossed with an outcome -- there is no gender filter on StatisticsFilter. If one is ever
// added, these counts must go through the suppressor first, or the API starts answering
// "grade 12, female, average 2.1", which names a child.
public record StudentStatisticsDto(
    int Total,
    int Active,
    int Graduated,
    int NewArrivals,
    int Female,
    int Male,
    int GenderNotRecorded);
