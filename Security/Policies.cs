using EduCore.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EduCore.Security;

public static class Policies
{
    public const string StateAdmin = nameof(StateAdmin);
    public const string RegionalAdmin = nameof(RegionalAdmin);
    public const string Directorate = nameof(Directorate);
    public const string Teacher = nameof(Teacher);
    public const string Guardian = nameof(Guardian);

    public const string RegionalOrState = nameof(RegionalOrState);
    public const string DirectorateOrTeacher = nameof(DirectorateOrTeacher);
    public const string ManageAccounts = nameof(ManageAccounts);
    public const string StatisticsViewer = nameof(StatisticsViewer);
    public const string AnySchoolUser = nameof(AnySchoolUser);
    public const string StudentReadable = nameof(StudentReadable);

    public static void AddEduCorePolicies(this AuthorizationOptions options)
    {
        void Policy(string name, params UserRole[] roles) =>
            options.AddPolicy(name, p => p.RequireRole(roles.Select(r => r.ToString()).ToArray()));

        Policy(StateAdmin, UserRole.StateAdmin);
        Policy(RegionalAdmin, UserRole.RegionalAdmin);
        Policy(Directorate, UserRole.Directorate);
        Policy(Teacher, UserRole.Teacher);
        Policy(Guardian, UserRole.Guardian);

        Policy(RegionalOrState, UserRole.RegionalAdmin, UserRole.StateAdmin);
        Policy(DirectorateOrTeacher, UserRole.Directorate, UserRole.Teacher);
        Policy(ManageAccounts, UserRole.Directorate, UserRole.RegionalAdmin, UserRole.StateAdmin);
        Policy(StatisticsViewer, UserRole.Directorate, UserRole.RegionalAdmin, UserRole.StateAdmin);
        Policy(AnySchoolUser, UserRole.Directorate, UserRole.Teacher, UserRole.Student);
        Policy(StudentReadable, UserRole.Directorate, UserRole.Teacher, UserRole.Student, UserRole.Guardian);
    }
}
