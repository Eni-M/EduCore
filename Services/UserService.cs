using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduCore.Dtos.User;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Account;
using EduCore.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PasswordGenerator;

namespace EduCore.Services;

public record ProfileScope(
    IReadOnlyList<Guid> SchoolIds,
    Guid? TeacherId,
    Guid? StudentId,
    Guid? CityId,
    Guid? StateId,
    Guid? GuardianId)
{
    public static readonly ProfileScope Empty = new([], null, null, null, null, null);

    // The single school this profile is pinned to, or null when it spans zero or several.
    public Guid? SchoolId => SchoolIds.Count == 1 ? SchoolIds[0] : null;
}

public class UserService(
    IUserRepository userRepository,
    ITeacherRepository teacherRepository,
    IStudentRepository studentRepository,
    IDirectorateRepository directorateRepository,
    IRegionalAdminRepository regionalAdminRepository,
    IStateAdminRepository stateAdminRepository,
    IGuardianRepository guardianRepository,
    ISchoolRepository schoolRepository,
    IPasswordHasher<User> passwordHasher,
    ITwoFactorService twoFactor,
    IOptions<TwoFactorOptions> twoFactorOptions,
    ICurrentUser currentUser,
    IConfiguration config) : IUserService
{
    public async Task<LoginResultDto> LoginAsync(LoginUserDto dto, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(dto.Email, ct);

        if (user is null)
            throw new UnauthorizedException("Invalid email or password.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid email or password.");

        if (user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPassword(dto.Password, passwordHasher);
            userRepository.Update(user);
            await userRepository.SaveChangesAsync(ct);
        }

        // The password was right, but for these accounts it is not sufficient on its own. No
        // session cookie is issued here -- the caller gets a handle and has to prove they can
        // read the mailbox before anything is signed in.
        if (twoFactor.SignInRequiresSecondStep(user))
            return LoginResultDto.NeedsSecondStep(
                await twoFactor.IssueAsync(user, TwoFactorPurpose.SignIn, ct));

        return LoginResultDto.SignedIn(await BuildAuthResponseAsync(user, ct));
    }

    public async Task<AuthResultDto> CompleteSignInAsync(VerifyTwoFactorDto dto, CancellationToken ct)
    {
        var user = await twoFactor.CompleteAsync(
            dto.ChallengeId, dto.Code, TwoFactorPurpose.SignIn, ct);

        // Re-checked here, not just at the first step: an account can be deactivated in the
        // minutes between the password and the code.
        if (user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<Guid> RequestPasswordChangeCodeAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        if (user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        return await twoFactor.IssueAsync(user, TwoFactorPurpose.PasswordChange, ct);
    }

    public async Task<TwoFactorStatusDto> GetTwoFactorStatusAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        var required = twoFactorOptions.Value.IsRequiredFor(user.Role);
        return new TwoFactorStatusDto(required || user.TwoFactorEnabled, required);
    }

    public async Task<TwoFactorStatusDto> SetTwoFactorAsync(
        Guid userId, bool enabled, string? currentPassword, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        // Switching the second step off weakens the account, so a borrowed session is not enough.
        if (!enabled && user.TwoFactorEnabled)
            RequireCurrentPassword(user, currentPassword);

        // The roles that manage accounts do not get to decline. Refused outright rather than
        // silently ignored, so the screen can say why instead of appearing to have worked.
        if (!enabled && twoFactorOptions.Value.IsRequiredFor(user.Role))
            throw new BusinessException(
                "Two-step sign-in cannot be switched off for this role.");

        user.TwoFactorEnabled = enabled;
        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return await GetTwoFactorStatusAsync(userId, ct);
    }

    public async Task<AuthResultDto> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Current password is incorrect.");

        if (user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        // Emailed code, for every role. Checked after the current password so a caller who does
        // not know it cannot burn somebody else's challenge, and bound to this user so a code
        // issued for one account cannot be spent on another.
        var confirmed = await twoFactor.CompleteAsync(
            dto.ChallengeId, dto.Code, TwoFactorPurpose.PasswordChange, ct);

        if (confirmed.Id != userId)
            throw new UnauthorizedException("That code is not valid. Ask for a new one.");

        user.SetPassword(dto.NewPassword, passwordHasher);
        user.MustChangePassword = false;
        // Every other session ends with the old password; the caller is re-signed in below.
        user.SessionsValidFrom = DateTime.UtcNow;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResultDto> UpdateOwnCredentialsAsync(
        Guid userId, string firstName, string lastName, string email, string? currentPassword,
        CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        if (user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        // The email is where reset links and sign-in codes go. Moving it on the strength of a
        // session alone would turn one stolen cookie into a permanent takeover.
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            RequireCurrentPassword(user, currentPassword);

        await ApplyLoginEditAsync(user, firstName, lastName, email, ct);

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponseDto> GetCurrentAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        if (user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        return (await BuildAuthResponseAsync(user, ct)).User;
    }

    public async Task<ResetPasswordResponseDto> AdminResetPasswordAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        await EnsureCanManageAsync(user, ct);

        var temporaryPassword = new Password(12)
            .IncludeLowercase()
            .IncludeUppercase()
            .IncludeNumeric()
            .IncludeSpecial()
            .Next();

        user.SetPassword(temporaryPassword, passwordHasher);
        user.MustChangePassword = true;
        user.SessionsValidFrom = DateTime.UtcNow;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return new ResetPasswordResponseDto("Password reset successfully.", temporaryPassword);
    }


    public async Task EndSessionsAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return;

        user.SessionsValidFrom = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }

    // Not UnauthorizedException: the client treats any 401 as an expired session and signs out,
    // which is the wrong answer to a mistyped password.
    private void RequireCurrentPassword(User user, string? currentPassword)
    {
        if (string.IsNullOrEmpty(currentPassword)
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword)
            == PasswordVerificationResult.Failed)
            throw new BusinessException("Your current password is incorrect.");
    }

    // The unsubscribe. A digest people cannot turn off is one they report as spam, which costs
    // the sending domain its reputation and takes the password-reset mail down with it.
    public async Task SetDigestPreferenceAsync(bool optOut, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, ct)
                   ?? throw new NotFoundException("User", currentUser.UserId);

        user.EmailDigestsOptOut = optOut;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }
    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        if (!isActive && userId == currentUser.UserId)
            throw new BusinessException("You cannot deactivate your own account.");

        await EnsureCanManageAsync(user, ct);

        user.DeactivatedAt = isActive ? null : DateTime.UtcNow;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }

    public async Task<User> BuildLoginAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        UserRole role,
        CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(email, ct))
            throw new ConflictException($"A user with the email {email} already exists.");

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = role,
            MustChangePassword = true
        };

        user.SetPassword(password, passwordHasher);

        await userRepository.AddAsync(user);
        return user;
    }

    public async Task ApplyLoginEditAsync(
        User user,
        string firstName,
        string lastName,
        string email,
        CancellationToken ct)
    {
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
            && await userRepository.ExistsByEmailAsync(email, ct))
            throw new ConflictException($"A user with the email {email} already exists.");

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
    }

    private async Task EnsureCanManageAsync(User target, CancellationToken ct)
    {
        if (target.Id == currentUser.UserId)
            return;

        if (target.Role == UserRole.StateAdmin)
            throw new ForbiddenException(
                "A state administrator's account cannot be managed through the API.");

        if (currentUser.IsStateAdmin)
            return;

        var scope = await ResolveProfileAsync(target, ct);

        if (currentUser.IsRegionalAdmin)
        {
            if (target.Role is UserRole.RegionalAdmin)
                throw new ForbiddenException("You are not allowed to manage this account.");

            var callerCityId = currentUser.CityId;

            foreach (var schoolId in scope.SchoolIds)
            {
                var school = await schoolRepository.GetByIdAsync(schoolId, ct);
                if (school?.CityId is not null && school.CityId == callerCityId)
                    return;
            }

            throw new ForbiddenException("You are not allowed to manage this account.");
        }

        if (currentUser.IsDirectorate)
        {
            if (target.Role is UserRole.Directorate or UserRole.RegionalAdmin)
                throw new ForbiddenException("You are not allowed to manage this account.");

            if (currentUser.SchoolId is { } callerSchoolId && scope.SchoolIds.Contains(callerSchoolId))
                return;

            throw new ForbiddenException("You are not allowed to manage this account.");
        }

        throw new ForbiddenException("You are not allowed to manage this account.");
    }

    private async Task<AuthResultDto> BuildAuthResponseAsync(User user, CancellationToken ct)
    {
        var scope = await ResolveProfileAsync(user, ct);

        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(config.GetValue("Jwt:ExpiryMinutes", 60));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Read by AccountStatus.Admits against the account's SessionsValidFrom.
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        void Add(string name, Guid? value)
        {
            if (value is not null)
                claims.Add(new Claim(name, value.Value.ToString()));
        }

        foreach (var schoolId in scope.SchoolIds)
            claims.Add(new Claim(ClaimNames.SchoolId, schoolId.ToString()));

        Add(ClaimNames.TeacherId, scope.TeacherId);
        Add(ClaimNames.StudentId, scope.StudentId);
        Add(ClaimNames.CityId, scope.CityId);
        Add(ClaimNames.StateId, scope.StateId);
        Add(ClaimNames.GuardianId, scope.GuardianId);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResultDto(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            User: new AuthResponseDto(
                ExpiresAt: expiresAt,
                UserId: user.Id,
                SchoolId: scope.SchoolId,
                SchoolIds: scope.SchoolIds,
                TeacherId: scope.TeacherId,
                StudentId: scope.StudentId,
                CityId: scope.CityId,
                StateId: scope.StateId,
                GuardianId: scope.GuardianId,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Role: user.Role.ToString(),
                MustChangePassword: user.MustChangePassword));
    }

    private async Task<ProfileScope> ResolveProfileAsync(User user, CancellationToken ct)
    {
        switch (user.Role)
        {
            case UserRole.Teacher:
            {
                var teacher = await teacherRepository.GetByUserIdAsync(user.Id, ct);
                return ProfileScope.Empty with { SchoolIds = One(teacher?.SchoolId), TeacherId = teacher?.Id };
            }

            case UserRole.Student:
            {
                var student = await studentRepository.GetByUserIdAsync(user.Id, ct);
                return ProfileScope.Empty with { SchoolIds = One(student?.SchoolId), StudentId = student?.Id };
            }

            case UserRole.Directorate:
            {
                var directorate = await directorateRepository.GetByUserIdAsync(user.Id, ct);
                return ProfileScope.Empty with { SchoolIds = One(directorate?.SchoolId) };
            }

            case UserRole.RegionalAdmin:
            {
                var regional = await regionalAdminRepository.GetByUserIdAsync(user.Id, ct);
                return ProfileScope.Empty with { CityId = regional?.CityId };
            }

            case UserRole.StateAdmin:
            {
                var state = await stateAdminRepository.GetByUserIdAsync(user.Id, ct);
                return ProfileScope.Empty with { StateId = state?.StateId };
            }

            case UserRole.Guardian:
            {
                var guardian = await guardianRepository.GetByUserIdAsync(user.Id, ct);

                if (guardian is null)
                    return ProfileScope.Empty;

                // Every school the guardian has a child in — one school_id claim is emitted per id
                // so the school scope filter admits all of them.
                return ProfileScope.Empty with
                {
                    GuardianId = guardian.Id,
                    SchoolIds = guardian.Children.Select(c => c.SchoolId).Distinct().ToList()
                };
            }

            default:
                return ProfileScope.Empty;
        }

        static IReadOnlyList<Guid> One(Guid? id) => id is { } value ? [value] : [];
    }
}
