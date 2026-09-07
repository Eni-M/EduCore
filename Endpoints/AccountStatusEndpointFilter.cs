using EduCore.Exceptions;
using EduCore.Security;

namespace EduCore.Endpoints;

// Revocation gate. A JWT is valid for an hour, so deactivating an account does nothing on
// its own -- the token keeps verifying until it expires. This re-checks the account on every
// request so the deactivation takes effect immediately.
//
// The check reads IAccountStatusCache rather than the database directly. It used to be one
// SELECT per request against Users, which made it the highest-frequency query in the whole
// application; the cache is evicted by the write that deactivates an account, so revocation is
// still immediate rather than merely eventual. See AccountStatusCache.
//
// Deliberately does NOT enforce MustChangePassword: this is what /api/auth/me and the
// password-change endpoint use, and a user who is mid-rotation must still be able to read
// their own profile and set a new password, or they can never get out of that state.
public class ActiveAccountEndpointFilter(IAccountStatusCache accounts, ICurrentUser currentUser)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        await RequireActiveAsync(accounts, currentUser, context);

        return await next(context);
    }

    internal static async Task<AccountStatus> RequireActiveAsync(
        IAccountStatusCache accounts, ICurrentUser currentUser, EndpointFilterInvocationContext context)
    {
        var status = await accounts.GetAsync(currentUser.UserId, context.HttpContext.RequestAborted);

        if (!status.Exists || status.IsDeactivated)
            throw new UnauthorizedException("This account has been deactivated.");

        return status;
    }
}

// The revocation gate plus the password-rotation gate, for the application proper: an account
// that owes a password change may not touch any data until it has changed one.
public class AccountStatusEndpointFilter(IAccountStatusCache accounts, ICurrentUser currentUser)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var status = await ActiveAccountEndpointFilter.RequireActiveAsync(accounts, currentUser, context);

        if (status.MustChangePassword)
            throw new ForbiddenException("You must change your password before using the application.");

        return await next(context);
    }
}
