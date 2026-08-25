using EduCore.Exceptions;
using EduCore.Security;

namespace EduCore.Endpoints;

// A user whose password was reset by an admin holds a valid token, but the only thing it may
// do is set a new password. That endpoint lives outside the group this filter is added to.
public class PasswordChangeEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.User.FindFirst(ClaimNames.MustChangePassword)?.Value == "true")
            throw new ForbiddenException("You must change your password before using the application.");

        return await next(context);
    }
}
