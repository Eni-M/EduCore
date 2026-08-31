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

public class UserService(
    IUserRepository userRepository,
    ITeacherRepository teacherRepository,
    IStudentRepository studentRepository,
    IAdminRepository adminRepository,
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
        if (target.Role == UserRole.SuperAdmin && target.Id != currentUser.UserId)
            throw new ForbiddenException(
                "A super administrator's account cannot be managed through the API.");

        if (currentUser.IsSuperAdmin)
            return;

        if (!currentUser.IsAdmin)
            throw new ForbiddenException("You are not allowed to manage this account.");

        if (target.Role is UserRole.Admin or UserRole.SuperAdmin)
            throw new ForbiddenException("Administrator accounts are managed by the super administrator.");

        var (targetSchoolId, _, _) = await ResolveProfileAsync(target, ct);

        if (targetSchoolId is null || targetSchoolId != currentUser.SchoolId)
            throw new ForbiddenException("You are not allowed to manage this account.");
    }

    private async Task<AuthResultDto> BuildAuthResponseAsync(User user, CancellationToken ct)
    {
        var (schoolId, teacherId, studentId) = await ResolveProfileAsync(user, ct);

        var expiresAt = DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:ExpiryMinutes", 60));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (schoolId is not null)
            claims.Add(new Claim(ClaimNames.SchoolId, schoolId.Value.ToString()));

        if (teacherId is not null)
            claims.Add(new Claim(ClaimNames.TeacherId, teacherId.Value.ToString()));

        if (studentId is not null)
            claims.Add(new Claim(ClaimNames.StudentId, studentId.Value.ToString()));

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
                SchoolId: schoolId,
                TeacherId: teacherId,
                StudentId: studentId,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Role: user.Role.ToString(),
                MustChangePassword: user.MustChangePassword));
    }

    private async Task<(Guid? SchoolId, Guid? TeacherId, Guid? StudentId)> ResolveProfileAsync(
        User user,
        CancellationToken ct)
    {
        switch (user.Role)
        {
            case UserRole.Teacher:
            {
                var teacher = await teacherRepository.GetByUserIdAsync(user.Id, ct);
                return (teacher?.SchoolId, teacher?.Id, null);
            }

            case UserRole.Student:
            {
                var student = await studentRepository.GetByUserIdAsync(user.Id, ct);
                return (student?.SchoolId, null, student?.Id);
            }

            case UserRole.Admin:
            {
                var admin = await adminRepository.GetByUserIdAsync(user.Id, ct);
                return (admin?.SchoolId, null, null);
            }

            default:
                return (null, null, null);
        }
    }
}
