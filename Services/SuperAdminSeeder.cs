using EduCore.Data;
using EduCore.Enums;
using EduCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services;

public static class SuperAdminSeeder
{
    private const int MinimumPasswordLength = 12;

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

        if (await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin && u.DeactivatedAt == null))
            return;

        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];

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
