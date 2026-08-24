using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services;

// Ensures exactly one SuperAdmin User exists. Runs once at startup -- idempotent, so it's
// safe to call on every boot.
public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
            return;

        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "SuperAdmin:Email and SuperAdmin:Password must be configured to seed the initial super admin.");

        db.Users.Add(new User
        {
            FirstName = "Super",
            LastName = "Admin",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            MustChangePassword = true,
            Role = UserRole.SuperAdmin
        });

        await db.SaveChangesAsync();
    }
}
