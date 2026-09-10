using FluentValidation;

namespace EduCore.Validation;

// A student's personal number and a guardian's national id are the same kind of value and were
// each validated as "not empty, under fifty characters" in three separate places. One rule, so
// the three cannot drift.
public static class NationalIdentifierRules
{
    // Letters in any alphabet -- these are national documents, and Albanian and Serbian ones are
    // written in their own -- plus digits and the separators that actually appear on them: space,
    // hyphen, slash. A separator may not open or close the value, which is what keeps " - " and
    // similar out. Everything else, %$#@ included, is rejected.
    public const string Pattern = @"^[\p{L}\p{Nd}](?:[\p{L}\p{Nd} /-]*[\p{L}\p{Nd}])?$";

    public const int MinimumLength = 7;
    public const int MaximumLength = 20;

    public static IRuleBuilderOptions<T, string> NationalIdentifier<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Length(MinimumLength, MaximumLength)
            .Matches(Pattern)
            // One literal, not a concatenation: the catalogue check only recognises a whole
            // string, and a message it cannot see is a message that never gets translated.
            .WithMessage("An identifier may contain only letters, digits, spaces, hyphens and slashes.");
}
