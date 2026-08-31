namespace EduCore.Helpers;

public static class GraduationRule
{
    public static readonly IReadOnlyList<int> FinalGrades = [5, 9, 12];

    public static bool IsFinalGrade(int grade) => FinalGrades.Contains(grade);

    public static string FinalGradesText =>
        string.Join(", ", FinalGrades.Take(FinalGrades.Count - 1)) + " or " + FinalGrades[^1];
}
