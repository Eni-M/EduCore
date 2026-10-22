using System.Linq;
using EduCore.Exceptions;
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

        var schoolIds = currentUser.SchoolIds;

        foreach (var entry in context.ChangeTracker.Entries<ISchoolScoped>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (!schoolIds.Contains(entry.Entity.SchoolId))
                // A 403, as docs/ARCHITECTURE.md promises -- not a 500 that reads as a server fault.
                // No school ids in the message: it goes back to the caller.
                throw new ForbiddenException("You cannot change records that belong to another school.");
        }
    }
}
