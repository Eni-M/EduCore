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

    // Opt-out from every scheduled digest -- the daily schedule mail, the Sunday week-ahead, and
    // the guardian weekly summary. One flag rather than one per digest: a person who does not
    // want scheduled mail does not want it broken down by kind, and a per-digest matrix is a
    // settings screen nobody will build. Default false, so digests are opt-out, not opt-in --
    // they are the reason most guardians have an account at all.
    public bool EmailDigestsOptOut { get; set; }

    // Opt-in second step at sign-in. The roles that manage accounts get it whether or not this
    // is set -- see TwoFactorOptions.RequiredForRoles -- so this flag only ever adds it.
    public bool TwoFactorEnabled { get; set; }
    public UserRole Role { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    public void SetPassword(string password, IPasswordHasher<User> hasher) =>
        PasswordHash = hasher.HashPassword(this, password);
}
