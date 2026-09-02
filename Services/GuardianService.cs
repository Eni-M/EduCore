using EduCore.Dtos.Account;
using EduCore.Dtos.Guardian;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class GuardianService(
    IGuardianRepository guardians,
    IStudentRepository students,
    IUserRepository userRepository,
    IUserService users,
    IAccessGuard access,
    ICurrentUser currentUser) : IGuardianService
{
    public async Task<IEnumerable<GuardianContactDto>> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);

        var contacts = await guardians.GetContactsForStudentAsync(studentId, ct);

        return contacts.Select(gs => new GuardianContactDto(
            gs.Guardian.Id,
            gs.Guardian.User.FirstName,
            gs.Guardian.User.LastName,
            gs.Guardian.User.Email,
            gs.Guardian.PhoneNumber));
    }

    public async Task<IEnumerable<GuardianListDto>> GetAllAsync(CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();
        var rows = await guardians.GetBySchoolAsync(schoolId, ct);
        return rows.Select(g => ToListDto(g, schoolId));
    }

    public async Task<GuardianListDto> GetByIdAsync(Guid guardianId, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        var guardian = await guardians.GetWithChildrenAsync(guardianId, ct)
                       ?? throw new NotFoundException("Guardian", guardianId);

        EnsureManageable(guardian, schoolId);

        return ToListDto(guardian, schoolId);
    }

    public async Task<GuardianListDto> CreateAsync(GuardianCreateDto dto, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        var studentIds = dto.StudentIds.Distinct().ToList();
        await EnsureStudentsInSchoolAsync(studentIds, ct);

        var user = await users.BuildLoginAsync(
            dto.FirstName, dto.LastName, dto.Email, dto.Password, UserRole.Guardian, ct);

        var guardian = new Guardian { User = user, PhoneNumber = dto.PhoneNumber.Trim() };

        foreach (var studentId in studentIds)
            guardian.Children.Add(new GuardianStudent { StudentId = studentId, SchoolId = schoolId });

        await guardians.AddAsync(guardian);
        await guardians.SaveChangesAsync(ct);

        return await GetByIdAsync(guardian.Id, ct);
    }

    public async Task<GuardianListDto> UpdateAsync(Guid guardianId, GuardianEditDto dto, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        var guardian = await guardians.GetTrackedWithChildrenAsync(guardianId, ct)
                       ?? throw new NotFoundException("Guardian", guardianId);

        EnsureManageable(guardian, schoolId);

        var user = await userRepository.GetByIdAsync(guardian.UserId, ct)
                   ?? throw new NotFoundException("User", guardian.UserId);

        await users.ApplyLoginEditAsync(user, dto.FirstName, dto.LastName, dto.Email, ct);

        guardian.PhoneNumber = dto.PhoneNumber.Trim();
        guardians.Update(guardian);
        await guardians.SaveChangesAsync(ct);

        return await GetByIdAsync(guardianId, ct);
    }

    public async Task<GuardianListDto> SetChildrenAsync(Guid guardianId, GuardianLinksDto dto, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        var guardian = await guardians.GetTrackedWithChildrenAsync(guardianId, ct)
                       ?? throw new NotFoundException("Guardian", guardianId);

        EnsureManageable(guardian, schoolId);

        var desired = dto.StudentIds.Distinct().ToList();
        await EnsureStudentsInSchoolAsync(desired, ct);

        var inSchool = guardian.Children.Where(c => c.SchoolId == schoolId).ToList();

        foreach (var link in inSchool.Where(c => !desired.Contains(c.StudentId)))
            guardians.RemoveLink(link);

        foreach (var studentId in desired.Where(id => inSchool.All(c => c.StudentId != id)))
            await guardians.AddLinkAsync(new GuardianStudent
            {
                GuardianId = guardianId,
                StudentId = studentId,
                SchoolId = schoolId
            });

        await guardians.SaveChangesAsync(ct);

        return await GetByIdAsync(guardianId, ct);
    }

    public async Task<IEnumerable<GuardianChildDto>> GetMyChildrenAsync(CancellationToken ct)
    {
        var guardianId = currentUser.RequireGuardianId();

        var guardian = await guardians.GetWithChildrenAsync(guardianId, ct)
                       ?? throw new NotFoundException("Guardian", guardianId);

        return guardian.Children
            .Select(c => new GuardianChildDto(c.StudentId, c.Student.User.FirstName, c.Student.User.LastName))
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName);
    }

    private async Task EnsureStudentsInSchoolAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct)
    {
        foreach (var studentId in studentIds)
            _ = await students.GetByIdAsync(studentId, ct)
                ?? throw new NotFoundException("Student", studentId);
    }

    private void EnsureManageable(Guardian guardian, Guid schoolId)
    {
        if (!guardian.Children.Any(c => c.SchoolId == schoolId))
            throw new ForbiddenException("You can only manage guardians linked to a student in your school.");
    }

    private static GuardianListDto ToListDto(Guardian guardian, Guid schoolId) =>
        new(
            guardian.Id,
            guardian.User.FirstName,
            guardian.User.LastName,
            guardian.User.Email,
            guardian.PhoneNumber,
            guardian.Children
                .Where(c => c.SchoolId == schoolId)
                .Select(c => new GuardianChildDto(
                    c.StudentId, c.Student.User.FirstName, c.Student.User.LastName))
                .ToList(),
            new AccountDto(guardian.UserId, guardian.User.DeactivatedAt));
}
