using FluentValidation;

namespace EduCore.Validation;

// NIST SP 800-63B-4: length is what makes a password hard to guess, so passwords that may be
// the only factor need at least 15 characters, and verifiers "SHALL NOT impose other
// composition rules" -- mixed-case-and-digit rules push people to Password1, not to strength.
// What replaces them is a check against common and predictable choices.
public static class PasswordRules
{
    public const int MinimumLength = 15;

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Length(MinimumLength, 128)
            .WithMessage("Password must be at least 15 characters.")
            .Must(p => p is null || !PasswordBlocklist.IsBlocked(p))
            .WithMessage("This password is too common or too predictable. Choose a longer phrase.");
}
