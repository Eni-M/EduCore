using EduCore.Exceptions;

namespace EduCore.Security;

public static class CurrentUserExtensions
{
    public static Guid RequireSchoolId(this ICurrentUser user) =>
        user.SchoolId
        ?? throw new ForbiddenException("This action can only be performed by a user that belongs to a school.");

    public static Guid RequireTeacherId(this ICurrentUser user) =>
        user.TeacherId
        ?? throw new ForbiddenException("This action can only be performed by a teacher.");

    public static Guid RequireStudentId(this ICurrentUser user) =>
        user.StudentId
        ?? throw new ForbiddenException("This action can only be performed by a student.");

    // Who may see the sign-in behind a person -- their user id and whether the
    // account is deactivated -- and therefore who may act on it through
    // /api/auth/users/{userId}/{activate,deactivate,reset-password}.
    //
    // The same two roles those endpoints already accept, named once here so a
    // list cannot disagree with the endpoint it is meant to feed. It is only ever
    // used to decide what to show: whether this particular administrator may act
    // on this particular account is still EnsureCanManageAsync's answer, and the
    // API asks it again on the write.
    public static bool CanManageAccounts(this ICurrentUser user) =>
        user.IsSuperAdmin || user.IsAdmin;
}
