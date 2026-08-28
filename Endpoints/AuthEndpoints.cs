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
                    Results.Ok(await users.ChangePasswordAsync(currentUser.UserId, dto, ct)))
            .RequireAuthorization();

        // A person's own name and email -- reachable by anyone signed in, unlike
        // the per-role edit routes (PUT /api/students/{id} and its siblings),
        // which are how an administrator corrects someone else's record and
        // carry fields, like a student's city, that are not this endpoint's
        // business. Kept alongside change-password rather than in /api because
        // it must stay reachable while must-change-password is still set --
        // someone forced to a temporary password may still have a typo in the
        // name it was created under.
        group.MapPut("/me",
                async (UpdateMyCredentialsDto dto, IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                    Results.Ok(await users.UpdateOwnCredentialsAsync(
                        currentUser.UserId, dto.FirstName, dto.LastName, dto.Email, ct)))
            .RequireAuthorization();

        // The temporary password is returned once for the admin to hand over, never stored.
        // Managing other people's accounts is not something you may do on a token that is
        // still pending its own password change.
        var accounts = group.MapGroup("/users")
            .RequireAuthorization(Policies.AdminOrSuperAdmin)
            .AddEndpointFilter<AccountStatusEndpointFilter>();

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
