using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Security;

namespace EduCore.Endpoints;

// Checks the account behind the token, on every request, before the endpoint runs.
//
// A token proves it was issued by us and has not expired. It cannot prove the
// account still exists, is still active, or is not sitting on a password an
// administrator just reset -- those are facts about a database row, and the row
// can change after the token is handed out.
//
// This replaces PasswordChangeEndpointFilter, which asked the token instead of
// the row and so had two holes:
//
//   Deactivating an account did nothing until its token expired -- up to an hour
//   of full access for someone who had just been removed. And that hour did not
//   have to end: /api/auth/change-password hands back a fresh token, so a
//   deactivated user could renew before each expiry and keep going for ever.
//   (UserService.ChangePasswordAsync now refuses them too, which closes the
//   renewal; this closes the hour.)
//
//   Resetting a compromised account's password wrote MustChangePassword to the
//   row, but the old filter read the copy baked into the token when it was
//   issued -- so the intruder's existing session carried on unaffected. Reading
//   the row means the reset bites on their very next request.
//
// The cost is one lookup by primary key per request. That is the price of being
// able to revoke at all: the alternative is a token nobody can withdraw once it
// is out.
// Both dependencies arrive through the constructor: AddEndpointFilter<T> builds
// the filter once per request from that request's services, so a scoped service
// is exactly what it can supply. ValidationEndpointFilter next door does the
// same thing.
public class AccountStatusEndpointFilter(IUserRepository users, ICurrentUser currentUser)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, context.HttpContext.RequestAborted);

        // Deleted or switched off. 401 rather than 403, because the right answer
        // is "sign in again" -- and the login endpoint will say why.
        if (user is null || user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        // A user whose password was reset by an admin holds a valid token, but
        // the only thing it may do is set a new password -- and that endpoint
        // lives outside the group this filter is added to.
        if (user.MustChangePassword)
            throw new ForbiddenException("You must change your password before using the application.");

        return await next(context);
    }
}
