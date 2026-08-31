using EduCore.Dtos.User;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class AuthEndpoints
{
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

        group.MapPut("/me",
                async (UpdateMyCredentialsDto dto, IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                    Results.Ok(await users.UpdateOwnCredentialsAsync(
                        currentUser.UserId, dto.FirstName, dto.LastName, dto.Email, ct)))
            .RequireAuthorization();

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
