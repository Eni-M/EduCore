using AutoMapper;
using EduCore.Dtos.Class;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class ClassService(
    IClassRepository classes,
    IAcademicYearRepository academicYears,
    IAccessGuard access,
    ITeacherRepository teachers,
    ICurrentUser currentUser,
    IMapper mapper) : IClassService
{
    public async Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct) =>
        await classes.GetAllAsync(academicYearId, ct);

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

        var entity = mapper.Map<Class>(dto);
        entity.SchoolId = currentUser.RequireSchoolId();

        await classes.AddAsync(entity);
        await classes.SaveChangesAsync(ct);

        return await ReadAsync(entity.Id, ct);
    }

    public async Task<ClassListDto> UpdateAsync(Guid classId, ClassEditDto dto, CancellationToken ct)
    {
        var entity = await GetOwnedAsync(classId, ct);

        if (await classes.NameExistsInYearAsync(entity.AcademicYearId, dto.Name, classId, ct))
            throw new ConflictException($"A class named {dto.Name} already exists in this academic year.");

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

        if (dto.TeacherId is not null)
            _ = await teachers.GetByIdAsync(dto.TeacherId.Value, ct)
                ?? throw new NotFoundException("Teacher", dto.TeacherId.Value);

        entity.HomeroomTeacherId = dto.TeacherId;

        classes.Update(entity);
        await classes.SaveChangesAsync(ct);

        return await ReadAsync(classId, ct);
    }

    private async Task<Class> GetOwnedAsync(Guid classId, CancellationToken ct) =>
        await classes.GetByIdAsync(classId, ct)
        ?? throw new NotFoundException("Class", classId);
}
