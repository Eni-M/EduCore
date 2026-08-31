using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Security;

namespace EduCore.Endpoints;

public class AccountStatusEndpointFilter(IUserRepository users, ICurrentUser currentUser)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, context.HttpContext.RequestAborted);

        if (user is null || user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        if (user.MustChangePassword)
            throw new ForbiddenException("You must change your password before using the application.");

        return await next(context);
    }
}
