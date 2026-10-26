using EduCore.Data;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Email;
using EduCore.Services.Interfaces;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EduCore.Services.Account;

public record PasskeyDto(Guid Id, string Name, DateTime CreatedAt, DateTime? LastUsedAt);

public interface IPasskeyService
{
    Task<bool> HasPasskeysAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<PasskeyDto>> ListAsync(Guid userId, CancellationToken ct);

    // Registration, for a signed-in user who has already re-entered their password.
    Task<CredentialCreateOptions> BeginRegistrationAsync(User user, CancellationToken ct);
    Task<PasskeyDto> CompleteRegistrationAsync(User user, string name, AuthenticatorAttestationRawResponse response, CancellationToken ct);
    Task RemoveAsync(User user, Guid passkeyId, CancellationToken ct);

    // Removes every passkey on the account, staged on the shared context; the caller saves.
    Task StageRemoveAllAsync(Guid userId, CancellationToken ct);

    // The second step of a sign-in, answered with a passkey instead of a code.
    Task<AssertionOptions> BeginAssertionAsync(Guid challengeId, CancellationToken ct);
    Task<User> CompleteAssertionAsync(Guid challengeId, AuthenticatorAssertionRawResponse response, CancellationToken ct);
}

// Passkeys through Fido2NetLib, which does the WebAuthn verification: origin and RP id binding,
// signature, counter. The options handed to the browser are kept server-side for five minutes, so
// a response can only answer the challenge this server issued.
public class PasskeyService(
    IFido2 fido2,
    AppDbContext db,
    ITwoFactorChallengeRepository challenges,
    IAccountMailQueue mail,
    IAuditTrail audit,
    IMemoryCache cache) : IPasskeyService
{
    private static readonly TimeSpan OptionsLifetime = TimeSpan.FromMinutes(5);
    private const int MaxAttempts = 5;

    public async Task<bool> HasPasskeysAsync(Guid userId, CancellationToken ct) =>
        await db.Passkeys.AnyAsync(p => p.UserId == userId, ct);

    public async Task<IReadOnlyList<PasskeyDto>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.Passkeys.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PasskeyDto(p.Id, p.Name, p.CreatedAt, p.LastUsedAt))
            .ToListAsync(ct);

    public async Task<CredentialCreateOptions> BeginRegistrationAsync(User user, CancellationToken ct)
    {
        var existing = await db.Passkeys.Where(p => p.UserId == user.Id).Select(p => p.CredentialId).ToListAsync(ct);

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = user.Id.ToByteArray(),
                Name = user.Email,
                DisplayName = $"{user.FirstName} {user.LastName}",
            },
            // The same device cannot be registered twice.
            ExcludeCredentials = existing.Select(id => new PublicKeyCredentialDescriptor(id)).ToList(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Preferred,
                UserVerification = UserVerificationRequirement.Preferred,
            },
            // No attestation: which brand of authenticator someone uses is not this school's business.
            AttestationPreference = AttestationConveyancePreference.None,
        });

        cache.Set(RegistrationKey(user.Id), options.ToJson(), OptionsLifetime);
        return options;
    }

    public async Task<PasskeyDto> CompleteRegistrationAsync(
        User user, string name, AuthenticatorAttestationRawResponse response, CancellationToken ct)
    {
        if (!cache.TryGetValue(RegistrationKey(user.Id), out string? json) || json is null)
            throw new BusinessException("Start adding the passkey again; the request expired.");
        cache.Remove(RegistrationKey(user.Id));

        RegisteredPublicKeyCredential credential;
        try
        {
            credential = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = response,
                OriginalOptions = CredentialCreateOptions.FromJson(json),
                IsCredentialIdUniqueToUserCallback = async (args, token) =>
                    !await db.Passkeys.AnyAsync(p => p.CredentialId == args.CredentialId, token),
            }, ct);
        }
        catch (Fido2VerificationException)
        {
            throw new BusinessException("That passkey could not be verified. Try again.");
        }

        var passkey = new PasskeyCredential
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            CredentialId = credential.Id,
            PublicKey = credential.PublicKey,
            SignCount = credential.SignCount,
            Name = string.IsNullOrWhiteSpace(name) ? "Passkey" : name.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        db.Passkeys.Add(passkey);
        await db.SaveChangesAsync(ct);

        mail.Enqueue(new AccountMailRequest(AccountMailKind.PasskeyAdded, user.Email, user.FirstName));
        return new PasskeyDto(passkey.Id, passkey.Name, passkey.CreatedAt, null);
    }

    public async Task RemoveAsync(User user, Guid passkeyId, CancellationToken ct)
    {
        var passkey = await db.Passkeys.FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == user.Id, ct)
                      ?? throw new NotFoundException("Passkey", passkeyId);

        db.Passkeys.Remove(passkey);
        await db.SaveChangesAsync(ct);

        mail.Enqueue(new AccountMailRequest(AccountMailKind.PasskeyRemoved, user.Email, user.FirstName));
    }

    public async Task StageRemoveAllAsync(Guid userId, CancellationToken ct) =>
        db.Passkeys.RemoveRange(await db.Passkeys.Where(p => p.UserId == userId).ToListAsync(ct));

    public async Task<AssertionOptions> BeginAssertionAsync(Guid challengeId, CancellationToken ct)
    {
        var challenge = await OpenSignInChallengeAsync(challengeId, ct);
        var credentials = await db.Passkeys.Where(p => p.UserId == challenge.UserId).Select(p => p.CredentialId).ToListAsync(ct);

        if (credentials.Count == 0)
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = credentials.Select(id => new PublicKeyCredentialDescriptor(id)).ToList(),
            UserVerification = UserVerificationRequirement.Preferred,
        });

        cache.Set(AssertionKey(challengeId), options.ToJson(), OptionsLifetime);
        return options;
    }

    public async Task<User> CompleteAssertionAsync(
        Guid challengeId, AuthenticatorAssertionRawResponse response, CancellationToken ct)
    {
        var challenge = await OpenSignInChallengeAsync(challengeId, ct);
        var now = DateTime.UtcNow;

        if (!cache.TryGetValue(AssertionKey(challengeId), out string? json) || json is null)
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");
        cache.Remove(AssertionKey(challengeId));

        // Charged in one UPDATE before verifying, as codes are, so concurrent tries are all counted.
        if (!await challenges.TryTakeAttemptAsync(challengeId, MaxAttempts, now, ct))
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        var passkey = await db.Passkeys.FirstOrDefaultAsync(
            p => p.UserId == challenge.UserId && p.CredentialId == response.RawId, ct);

        try
        {
            if (passkey is null)
                throw new Fido2VerificationException("Unknown credential.");

            var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = response,
                OriginalOptions = AssertionOptions.FromJson(json),
                StoredPublicKey = passkey.PublicKey,
                StoredSignatureCounter = passkey.SignCount,
                IsUserHandleOwnerOfCredentialIdCallback = (args, _) =>
                    Task.FromResult(args.UserHandle is null || new Guid(args.UserHandle) == passkey.UserId),
            }, ct);

            passkey.SignCount = result.SignCount;
            passkey.LastUsedAt = now;
        }
        catch (Fido2VerificationException)
        {
            await audit.RecordAsync("Passkey sign-in rejected", ct, userId: challenge.UserId);
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");
        }

        // Claimed in one UPDATE too, so of two good responses only one signs in.
        if (!await challenges.TryCompleteAsync(challengeId, now, ct))
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        await db.SaveChangesAsync(ct);

        return challenge.User;
    }

    // One message for every failure, as with codes: which of unknown, expired, used or
    // wrong-purpose applied is not the caller's business.
    private async Task<TwoFactorChallenge> OpenSignInChallengeAsync(Guid challengeId, CancellationToken ct)
    {
        var challenge = await challenges.GetOpenAsync(challengeId, ct);

        if (challenge is null || challenge.Purpose != TwoFactorPurpose.SignIn
                              || !challenge.IsOpen(DateTime.UtcNow, MaxAttempts))
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        return challenge;
    }

    private static string RegistrationKey(Guid userId) => $"passkey-registration:{userId}";
    private static string AssertionKey(Guid challengeId) => $"passkey-assertion:{challengeId}";
}
