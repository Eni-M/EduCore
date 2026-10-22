using EduCore.Dtos.User;
using EduCore.Validation;
using EduCore.Validation.User;
using Xunit;

namespace EduCore.Tests;

// NIST SP 800-63B-4: length over composition, plus a check against common and predictable
// choices. These pin both halves -- a long passphrase with no digits or capitals must pass, and
// a long-but-predictable one must not.
public class PasswordRulesTests
{
    [Theory]
    [InlineData("passwordpassword")]
    [InlineData("PasswordPassword")]
    [InlineData("abcdefghijklmnopq")]
    [InlineData("qwertyuiopasdfghj")]
    [InlineData("987654321098765")]
    [InlineData("aaaaaaaaaaaaaaaa")]
    [InlineData("abcabcabcabcabcabc")]
    [InlineData("hello1hello1hello1")]
    [InlineData("1q2w3e4r5t6y7u8i")]
    public void Predictable_passwords_are_blocked(string password) =>
        Assert.True(PasswordBlocklist.IsBlocked(password));

    [Theory]
    [InlineData("river cobalt lantern")]
    [InlineData("my dog eats tangerines")]
    [InlineData("Tr0ub4dor&3xtra-long")]
    public void Ordinary_passphrases_pass(string password) =>
        Assert.False(PasswordBlocklist.IsBlocked(password));

    [Fact]
    public void A_long_passphrase_needs_no_capitals_or_digits()
    {
        var result = new ResetPasswordDtoValidator().Validate(
            new ResetPasswordDto("token", "river cobalt lantern", "river cobalt lantern"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Fourteen_characters_is_too_short()
    {
        var result = new ResetPasswordDtoValidator().Validate(
            new ResetPasswordDto("token", "Short1Password", "Short1Password"));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password must be at least 15 characters.");
    }
}
