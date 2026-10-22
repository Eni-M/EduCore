using EduCore.Exceptions;
using EduCore.Security;

namespace EduCore.Endpoints;

// Revocation gate: a token stays valid for an hour, so deactivation has to be re-checked per
// request. Reads IAccountStatusCache, not the database.
//
// Deliberately does NOT enforce MustChangePassword -- /api/auth/me and the password-change
// endpoint use this filter, and a user mid-rotation must still be able to get out of that state.
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

        if (!status.Admits(context.HttpContext.User))
            throw new UnauthorizedException("Your session has ended. Please sign in again.");

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
