namespace EduCore.Data;

// The write-side counterpart to IgnoreQueryFilters() on the read side: an explicit,
// auditable escape hatch for intentional cross-school/system writes (seeding the first
// School and Admin, admin tooling, etc). Everything else is blocked by
// SchoolIsolationSaveChangesInterceptor.
public static class SchoolIsolationScope
{
    private static readonly AsyncLocal<int> SuppressionDepth = new();

    public static bool IsSuppressed => SuppressionDepth.Value > 0;

    public static IDisposable Suppress()
    {
        SuppressionDepth.Value++;
        return new Restorer();
    }

    private sealed class Restorer : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (SuppressionDepth.Value > 0)
                SuppressionDepth.Value--;
        }
    }
}
