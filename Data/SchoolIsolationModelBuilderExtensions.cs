using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Data;

public static class SchoolIsolationModelBuilderExtensions
{
    public static void ApplySchoolIsolation(this ModelBuilder modelBuilder, ICurrentSchoolAccessor currentSchoolAccessor)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(ISchoolScoped).IsAssignableFrom(clrType))
                continue;

            ApplySchoolFilter(modelBuilder, clrType, currentSchoolAccessor);
        }
    }

    private static void ApplySchoolFilter(ModelBuilder modelBuilder, Type clrType, ICurrentSchoolAccessor currentSchoolAccessor)
    {
        var method = typeof(SchoolIsolationModelBuilderExtensions)
            .GetMethod(nameof(ApplySchoolFilterGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;

        method.MakeGenericMethod(clrType).Invoke(null, new object[] { modelBuilder, currentSchoolAccessor });
    }

    private static void ApplySchoolFilterGeneric<TEntity>(ModelBuilder modelBuilder, ICurrentSchoolAccessor currentSchoolAccessor)
        where TEntity : class, ISchoolScoped
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => entity.SchoolId == currentSchoolAccessor.CurrentSchoolId);
    }
}
