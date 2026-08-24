using EduCore.Enums;

namespace EduCore.Models;

public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool MustChangePassword { get; set; } = true;
    public UserRole Role { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}
