using AutoMapper;
using EduCore.Dtos.Class;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Assignment;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class ClassService(
    IClassRepository classes,
    ISchoolRepository schools,
    IAcademicYearRepository academicYears,
    IAccessGuard access,
    ITeacherRepository teachers,
    IElementaryAssignmentSync elementarySync,
    ICurrentUser currentUser,
    IMapper mapper) : IClassService
{
    public async Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct)
    {
        if (currentUser.IsDirectorate)
            return await classes.GetAllAsync(academicYearId, ct);

        var mine = await classes.GetByTeacherAsync(currentUser.RequireTeacherId(), ct);

        return academicYearId is null
            ? mine
            : mine.Where(c => c.AcademicYearId == academicYearId);
    }

    public async Task<ClassListDto> GetByIdAsync(Guid classId, CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);
        return await ReadAsync(classId, ct);
    }

    private async Task<ClassListDto> ReadAsync(Guid classId, CancellationToken ct) =>
        await classes.GetDtoAsync(classId, ct)
        ?? throw new NotFoundException("Class", classId);

    public async Task<IEnumerable<ClassListDto>> GetMineAsync(CancellationToken ct) =>
        await classes.GetByTeacherAsync(currentUser.RequireTeacherId(), ct);

    public async Task<ClassListDto> CreateAsync(ClassCreateDto dto, CancellationToken ct)
    {
        _ = await academicYears.GetByIdAsync(dto.AcademicYearId, ct)
            ?? throw new NotFoundException("Academic year", dto.AcademicYearId);

        if (await classes.NameExistsInYearAsync(dto.AcademicYearId, dto.Name, null, ct))
            throw new ConflictException($"A class named {dto.Name} already exists in this academic year.");

        var schoolId = currentUser.RequireSchoolId();
        var school = await schools.GetByIdAsync(schoolId, ct)
                     ?? throw new NotFoundException("School", schoolId);
        GradeBands.Validate(school.Type, dto.Grade);

        var entity = mapper.Map<Class>(dto);
        entity.SchoolId = schoolId;

        await classes.AddAsync(entity);
        await classes.SaveChangesAsync(ct);

        return await ReadAsync(entity.Id, ct);
    }

    public async Task<ClassListDto> UpdateAsync(Guid classId, ClassEditDto dto, CancellationToken ct)
    {
        var entity = await GetOwnedAsync(classId, ct);

        if (await classes.NameExistsInYearAsync(entity.AcademicYearId, dto.Name, classId, ct))
            throw new ConflictException($"A class named {dto.Name} already exists in this academic year.");

        var school = await schools.GetByIdAsync(entity.SchoolId, ct)
                     ?? throw new NotFoundException("School", entity.SchoolId);
        GradeBands.Validate(school.Type, dto.Grade);

        mapper.Map(dto, entity);

        classes.Update(entity);
        await classes.SaveChangesAsync(ct);

        return await ReadAsync(classId, ct);
    }

    public async Task<ClassListDto> AssignHomeroomTeacherAsync(
        Guid classId,
        ClassAssignHomeroomTeacherDto dto,
        CancellationToken ct)
    {
        var entity = await GetOwnedAsync(classId, ct);
        var oldTeacherId = entity.HomeroomTeacherId;

        if (dto.TeacherId is not null)
            _ = await teachers.GetByIdAsync(dto.TeacherId.Value, ct)
                ?? throw new NotFoundException("Teacher", dto.TeacherId.Value);

        entity.HomeroomTeacherId = dto.TeacherId;
        classes.Update(entity);

        await elementarySync.SyncHomeroomChangeAsync(
            entity.SchoolId, classId, oldTeacherId, dto.TeacherId, ct);

        await classes.SaveChangesAsync(ct);

        return await ReadAsync(classId, ct);
    }

    private async Task<Class> GetOwnedAsync(Guid classId, CancellationToken ct) =>
        await classes.GetByIdAsync(classId, ct)
        ?? throw new NotFoundException("Class", classId);
}
