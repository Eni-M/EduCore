using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;

namespace EduCore.Endpoints;

// Revocation gate. A JWT is valid for an hour, so deactivating an account does nothing on
// its own -- the token keeps verifying until it expires. This re-reads the account on every
// request so the deactivation takes effect immediately.
//
// Deliberately does NOT enforce MustChangePassword: this is what /api/auth/me and the
// password-change endpoint use, and a user who is mid-rotation must still be able to read
// their own profile and set a new password, or they can never get out of that state.
public class ActiveAccountEndpointFilter(IUserRepository users, ICurrentUser currentUser)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        await RequireActiveAsync(users, currentUser, context);

        return await next(context);
    }

    internal static async Task<User> RequireActiveAsync(
        IUserRepository users, ICurrentUser currentUser, EndpointFilterInvocationContext context)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, context.HttpContext.RequestAborted);

        if (user is null || user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        return user;
    }
}

// The revocation gate plus the password-rotation gate, for the application proper: an account
// that owes a password change may not touch any data until it has changed one.
public class AccountStatusEndpointFilter(IUserRepository users, ICurrentUser currentUser)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = await ActiveAccountEndpointFilter.RequireActiveAsync(users, currentUser, context);

        if (user.MustChangePassword)
            throw new ForbiddenException("You must change your password before using the application.");

        return await next(context);
    }
}
