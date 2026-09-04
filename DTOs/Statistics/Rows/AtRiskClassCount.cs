namespace EduCore.Dtos.Statistics.Rows;

// Database-side count of at-risk students per class. The class summary only needs the
// count, never the per-student rows, so the database groups instead of materialising them.
public record AtRiskClassCount(Guid ClassId, int Count);
