using EduCore.Dtos.Guardian;
using EduCore.Validation.Guardian;
using Xunit;

namespace EduCore.Tests;

// The personal number and the national id were each "not empty, under fifty characters" in
// three separate validators. These pin the single shared rule that replaced them.
public class NationalIdentifierRulesTests
{
    private static readonly GuardianCreateDtoValidator Validator = new();

    private static bool Accepts(string identifier) =>
        !Validator.Validate(new GuardianCreateDto(
                "Ana", "Berisha", "ana@shkolla.example", "Aa1aaaaaa", identifier, "044111222", []))
            .Errors.Any(e => e.PropertyName == nameof(GuardianCreateDto.NationalId));

    [Theory]
    [InlineData("1234567")]              // the shortest allowed
    [InlineData("12345678901234567890")] // the longest allowed
    [InlineData("AB-123456")]
    [InlineData("12/34/5678")]
    [InlineData("RKS 1234567")]
    [InlineData("çmimtar12")]            // letters outside ASCII
    [InlineData("ЈМБГ1234567")]          // and outside Latin
    public void A_real_looking_identifier_is_accepted(string identifier) =>
        Assert.True(Accepts(identifier), identifier);

    [Theory]
    [InlineData("123456")]                // one short
    [InlineData("123456789012345678901")] // one long
    [InlineData("")]
    public void A_wrong_length_is_rejected(string identifier) =>
        Assert.False(Accepts(identifier), identifier);

    [Theory]
    [InlineData("1234%567")]
    [InlineData("1234$567")]
    [InlineData("1234#567")]
    [InlineData("1234@567")]
    [InlineData("<script>x")]
    public void A_symbol_is_rejected(string identifier) =>
        Assert.False(Accepts(identifier), identifier);

    [Theory]
    [InlineData(" 1234567")]
    [InlineData("1234567 ")]
    [InlineData("-1234567")]
    [InlineData("1234567/")]
    public void A_separator_at_either_end_is_rejected(string identifier) =>
        Assert.False(Accepts(identifier), identifier);
}
