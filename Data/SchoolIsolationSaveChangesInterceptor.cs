using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;

// Global query filters only make cross-school data invisible to reads -- nothing stops
// Adding/Modifying an ISchoolScoped entity with a SchoolId that isn't the caller's own.
// This is the write-side enforcement: any such write outside the current school context
// throws, unless explicitly allowed via SchoolIsolationScope.Suppress().
public class SchoolIsolationSaveChangesInterceptor(ICurrentSchoolAccessor currentSchoolAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Validate(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Validate(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Validate(DbContext? context)
    {
        if (context is null || SchoolIsolationScope.IsSuppressed)
            return;

        var currentSchoolId = currentSchoolAccessor.CurrentSchoolId;

        foreach (var entry in context.ChangeTracker.Entries<ISchoolScoped>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            if (currentSchoolId is null || entry.Entity.SchoolId != currentSchoolId)
                throw new InvalidOperationException(
                    $"Attempted to write a {entry.Entity.GetType().Name} with SchoolId={entry.Entity.SchoolId} " +
                    $"outside the current school context ({currentSchoolId?.ToString() ?? "none"}). " +
                    "Use SchoolIsolationScope.Suppress() for an intentional cross-school/system write.");
        }
    }
}
