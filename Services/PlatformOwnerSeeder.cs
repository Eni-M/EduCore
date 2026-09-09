using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using EduCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services;

public static class PlatformOwnerSeeder
{
    // Defaults only. Both are overridden from the SuperAdmin section; see appsettings.json.
    private const int FallbackMinimumPasswordLength = 12;
    private const string FallbackStateName = "Kosova";

    private static bool IsStrongEnough(string password, int minimumLength) =>
        password.Length >= minimumLength
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

        var stateName = configuration["SuperAdmin:StateName"] ?? FallbackStateName;
        var minimumPasswordLength = configuration.GetValue(
            "SuperAdmin:MinimumPasswordLength", FallbackMinimumPasswordLength);

        var adminUserIds = await db.Users
            .Where(u => u.Role == UserRole.StateAdmin)
            .Select(u => u.Id)
            .ToListAsync();
        var linkedUserIds = await db.StateAdmins.Select(sa => sa.UserId).ToListAsync();
        var orphanIds = adminUserIds.Except(linkedUserIds).ToList();

        if (orphanIds.Count > 0)
        {
            var healState = await GetOrAddStateAsync(db, stateName);
            foreach (var userId in orphanIds)
                await db.StateAdmins.AddAsync(new StateAdmin { UserId = userId, State = healState });
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Linked {Count} existing state administrator(s) with no profile row to {State}.",
                orphanIds.Count, stateName);
        }

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

        if (!IsStrongEnough(password, minimumPasswordLength))
            throw new InvalidOperationException(
                $"SuperAdmin:Password must be at least {minimumPasswordLength} characters and " +
                "contain a lowercase letter, an uppercase letter and a digit. " +
                "This account owns every school on the platform.");

        var state = await GetOrAddStateAsync(db, stateName);
        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser is not null)
        {
            if (existingUser.Role != UserRole.StateAdmin)
                existingUser.Role = UserRole.StateAdmin;

            if (existingUser.DeactivatedAt is not null)
                existingUser.DeactivatedAt = null;

            if (!await db.StateAdmins.AnyAsync(sa => sa.UserId == existingUser.Id))
                await db.StateAdmins.AddAsync(new StateAdmin { UserId = existingUser.Id, State = state });

            await db.SaveChangesAsync();

            logger.LogInformation(
                "The configured platform owner email already exists; reusing user {UserId} as the state administrator for {State}.",
                existingUser.Id, stateName);
            return;
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

    private static async Task<State> GetOrAddStateAsync(AppDbContext db, string name)
    {
        var state = await db.States.FirstOrDefaultAsync(s => s.Name == name);
        if (state is null)
        {
            state = new State { Name = name };
            await db.States.AddAsync(state);
        }

        return state;
    }
}
