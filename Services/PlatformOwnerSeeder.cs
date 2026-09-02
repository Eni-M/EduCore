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

        var stateName = configuration["SuperAdmin:StateName"] ?? DefaultStateName;

        // A StateAdmin user with no StateAdmins row (the old SuperAdmin remapped by the
        // RoleHierarchyAndGuardians migration, which created the table empty) gets no state_id claim
        // at login, so every RequireStateId() path — /api/schools, /api/regional-admins, statistics —
        // 403s with "This action can only be performed by a state administrator." Link it here so
        // existing databases self-heal on the next start instead of needing a rebuild.
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

        if (!IsStrongEnough(password))
            throw new InvalidOperationException(
                $"SuperAdmin:Password must be at least {MinimumPasswordLength} characters and " +
                "contain a lowercase letter, an uppercase letter and a digit. " +
                "This account owns every school on the platform.");

        var state = await GetOrAddStateAsync(db, stateName);

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
