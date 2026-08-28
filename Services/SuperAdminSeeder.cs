using EduCore.Data;
using EduCore.Enums;
using EduCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services;

// Creates the first super administrator, once, on a database that has none.
//
// The credentials used to live in appsettings.json, which is committed. That
// meant every deployment stood up on a fresh database had the same super-admin
// email and the same password, both published -- and whoever signed in first got
// to set the new one, because the account is created with MustChangePassword.
// Reading the repository was the whole attack.
//
// They come from configuration only now, with no fallback of any kind. Set
// SuperAdmin__Email and SuperAdmin__Password in .env (see .env.example) or in
// the environment. Leave them unset and nothing is seeded: a database with no
// super admin is a database nobody can sign in to, which is inconvenient and
// safe, where a default account is convenient and not.
public static class SuperAdminSeeder
{
    // The account is created with MustChangePassword, so this password is only
    // ever used once. It still has to survive the window before it is changed --
    // and it is not covered by the FluentValidation rules, which run on request
    // DTOs and never see this path. Longer than those rules ask for, because
    // this account owns every school on the platform.
    private const int MinimumPasswordLength = 12;

    // The same shape PasswordRules asks of everyone else, spelled out because
    // FluentValidation's rule builder needs a DTO to hang off and there is none
    // here.
    private static bool IsStrongEnough(string password) =>
        password.Length >= MinimumPasswordLength
        && password.Any(char.IsLower)
        && password.Any(char.IsUpper)
        && password.Any(char.IsDigit);

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SuperAdminSeeder));

        // "Can anyone actually administer this system", not "does a row exist".
        // A deactivated super administrator is a row that cannot sign in, and
        // counting it left a database nobody could get into and a seeder that
        // silently declined to fix it on every restart.
        if (await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin && u.DeactivatedAt == null))
            return;

        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];

        // Not configured is a decision, not a failure: most starts of a running
        // system already have a super admin and want nothing done here. It is
        // logged because the one time it matters -- the very first start of a new
        // deployment -- silence would leave someone locked out with no clue why.
        // The password itself is never logged.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No super administrator exists, and SuperAdmin:Email / SuperAdmin:Password are not " +
                "configured, so none was created. Set them in .env (see .env.example) or in the " +
                "environment, then restart.");
            return;
        }

        if (!IsStrongEnough(password))
            throw new InvalidOperationException(
                $"SuperAdmin:Password must be at least {MinimumPasswordLength} characters and " +
                "contain a lowercase letter, an uppercase letter and a digit. " +
                "This account owns every school on the platform.");

        await users.BuildLoginAsync("Super", "Admin", email, password, UserRole.SuperAdmin, CancellationToken.None);

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Created the initial super administrator ({Email}). They must change this password on first sign-in.",
            email);
    }
}
