using EduCore.Helpers;
using EduCore.Services.Statistics;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// The rules behind the guardian weekly summary that do not need a database to be wrong: what a
// half-entered mark is called, and how a mark reaches a parent as a number.
//
// The query itself -- who receives one, over what window -- is not covered here. It is a single
// EF query over five joined tables, and a fake that answered it would be asserting my own
// arithmetic rather than the database's. That is the integration-test gap already on record.
public class GuardianDigestTests
{
    private static GradingCalculator Grading(int passMark = 2) =>
        new(Options.Create(new GradingOptions
        {
            FirstMarkWeight = 0.40,
            SecondMarkWeight = 0.60,
            PassMark = passMark,
            AtRiskMark = 2
        }));

    [Fact]
    public void A_complete_mark_reaches_the_parent_as_its_final_grade()
    {
        // 4 and 5 weight to 4.6, which rounds away from zero to 5. A guardian sees the same
        // number the report card shows, because both go through IGradingCalculator.
        Assert.Equal(5, Grading().FinalMark(4, 5));
    }

    [Theory]
    [InlineData(3, null)]
    [InlineData(null, 4)]
    [InlineData(null, null)]
    public void A_half_entered_mark_has_no_final_grade(int? first, int? second)
    {
        // The digest says "recorded, not yet complete" for these rather than showing a number.
        // A mark with one component counts for nothing yet, and a parent reading a bare "3"
        // would take it as the grade their child received.
        Assert.Null(Grading().FinalMark(first, second));
    }
}
