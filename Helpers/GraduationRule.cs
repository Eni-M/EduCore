namespace EduCore.Helpers;

// Which grades are the end of a school, and so the only ones a student can graduate from.
//
// These are not an arbitrary list: they are the last grade of each SchoolType -- Elementary
// runs 1-5, JuniorHigh 6-9, HighSchool 10-12. A student in grade 7 has not finished anything
// yet; a student in grade 9 has finished junior high. Moving from grade 5 to grade 6 is a
// transfer to a different school, not a graduation, which is why 5 counts.
//
// Pure and dependency-free on purpose: the rule is a fact about the school system, not about
// this database, so it is testable on its own and cannot drift with a query.
public static class GraduationRule
{
    public static readonly IReadOnlyList<int> FinalGrades = [5, 9, 12];

    public static bool IsFinalGrade(int grade) => FinalGrades.Contains(grade);

    // "5, 9 or 12" -- built from the list above so a message can never name a different set
    // of grades than the check enforces.
    public static string FinalGradesText =>
        string.Join(", ", FinalGrades.Take(FinalGrades.Count - 1)) + " or " + FinalGrades[^1];
}
