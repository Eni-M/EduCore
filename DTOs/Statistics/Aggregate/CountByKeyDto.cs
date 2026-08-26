namespace EduCore.Dtos.Statistics.Aggregate;

// One bucket of a breakdown -- by grade, by class, by age, by school. Key is the identifier
// the caller can filter by; Label is what a chart axis should show.
public record CountByKeyDto(string Key, string Label, int Count);
