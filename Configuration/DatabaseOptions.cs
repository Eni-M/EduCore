namespace EduCore.Configuration;

// How the connection behaves when PostgreSQL is slow or briefly unreachable. Read at start-up,
// while the DbContext is being registered and before anything can be resolved from DI.
public class DatabaseOptions
{
    public const string SectionName = "Database";

    // Retries are safe here only because the application opens no explicit transactions, so the
    // execution strategy can replay a whole operation. A BeginTransaction added later has to be
    // wrapped in strategy.ExecuteAsync or EF refuses it at run time.
    public int MaxRetryCount { get; set; } = 3;
    public int MaxRetryDelaySeconds { get; set; } = 5;

    // A query that runs longer than this is not going to finish usefully -- it is going to hold
    // a connection while somebody watches a spinner. Above the slowest legitimate screen (the
    // statistics overview on a cold snapshot), well below anyone's patience.
    public int CommandTimeoutSeconds { get; set; } = 30;
}
