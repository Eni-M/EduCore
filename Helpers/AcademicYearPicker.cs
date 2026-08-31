using EduCore.Models;

namespace EduCore.Helpers;

public static class AcademicYearPicker
{
    public static AcademicYear? CurrentOn(IEnumerable<AcademicYear> years, DateOnly onDate)
    {
        var all = years.ToList();

        var containingToday = all.FirstOrDefault(y => y.StartDate <= onDate && onDate <= y.EndDate);

        if (containingToday is not null)
            return containingToday;

        var lastFinished = all
            .Where(y => y.EndDate < onDate)
            .OrderByDescending(y => y.EndDate)
            .FirstOrDefault();

        if (lastFinished is not null)
            return lastFinished;

        return all.OrderBy(y => y.StartDate).FirstOrDefault();
    }
}
