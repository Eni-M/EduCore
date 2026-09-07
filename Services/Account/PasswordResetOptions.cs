using Microsoft.Extensions.Options;

namespace EduCore.Services.Account;

public class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    // How long a reset link stays redeemable. Short enough that a forwarded or archived mail
    // stops working quickly, long enough to survive a delayed inbox.
    public int TokenLifetimeMinutes { get; set; } = 60;

    // Where the emailed link points. The token is appended as ?token=... , so this is the
    // address of the front-end reset screen, not an API route.
    public string ResetUrlTemplate { get; set; } = "http://localhost:5173/reset-password";
}

public class PasswordResetOptionsValidator : IValidateOptions<PasswordResetOptions>
{
    public ValidateOptionsResult Validate(string? name, PasswordResetOptions options)
    {
        if (options.TokenLifetimeMinutes is < 5 or > 1440)
            return ValidateOptionsResult.Fail(
                $"TokenLifetimeMinutes ({options.TokenLifetimeMinutes}) must be between 5 and 1440. "
                + "Below 5 the link expires before a slow inbox delivers it; above a day it is a "
                + "standing key to the account.");

        return !Uri.TryCreate(options.ResetUrlTemplate, UriKind.Absolute, out _)
            ? ValidateOptionsResult.Fail(
                $"ResetUrlTemplate ('{options.ResetUrlTemplate}') must be an absolute URL — "
                + "it is emailed to users as a link.")
            : ValidateOptionsResult.Success;
    }
}
