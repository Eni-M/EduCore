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

        // Anonymous by necessity — someone who cannot sign in is the only caller. Always 204,
        // whether or not the address matches an account: a 404 here would let anyone test
        // which email addresses hold accounts.
        group.MapPost("/forgot-password", async (
                ForgotPasswordDto dto, IPasswordResetService service, CancellationToken ct) =>
            {
                await service.RequestAsync(dto, ct);
                return Results.NoContent();
            })
            .RequireRateLimiting("PasswordResetPolicy");

        group.MapPost("/reset-password", async (
                ResetPasswordDto dto, IPasswordResetService service, CancellationToken ct) =>
            {
                await service.ResetAsync(dto, ct);
                return Results.NoContent();
            })
            .RequireRateLimiting("PasswordResetPolicy");

        group.MapPost("/logout", (ISessionCookies cookies, HttpResponse response) =>
        {
            cookies.Clear(response);
            return Results.NoContent();
        });

        // These three sit outside the /api group, so they do not inherit its account-status
        // filter and need the revocation check attached explicitly -- otherwise a deactivated
        // user's /me kept answering 200 for up to an hour while every data route said 401.
        // ActiveAccountEndpointFilter, not AccountStatusEndpointFilter: the password-rotation
        // gate would lock a must-change-password user out of the very endpoints that let them
        // finish rotating.
        group.MapGet("/me", async (IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Ok(await users.GetCurrentAsync(currentUser.UserId, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

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
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();


        // Turning scheduled mail off is a self-service action, deliberately available to every
        // signed-in role rather than only guardians -- teachers and students receive the daily
        // schedule digest and have the same claim on being left alone.
        group.MapPut("/me/email-digests", async (
                DigestPreferenceDto dto, IUserService users, CancellationToken ct) =>
            {
                await users.SetDigestPreferenceAsync(dto.OptOut, ct);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();
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
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

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
