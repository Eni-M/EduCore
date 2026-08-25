using EduCore.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EduCore.Security;

public static class Policies
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string Admin = nameof(Admin);
    public const string Teacher = nameof(Teacher);
    public const string AdminOrSuperAdmin = nameof(AdminOrSuperAdmin);
    public const string AdminOrTeacher = nameof(AdminOrTeacher);
    public const string AnySchoolUser = nameof(AnySchoolUser);

    public static void AddEduCorePolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(SuperAdmin, p => p.RequireRole(Role(UserRole.SuperAdmin)));
        options.AddPolicy(Admin, p => p.RequireRole(Role(UserRole.Admin)));
        options.AddPolicy(Teacher, p => p.RequireRole(Role(UserRole.Teacher)));

        options.AddPolicy(AdminOrSuperAdmin, p =>
            p.RequireRole(Role(UserRole.Admin), Role(UserRole.SuperAdmin)));

        options.AddPolicy(AdminOrTeacher, p =>
            p.RequireRole(Role(UserRole.Admin), Role(UserRole.Teacher)));

        options.AddPolicy(AnySchoolUser, p =>
            p.RequireRole(Role(UserRole.Admin), Role(UserRole.Teacher), Role(UserRole.Student)));
    }

    private static string Role(UserRole role) => role.ToString();
}
