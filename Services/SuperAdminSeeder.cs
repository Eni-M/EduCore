using EduCore.Data;
using EduCore.Enums;
using EduCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        if (await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
            return;

        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "SuperAdmin:Email and SuperAdmin:Password must be configured to seed the initial super admin.");

        // Goes through the same path as every other account, so hashing and the
        // must-change-password default cannot drift from the rest of the application.
        await users.BuildLoginAsync("Super", "Admin", email, password, UserRole.SuperAdmin, CancellationToken.None);

        await db.SaveChangesAsync();
    }
}
