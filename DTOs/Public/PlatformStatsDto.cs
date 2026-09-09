namespace EduCore.Dtos.Public;

// Whole-platform totals for the public landing page. Deliberately the coarsest possible
// figures: nothing here narrows to a school, a class or a person, so there is no cell small
// enough to identify anybody.
//
// Counts rather than rates, on purpose. A rate needs a sample to mean anything -- one present
// pupil would publish "100% attendance" across the country -- whereas a count is honest at
// every size.
//
// Every figure is nullable because it passes through the same ICellSuppressor the statistics
// screens use: a count below Privacy:MinimumCellSize comes back null and the page leaves it
// out rather than publishing it. Zero is not suppressed -- "nothing recorded yet" is a fact,
// not a small sample.
public record PlatformStatsDto(
    int? Schools,
    int? Cities,
    int? Students,
    int? Teachers,
    int? Lessons,
    int? Attendances);
