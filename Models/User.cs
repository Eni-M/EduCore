using EduCore.Enums;
using Microsoft.AspNetCore.Identity;

namespace EduCore.Models;

public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public bool MustChangePassword { get; set; } = true;
    public UserRole Role { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    public void SetPassword(string password, IPasswordHasher<User> hasher) =>
        PasswordHash = hasher.HashPassword(this, password);
}
