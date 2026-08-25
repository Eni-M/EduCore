using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Services;

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

        var user = new User
        {
            FirstName = "Super",
            LastName = "Admin",
            Email = email,
            MustChangePassword = true,
            Role = UserRole.SuperAdmin
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        db.Users.Add(user);

        await db.SaveChangesAsync();
    }
}
