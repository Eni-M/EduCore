using AutoMapper;
using EduCore.Dtos.ClassSubjectTeacher;
using EduCore.Dtos.Subject;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;

namespace EduCore.Services;

public class SubjectService(
    ISubjectRepository subjects,
    IClassSubjectTeacherRepository assignments,
    IEnrollmentRepository enrollments,
    ICurrentUser currentUser,
    IMapper mapper) : ISubjectService
{
    public async Task<IEnumerable<SubjectListDto>> GetAllAsync(CancellationToken ct)
    {
        if (currentUser.IsDirectorate)
            return mapper.Map<IEnumerable<SubjectListDto>>(await subjects.GetAllOrderedAsync(ct));

        if (currentUser.IsTeacher)
            return Distinct(
                await assignments.GetByTeacherAsync(currentUser.RequireTeacherId(), false, ct));

        var enrollment = await enrollments.GetActiveForStudentAsync(currentUser.RequireStudentId(), ct);

        return enrollment is null
            ? []
            : Distinct(await assignments.GetByClassAsync(enrollment.ClassId, false, ct));
    }

    public async Task<PagedResultDto<SubjectListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        // A teacher or student sees only their own subjects, which comes from their
        // assignments rather than the catalogue, so those two keep the in-memory path.
        if (!currentUser.IsDirectorate)
            return (await GetAllAsync(ct)).Search(search, s => new[] { s.Name }).ToPagedResult(pagination);

        var (items, total) = await subjects.GetPagedAsync(pagination, search, ct);
        var (page, pageSize) = ListPagination.Normalize(pagination);

        return new PagedResultDto<SubjectListDto>(
            mapper.Map<IReadOnlyList<SubjectListDto>>(items), page, pageSize, total);
    }

    private static IEnumerable<SubjectListDto> Distinct(
        IEnumerable<ClassSubjectTeacherListDto> rows) =>
        rows.GroupBy(a => a.SubjectId)
            .Select(g => new SubjectListDto(g.Key, g.First().SubjectName))
            .OrderBy(s => s.Name);

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
