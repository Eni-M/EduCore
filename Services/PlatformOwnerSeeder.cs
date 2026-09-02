using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using EduCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services;

public static class PlatformOwnerSeeder
{
    private const int MinimumPasswordLength = 12;
    private const string DefaultStateName = "Kosova";

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
            .CreateLogger(typeof(PlatformOwnerSeeder));

        if (await db.Users.AnyAsync(u => u.Role == UserRole.StateAdmin && u.DeactivatedAt == null))
            return;

        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No state administrator exists, and SuperAdmin:Email / SuperAdmin:Password are not " +
                "configured, so none was created. Set them in .env (see .env.example) or in the " +
                "environment, then restart.");
            return;
        }

        if (!IsStrongEnough(password))
            throw new InvalidOperationException(
                $"SuperAdmin:Password must be at least {MinimumPasswordLength} characters and " +
                "contain a lowercase letter, an uppercase letter and a digit. " +
                "This account owns every school on the platform.");

        var stateName = configuration["SuperAdmin:StateName"] ?? DefaultStateName;

        var state = await db.States.FirstOrDefaultAsync(s => s.Name == stateName);
        if (state is null)
        {
            state = new State { Name = stateName };
            await db.States.AddAsync(state);
        }

        var user = await users.BuildLoginAsync(
            "Super", "Admin", email, password, UserRole.StateAdmin, CancellationToken.None);

        await db.StateAdmins.AddAsync(new StateAdmin { User = user, State = state });

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Created the initial state administrator ({Email}) for {State}. They must change this " +
            "password on first sign-in.",
            email, stateName);
    }
}
