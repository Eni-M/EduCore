using EduCore.Enums;
using EduCore.Helpers;
using Xunit;

namespace EduCore.Tests;

public class GraduationRuleTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(12)]
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

    [Fact]
    public void TheBoundaryBetweenSchools_IsTheGraduatingGrade()
    {
        Assert.True(GraduationRule.IsFinalGrade(5));
        Assert.False(GraduationRule.IsFinalGrade(6));

        Assert.True(GraduationRule.IsFinalGrade(9));
        Assert.False(GraduationRule.IsFinalGrade(10));
    }

    [Fact]
    public void ThereIsOneGraduatingGrade_PerSchoolType() =>
        Assert.Equal(Enum.GetValues<SchoolType>().Length, GraduationRule.FinalGrades.Count);

    [Fact]
    public void TheMessage_NamesEveryGradeTheRuleAccepts()
    {
        var text = GraduationRule.FinalGradesText;

        Assert.Equal("5, 9 or 12", text);

        foreach (var grade in GraduationRule.FinalGrades)
            Assert.Contains(grade.ToString(), text);
    }
}
