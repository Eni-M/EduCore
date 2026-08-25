using EduCore.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;

// Query filters hide other schools' rows from reads. This is the write side: you may only
// save a row into your own school. A SuperAdmin has no school and may write into any.
public class SchoolScopeInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
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
        if (context is null || currentUser.IsSuperAdmin)
            return;

        var schoolId = currentUser.SchoolId;

        foreach (var entry in context.ChangeTracker.Entries<ISchoolScoped>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (entry.Entity.SchoolId != schoolId)
                throw new InvalidOperationException(
                    $"Cannot save {entry.Entity.GetType().Name} for school {entry.Entity.SchoolId} " +
                    $"while acting for school {schoolId?.ToString() ?? "none"}.");
        }
    }
}
