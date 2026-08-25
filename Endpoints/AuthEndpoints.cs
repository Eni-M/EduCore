using EduCore.Dtos.User;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class AuthEndpoints
{
    // Not part of the /api group: login is anonymous, and change-password has to stay
    // reachable while the must-change-password flag is set.
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .AddEndpointFilter<ValidationEndpointFilter>()
            .WithTags("Auth");

        group.MapPost("/login", async (LoginUserDto dto, IUserService users, CancellationToken ct) =>
            Results.Ok(await users.LoginAsync(dto, ct)));

        group.MapPost("/change-password",
                async (ChangePasswordDto dto, IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                {
                    await users.ChangePasswordAsync(currentUser.UserId, dto, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization();

        // The temporary password is returned once for the admin to hand over, never stored.
        // Managing other people's accounts is not something you may do on a token that is
        // still pending its own password change.
        var accounts = group.MapGroup("/users")
            .RequireAuthorization(Policies.AdminOrSuperAdmin)
            .AddEndpointFilter<PasswordChangeEndpointFilter>();

        accounts.MapPost("/{userId:guid}/reset-password",
            async (Guid userId, IUserService users, CancellationToken ct) =>
                Results.Ok(await users.AdminResetPasswordAsync(userId, ct)));

        accounts.MapPost("/{userId:guid}/deactivate", async (Guid userId, IUserService users, CancellationToken ct) =>
        {
            await users.SetActiveAsync(userId, false, ct);
            return Results.NoContent();
        });

        accounts.MapPost("/{userId:guid}/activate", async (Guid userId, IUserService users, CancellationToken ct) =>
        {
            await users.SetActiveAsync(userId, true, ct);
            return Results.NoContent();
        });
    }
}
