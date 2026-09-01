using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

public class GradeBandsTests
{
    [Theory]
    [InlineData(SchoolType.Elementary, 1)]
    [InlineData(SchoolType.Elementary, 5)]
    [InlineData(SchoolType.JuniorHigh, 6)]
    [InlineData(SchoolType.JuniorHigh, 9)]
    [InlineData(SchoolType.HighSchool, 10)]
    [InlineData(SchoolType.HighSchool, 12)]
    public void Grade_inside_its_school_types_band_does_not_throw(SchoolType type, int grade) =>
        GradeBands.Validate(type, grade);

    [Theory]
    [InlineData(SchoolType.Elementary, 6)]
    [InlineData(SchoolType.Elementary, 0)]
    [InlineData(SchoolType.JuniorHigh, 5)]
    [InlineData(SchoolType.JuniorHigh, 10)]
    [InlineData(SchoolType.HighSchool, 9)]
    [InlineData(SchoolType.HighSchool, 13)]
    public void Grade_outside_its_school_types_band_throws_business_exception(SchoolType type, int grade) =>
        Assert.Throws<BusinessException>(() => GradeBands.Validate(type, grade));
}
