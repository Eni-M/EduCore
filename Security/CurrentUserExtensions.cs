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
}
