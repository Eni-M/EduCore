using System.Security.Claims;
using EduCore.Dtos.User;
using EduCore.Security;
using EduCore.Services.Account;
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

            // A second step is owed: no cookie yet, just the handle to answer with. The code
            // is in the account's mailbox and never in this response.
            if (result.Session is null)
                return Results.Ok(new TwoFactorRequiredDto(
                    result.TwoFactorChallengeId!.Value, Authenticator: result.ViaAuthenticator, Passkey: result.Passkey));

            cookies.Issue(response, result.Session.Token, result.Session.User.ExpiresAt);
            return Results.Ok(result.Session.User);
        }).RequireRateLimiting("LoginPolicy");

        // Anonymous: the caller has proved the password but holds no session yet, which is the
        // whole point of the step. Rate-limited like login, because this is where a six-digit
        // code would be guessed at if anywhere.
        group.MapPost("/login/verify", async (
            VerifyTwoFactorDto dto,
            IUserService users,
            ISessionCookies cookies,
            HttpResponse response,
            CancellationToken ct) =>
        {
            var result = await users.CompleteSignInAsync(dto, ct);
            cookies.Issue(response, result.Token, result.User.ExpiresAt);
            return TypedResults.Ok(result.User);
        }).RequireRateLimiting("LoginPolicy");

        // Anonymous by necessity — someone who cannot sign in is the only caller. Always 204,
        // whether or not the address matches an account: a 404 here would let anyone test
        // which email addresses hold accounts.
        group.MapPost("/forgot-password", async (
                ForgotPasswordDto dto, IPasswordResetService service, CancellationToken ct) =>
            {
                await service.RequestAsync(dto, ct);
                return TypedResults.NoContent();
            })
            .RequireRateLimiting("PasswordResetPolicy");

        group.MapPost("/reset-password", async (
                ResetPasswordDto dto, IPasswordResetService service, CancellationToken ct) =>
            {
                await service.ResetAsync(dto, ct);
                return TypedResults.NoContent();
            })
            .RequireRateLimiting("PasswordResetPolicy");

        // Ends the account's sessions on the server too, not just this browser's cookie: a copy
        // of the cookie taken earlier stops working with it. That means signing out signs the
        // account out on every device. Still answers 204 without a valid session, so a browser
        // holding an expired cookie can always clear it.
        // Only a token that still passes the revocation gate may end sessions: otherwise a copy
        // that has already been revoked could keep signing the real owner out of every new one.
        group.MapPost("/logout", async (
            ISessionCookies cookies, HttpContext http, IUserService users, IAccountStatusCache accounts,
            CancellationToken ct) =>
        {
            if (Guid.TryParse(http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
                && (await accounts.GetAsync(userId, ct)).Admits(http.User))
                await users.EndSessionsAsync(userId, ct);

            cookies.Clear(http.Response);
            return TypedResults.NoContent();
        });

        // These three sit outside the /api group, so they do not inherit its account-status
        // filter and need the revocation check attached explicitly -- otherwise a deactivated
        // user's /me kept answering 200 for up to an hour while every data route said 401.
        // ActiveAccountEndpointFilter, not AccountStatusEndpointFilter: the password-rotation
        // gate would lock a must-change-password user out of the very endpoints that let them
        // finish rotating.
        group.MapGet("/me", async (IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                TypedResults.Ok(await users.GetCurrentAsync(currentUser.UserId, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        // Sends the code that change-password will demand. Separate from the change itself so
        // the screen can ask for the code before the user has typed a new password.
        group.MapPost("/change-password/request-code", async (
                IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
            {
                var challengeId = await users.RequestPasswordChangeCodeAsync(currentUser.UserId, ct);
                return TypedResults.Ok(new TwoFactorRequiredDto(challengeId));
            })
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>()
            .RequireRateLimiting("PasswordResetPolicy");

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
                return TypedResults.Ok(result.User);
            })
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();


        group.MapGet("/me/two-factor", async (
                IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                TypedResults.Ok(await users.GetTwoFactorStatusAsync(currentUser.UserId, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        // Opt in or out. The service refuses to switch it off for the roles that manage
        // accounts, so this is only ever additive for them.
        group.MapPut("/me/two-factor", async (
                TwoFactorPreferenceDto dto, IUserService users, ICurrentUser currentUser,
                CancellationToken ct) =>
                TypedResults.Ok(await users.SetTwoFactorAsync(currentUser.UserId, dto.Enabled, dto.CurrentPassword, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        // Passkeys as the second step of a sign-in: the options for the browser's prompt, then the
        // signed response. Anonymous like /login/verify -- the challenge id is the handle.
        group.MapPost("/login/passkey/options", async (
                PasskeyChallengeDto dto, IPasskeyService passkeys, CancellationToken ct) =>
                Results.Text((await passkeys.BeginAssertionAsync(dto.ChallengeId, ct)).ToJson(), "application/json"))
            .RequireRateLimiting("LoginPolicy");

        group.MapPost("/login/passkey", async (
                PasskeySignInDto dto, IUserService users, ISessionCookies cookies, HttpResponse response,
                CancellationToken ct) =>
            {
                var result = await users.CompleteSignInWithPasskeyAsync(dto.ChallengeId, dto.Response, ct);
                cookies.Issue(response, result.Token, result.User.ExpiresAt);
                return TypedResults.Ok(result.User);
            })
            .RequireRateLimiting("LoginPolicy");

        // Managing your own passkeys: list, add (password first), remove (password).
        group.MapGet("/me/passkeys", async (IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                TypedResults.Ok(await users.ListPasskeysAsync(currentUser.UserId, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        group.MapPost("/me/passkeys/options", async (
                BeginAuthenticatorDto dto, IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Text((await users.BeginPasskeyRegistrationAsync(currentUser.UserId, dto.CurrentPassword, ct)).ToJson(),
                    "application/json"))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        group.MapPost("/me/passkeys", async (
                PasskeyRegistrationDto dto, IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                TypedResults.Ok(await users.CompletePasskeyRegistrationAsync(currentUser.UserId, dto, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        group.MapPost("/me/passkeys/{passkeyId:guid}/remove", async (
                Guid passkeyId, RemoveAuthenticatorDto dto, IUserService users, ICurrentUser currentUser,
                CancellationToken ct) =>
            {
                await users.RemovePasskeyAsync(currentUser.UserId, passkeyId, dto.CurrentPassword, ct);
                return TypedResults.NoContent();
            })
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        // An authenticator app: begin (returns the secret to add to the app), confirm with a first
        // code, or remove (which needs the password, like switching two-step sign-in off).
        group.MapPost("/me/authenticator", async (
                BeginAuthenticatorDto dto, IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                TypedResults.Ok(await users.BeginAuthenticatorSetupAsync(currentUser.UserId, dto.CurrentPassword, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        group.MapPost("/me/authenticator/confirm", async (
                AuthenticatorCodeDto dto, IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                TypedResults.Ok(await users.ConfirmAuthenticatorAsync(currentUser.UserId, dto.Code, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>()
            .RequireRateLimiting("LoginPolicy");

        group.MapPost("/me/authenticator/remove", async (
                RemoveAuthenticatorDto dto, IUserService users, ICurrentUser currentUser, CancellationToken ct) =>
                TypedResults.Ok(await users.RemoveAuthenticatorAsync(currentUser.UserId, dto.CurrentPassword, ct)))
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        // Turning scheduled mail off is a self-service action, deliberately available to every
        // signed-in role rather than only guardians -- teachers and students receive the daily
        // schedule digest and have the same claim on being left alone.
        group.MapPut("/me/email-digests", async (
                DigestPreferenceDto dto, IUserService users, CancellationToken ct) =>
            {
                await users.SetDigestPreferenceAsync(dto.OptOut, ct);
                return TypedResults.NoContent();
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
                    currentUser.UserId, dto.FirstName, dto.LastName, dto.Email, dto.CurrentPassword, ct);
                cookies.Issue(response, result.Token, result.User.ExpiresAt);
                return TypedResults.Ok(result.User);
            })
            .RequireAuthorization()
            .AddEndpointFilter<ActiveAccountEndpointFilter>();

        var accounts = group.MapGroup("/users")
            .RequireAuthorization(Policies.ManageAccounts)
            .AddEndpointFilter<AccountStatusEndpointFilter>();

        accounts.MapPost("/{userId:guid}/reset-password",
            async (Guid userId, IUserService users, CancellationToken ct) =>
                TypedResults.Ok(await users.AdminResetPasswordAsync(userId, ct)));

        accounts.MapPost("/{userId:guid}/deactivate", async (Guid userId, IUserService users, CancellationToken ct) =>
        {
            await users.SetActiveAsync(userId, false, ct);
            return TypedResults.NoContent();
        });

        accounts.MapPost("/{userId:guid}/activate", async (Guid userId, IUserService users, CancellationToken ct) =>
        {
            await users.SetActiveAsync(userId, true, ct);
            return TypedResults.NoContent();
        });
    }
}
