namespace EduCore.Dtos.Statistics.Individual;

// Deliberately splits the teacher's own work from their students' outcomes.
//
// A teacher handed the weakest class will always look worst on outcomes, so those figures
// measure class assignment at least as much as teaching. That is why every outcome carries a
// baseline and a previous-period value, and why there is no endpoint ranking teachers against
// each other: once a leaderboard exists, the context beside each number stops being read.
public record TeacherDetailStatisticsDto(
    TeacherWorkloadDto Workload,
    TeacherCoverageDto Coverage,
    IReadOnlyList<TeacherSubjectPerformanceDto> Performance);
