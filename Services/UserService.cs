using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduCore.Data;
using EduCore.Dtos.User;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PasswordGenerator;

namespace EduCore.Services;

public class UserService(
    IUserRepository userRepository,
    ITeacherRepository teacherRepository,
    IStudentRepository studentRepository,
    AppDbContext dbContext,
    IConfiguration config) : IUserService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<AuthResponseDto> LoginAsync(LoginUserDto dto, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(dto.Email, ct);

        
        if (user is null)
            throw new UnauthorizedException("Invalid email or password.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid email or password.");

        if (user.DeactivatedAt is not null)
            throw new UnauthorizedException("This account has been deactivated.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
            userRepository.Update(user);
            await userRepository.SaveChangesAsync(ct);
        }

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);

        if (user is null)
            throw new NotFoundException("User", userId);

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.MustChangePassword = false;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);
    }

    public async Task<ResetPasswordResponseDto> AdminResetPasswordAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);

        if (user is null)
            throw new NotFoundException("User", userId);

        var temporaryPassword = new Password(12)
            .IncludeLowercase()
            .IncludeUppercase()
            .IncludeNumeric()
            .IncludeSpecial()
            .Next();

        user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return new ResetPasswordResponseDto("Password reset successfully.", temporaryPassword);
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, CancellationToken ct)
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
            claims.Add(new Claim("school_id", schoolId.Value.ToString()));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponseDto(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt: expiresAt,
            UserId: user.Id,
            SchoolId: schoolId,
            TeacherId: teacherId,
            StudentId: studentId,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role.ToString(),
            MustChangePassword: user.MustChangePassword);
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
                var admin = await dbContext.Admins
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a => a.UserId == user.Id, ct);
                return (admin?.SchoolId, null, null);
            }

            default:
                return (null, null, null);
        }
    }
}
