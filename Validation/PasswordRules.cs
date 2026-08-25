using FluentValidation;

namespace EduCore.Validation;

public static class PasswordRules
{
    // One definition, used by every place that accepts a password, so an account created by
    // an admin cannot start out weaker than one the owner sets for themselves.
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Length(8, 128)
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"\d").WithMessage("Password must contain a digit.");
}
