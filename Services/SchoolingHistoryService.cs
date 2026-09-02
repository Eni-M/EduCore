using AutoMapper;
using EduCore.Dtos.SchoolingHistory;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class SchoolingHistoryService(
    ISchoolingHistoryRepository histories,
    IStudentRepository students,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : ISchoolingHistoryService
{
    public async Task<IEnumerable<SchoolingHistoryListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);

        return mapper.Map<IEnumerable<SchoolingHistoryListDto>>(await histories.GetByStudentAsync(studentId, ct));
    }

    public async Task<SchoolingHistoryListDto> CreateAsync(
        Guid studentId, SchoolingHistoryCreateDto dto, CancellationToken ct)
    {
        await EnsureStudentInSchoolAsync(studentId, ct);

        var entity = mapper.Map<SchoolingHistory>(dto);
        entity.StudentId = studentId;
        entity.SchoolId = currentUser.RequireSchoolId();

        await histories.AddAsync(entity);
        await histories.SaveChangesAsync(ct);

        return mapper.Map<SchoolingHistoryListDto>(entity);
    }

    public async Task<SchoolingHistoryListDto> UpdateAsync(
        Guid studentId, Guid historyId, SchoolingHistoryEditDto dto, CancellationToken ct)
    {
        await EnsureStudentInSchoolAsync(studentId, ct);

        var entity = await OwnedAsync(studentId, historyId, ct);

        mapper.Map(dto, entity);
        histories.Update(entity);
        await histories.SaveChangesAsync(ct);

        return mapper.Map<SchoolingHistoryListDto>(entity);
    }

    public async Task DeleteAsync(Guid studentId, Guid historyId, CancellationToken ct)
    {
        await EnsureStudentInSchoolAsync(studentId, ct);

        var entity = await OwnedAsync(studentId, historyId, ct);

        histories.Remove(entity);
        await histories.SaveChangesAsync(ct);
    }

    // The student repository is school-filtered, so a student in another school reads back as null
    // and this is where cross-school writes are refused.
    private async Task EnsureStudentInSchoolAsync(Guid studentId, CancellationToken ct) =>
        _ = await students.GetByIdAsync(studentId, ct) ?? throw new NotFoundException("Student", studentId);

    private async Task<SchoolingHistory> OwnedAsync(Guid studentId, Guid historyId, CancellationToken ct)
    {
        var entity = await histories.GetByIdAsync(historyId, ct)
                     ?? throw new NotFoundException("Schooling history", historyId);

        // A history row filed under one student must not be edited through another student's route.
        if (entity.StudentId != studentId)
            throw new NotFoundException("Schooling history", historyId);

        return entity;
    }
}
