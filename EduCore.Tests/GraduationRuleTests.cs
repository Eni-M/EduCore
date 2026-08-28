using EduCore.Enums;
using EduCore.Helpers;
using Xunit;

namespace EduCore.Tests;

// A student graduates from the END of a school, not from any year of it. These three grades
// are the last grade of each SchoolType, so the test states that relationship rather than
// re-listing the numbers -- if a school type's range ever changes, this is what notices.
public class GraduationRuleTests
{
    [Theory]
    [InlineData(5)]   // last grade of Elementary  (1-5)
    [InlineData(9)]   // last grade of JuniorHigh  (6-9)
    [InlineData(12)]  // last grade of HighSchool  (10-12)
    public void LastGradeOfEachSchool_CanGraduate(int grade) =>
        Assert.True(GraduationRule.IsFinalGrade(grade));

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(11)]
    public void AnyOtherGrade_CannotGraduate(int grade) =>
        Assert.False(GraduationRule.IsFinalGrade(grade));

    // Moving from grade 5 to grade 6 is a transfer to a different school, which is precisely
    // why 5 graduates and 6 does not -- the boundary between two SchoolTypes, not a midpoint.
    [Fact]
    public void TheBoundaryBetweenSchools_IsTheGraduatingGrade()
    {
        Assert.True(GraduationRule.IsFinalGrade(5));
        Assert.False(GraduationRule.IsFinalGrade(6));

        Assert.True(GraduationRule.IsFinalGrade(9));
        Assert.False(GraduationRule.IsFinalGrade(10));
    }

    // There is one final grade per school type, and no more.
    [Fact]
    public void ThereIsOneGraduatingGrade_PerSchoolType() =>
        Assert.Equal(Enum.GetValues<SchoolType>().Length, GraduationRule.FinalGrades.Count);

    // The message names the same grades the check enforces. Built from the list rather than
    // typed out, so a message can never promise a rule the code does not apply.
    [Fact]
    public void TheMessage_NamesEveryGradeTheRuleAccepts()
    {
        var text = GraduationRule.FinalGradesText;

        Assert.Equal("5, 9 or 12", text);

        foreach (var grade in GraduationRule.FinalGrades)
            Assert.Contains(grade.ToString(), text);
    }
}
