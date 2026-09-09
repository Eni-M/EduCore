using EduCore.Dtos.Account;
using EduCore.Dtos.Guardian;
using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Helpers;
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
            gs.Guardian.NationalId,
            gs.Guardian.PhoneNumber));
    }

    public async Task<IEnumerable<GuardianSearchResultDto>> SearchAsync(string query, CancellationToken ct)
    {
        // Directorate-only (enforced at endpoint). Search spans every school so a guardian
        // already registered elsewhere is reused, not duplicated.
        var matches = await guardians.SearchAsync(query, 20, ct);

        return matches.Select(g => new GuardianSearchResultDto(
            g.Id, g.User.FirstName, g.User.LastName, g.User.Email, g.NationalId, g.PhoneNumber));
    }

    public async Task LinkAsync(Guid studentId, Guid guardianId, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        // School-scoped: returns null if the student is not in the caller's school.
        _ = await students.GetByIdAsync(studentId, ct)
            ?? throw new NotFoundException("Student", studentId);

        _ = await guardians.GetGloballyAsync(guardianId, ct)
            ?? throw new NotFoundException("Guardian", guardianId);

        if (await guardians.LinkExistsAsync(guardianId, studentId, ct))
            return;

        await guardians.AddLinkAsync(new GuardianStudent
        {
            GuardianId = guardianId,
            StudentId = studentId,
            SchoolId = schoolId
        });
        await guardians.SaveChangesAsync(ct);
    }

    public async Task UnlinkAsync(Guid studentId, Guid guardianId, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        _ = await students.GetByIdAsync(studentId, ct)
            ?? throw new NotFoundException("Student", studentId);

        var link = await guardians.GetLinkAsync(guardianId, studentId, ct);
        if (link is null || link.SchoolId != schoolId)
            return;

        guardians.RemoveLink(link);
        await guardians.SaveChangesAsync(ct);
    }

    public async Task<PagedResultDto<GuardianListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();
        var page = Math.Max(1, pagination.Page);
        var pageSize = Math.Clamp(pagination.PageSize, 1, ListPagination.MaxPageSize);

        var (items, totalCount) = await guardians.GetPagedBySchoolAsync(
            schoolId, new PaginationQueryDto(page, pageSize), search, ct);

        return new PagedResultDto<GuardianListDto>(
            items.Select(g => ToListDto(g, schoolId)).ToList(),
            page,
            pageSize,
            totalCount);
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

        var nationalId = dto.NationalId.Trim();
        if (await guardians.NationalIdExistsAsync(nationalId, ct))
            throw new ConflictException(
                "A guardian with this national ID already exists. Search for them and add them to the student instead.");

        var user = await users.BuildLoginAsync(
            dto.FirstName, dto.LastName, dto.Email, dto.Password, UserRole.Guardian, ct);

        var guardian = new Guardian
        {
            User = user,
            NationalId = nationalId,
            PhoneNumber = dto.PhoneNumber.Trim()
        };

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
            .Select(c => new GuardianChildDto(
                c.StudentId, c.Student.User.FirstName, c.Student.User.LastName,
                c.Student.School.Name, c.Student.School.Type))
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
            guardian.NationalId,
            guardian.PhoneNumber,
            guardian.Children
                .Where(c => c.SchoolId == schoolId)
                .Select(c => new GuardianChildDto(
                    c.StudentId, c.Student.User.FirstName, c.Student.User.LastName,
                    c.Student.School.Name, c.Student.School.Type))
                .ToList(),
            new AccountDto(guardian.UserId, guardian.User.DeactivatedAt));
}
