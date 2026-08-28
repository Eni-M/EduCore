using EduCore.Models;

namespace EduCore.Helpers;

// Which academic year a report should default to on a given day.
//
// Three cases, in order:
//
//   1. The year today falls inside. The obvious answer during term time.
//   2. Otherwise the year that ended most recently. During the summer break that is the
//      year just taught -- the one whose marks and lessons people are still looking at.
//   3. Otherwise the soonest year still to come, for a school whose first year has not
//      started yet.
//
// Case 2 is the one that used to be wrong. The old rule sorted every year by start date and
// took the latest, so in August it picked the year about to BEGIN -- which has no marks, no
// lessons and no attendance in it. A school with a full year of records showed an empty
// report, and only showed a full one if somebody picked the previous year by hand.
//
// A school has a handful of years, so this sorts them in memory rather than in SQL: the rule
// is worth being able to read and test on its own.
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
