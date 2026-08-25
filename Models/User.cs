using EduCore.Enums;
using Microsoft.AspNetCore.Identity;

namespace EduCore.Models;

public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;

    // Private setter so nothing outside this class can put a plaintext password here.
    // Use SetPassword instead. EF Core reads and writes private setters normally.
    public string PasswordHash { get; private set; } = null!;

    public bool MustChangePassword { get; set; } = true;
    public UserRole Role { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    // Deliberately does not touch MustChangePassword: setting a new password means something
    // different depending on who did it, so the caller decides.
    public void SetPassword(string password, IPasswordHasher<User> hasher) =>
        PasswordHash = hasher.HashPassword(this, password);
}
