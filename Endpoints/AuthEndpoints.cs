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

        group.MapPost("/login", async (
            LoginUserDto dto,
            IUserService users,
            ISessionCookies cookies,
            HttpResponse response,
            CancellationToken ct) =>
        {
            var result = await users.LoginAsync(dto, ct);
            cookies.Issue(response, result.Token, result.User.ExpiresAt);
            return Results.Ok(result.User);
        }).RequireRateLimiting("LoginPolicy");

        group.MapPost("/logout", (ISessionCookies cookies, HttpResponse response) =>
        {
            cookies.Clear(response);
            return Results.NoContent();
        });

        group.MapGet("/me", async (IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Ok(await users.GetCurrentAsync(currentUser.UserId, ct)))
            .RequireAuthorization();

        group.MapPost("/change-password", async (
                ChangePasswordDto dto,
                IUserService users,
                ICurrentUser currentUser,
                ISessionCookies cookies,
                HttpResponse response,
                CancellationToken ct) =>
            {
                var result = await users.ChangePasswordAsync(currentUser.UserId, dto, ct);
                cookies.Issue(response, result.Token, result.User.ExpiresAt);
                return Results.Ok(result.User);
            })
            .RequireAuthorization();

        group.MapPut("/me", async (
                UpdateMyCredentialsDto dto,
                IUserService users,
                ICurrentUser currentUser,
                ISessionCookies cookies,
                HttpResponse response,
                CancellationToken ct) =>
            {
                var result = await users.UpdateOwnCredentialsAsync(
                    currentUser.UserId, dto.FirstName, dto.LastName, dto.Email, ct);
                cookies.Issue(response, result.Token, result.User.ExpiresAt);
                return Results.Ok(result.User);
            })
            .RequireAuthorization();

        var accounts = group.MapGroup("/users")
            .RequireAuthorization(Policies.ManageAccounts)
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
