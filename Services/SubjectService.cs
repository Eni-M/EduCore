using AutoMapper;
using EduCore.Dtos.Subject;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class SubjectService(ISubjectRepository subjects, ICurrentUser currentUser, IMapper mapper) : ISubjectService
{
    public async Task<IEnumerable<SubjectListDto>> GetAllAsync(CancellationToken ct) =>
        mapper.Map<IEnumerable<SubjectListDto>>(await subjects.GetAllOrderedAsync(ct));

    public async Task<SubjectListDto> GetByIdAsync(Guid subjectId, CancellationToken ct) =>
        mapper.Map<SubjectListDto>(await GetOwnedAsync(subjectId, ct));

    public async Task<SubjectListDto> CreateAsync(SubjectCreateDto dto, CancellationToken ct)
    {
        if (await subjects.NameExistsAsync(dto.Name, null, ct))
            throw new ConflictException($"A subject named {dto.Name} already exists in this school.");

        var subject = mapper.Map<Subject>(dto);
        subject.SchoolId = currentUser.RequireSchoolId();

        await subjects.AddAsync(subject);
        await subjects.SaveChangesAsync(ct);

        return mapper.Map<SubjectListDto>(subject);
    }

    public async Task<SubjectListDto> UpdateAsync(Guid subjectId, SubjectEditDto dto, CancellationToken ct)
    {
        var subject = await GetOwnedAsync(subjectId, ct);

        if (await subjects.NameExistsAsync(dto.Name, subjectId, ct))
            throw new ConflictException($"A subject named {dto.Name} already exists in this school.");

        mapper.Map(dto, subject);

        subjects.Update(subject);
        await subjects.SaveChangesAsync(ct);

        return mapper.Map<SubjectListDto>(subject);
    }

    public async Task DeleteAsync(Guid subjectId, CancellationToken ct)
    {
        var subject = await GetOwnedAsync(subjectId, ct);

        if (await subjects.IsInUseAsync(subjectId, ct))
            throw new ConflictException(
                "This subject is already used by teachers, lessons or marks and can no longer be deleted.");

        subjects.Remove(subject);
        await subjects.SaveChangesAsync(ct);
    }

    private async Task<Subject> GetOwnedAsync(Guid subjectId, CancellationToken ct) =>
        await subjects.GetByIdAsync(subjectId, ct)
        ?? throw new NotFoundException("Subject", subjectId);
}
