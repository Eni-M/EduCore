namespace EduCore.Services.Statistics;

// How aggressively small aggregates are withheld. A privacy rule, not a grading rule -- it has
// nothing to do with how a mark is worked out, so it gets its own section rather than riding
// along inside GradingOptions.
public class PrivacyOptions
{
    public const string SectionName = "Privacy";

    // Aggregates computed from fewer rows than this are suppressed rather than returned: small
    // buckets identify individual students.
    public int MinimumCellSize { get; set; } = 5;
}
