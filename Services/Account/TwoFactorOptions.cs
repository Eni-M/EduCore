using EduCore.Enums;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Account;

public class TwoFactorOptions
{
    public const string SectionName = "TwoFactor";

    // How long an emailed code stays usable. Long enough for a slow inbox, short enough that a
    // code sitting in a mailbox stops being a key to the account.
    public int CodeLifetimeMinutes { get; set; } = 10;

    // Wrong guesses before the challenge is dead. Six digits is a million possibilities; this
    // cap is what stands between that and an attacker who can simply try them.
    public int MaxAttempts { get; set; } = 5;

    // Roles that cannot opt out. These are the accounts that create other accounts and can read
    // or change a whole school's records, so the second step is not theirs to decline.
    public UserRole[] RequiredForRoles { get; set; } =
        [UserRole.Directorate, UserRole.RegionalAdmin, UserRole.StateAdmin];

    public bool IsRequiredFor(UserRole role) => RequiredForRoles.Contains(role);
}

public class TwoFactorOptionsValidator : IValidateOptions<TwoFactorOptions>
{
    public ValidateOptionsResult Validate(string? name, TwoFactorOptions options)
    {
        if (options.CodeLifetimeMinutes is < 2 or > 60)
            return ValidateOptionsResult.Fail(
                $"TwoFactor:CodeLifetimeMinutes ({options.CodeLifetimeMinutes}) must be between 2 and 60. "
                + "Below 2 the code expires before a slow inbox delivers it; above an hour it is a "
                + "standing second factor sitting in a mailbox.");

        if (options.MaxAttempts is < 3 or > 10)
            return ValidateOptionsResult.Fail(
                $"TwoFactor:MaxAttempts ({options.MaxAttempts}) must be between 3 and 10. The cap is "
                + "what keeps a six-digit code out of guessing range, so it cannot be raised freely.");

        return ValidateOptionsResult.Success;
    }
}
