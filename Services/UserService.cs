using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduCore.Dtos.User;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
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

    /// <summary>The single school this profile is pinned to, or null when it spans zero or several.</summary>
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
    ICurrentUser currentUser,
    IConfiguration config) : IUserService
{
    public async Task<AuthResultDto> LoginAsync(LoginUserDto dto, CancellationToken ct)
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

        return await BuildAuthResponseAsync(user, ct);
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

        user.SetPassword(dto.NewPassword, passwordHasher);
        user.MustChangePassword = false;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResultDto> UpdateOwnCredentialsAsync(
        Guid userId, string firstName, string lastName, string email, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User", userId);

        if (user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

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

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return new ResetPasswordResponseDto("Password reset successfully.", temporaryPassword);
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

        var expiresAt = DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:ExpiryMinutes", 60));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
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
