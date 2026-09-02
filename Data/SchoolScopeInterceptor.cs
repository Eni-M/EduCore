using EduCore.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduCore.Data;

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
        if (context is null)
            return;

        // StateAdmin acts platform-wide; RegionalAdmin creates school-scoped rows (e.g. a Directorate)
        // for schools in its city, and that city ownership is verified in the service layer.
        if (currentUser.IsStateAdmin || currentUser.IsRegionalAdmin)
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
