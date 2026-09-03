using AutoMapper;
using EduCore.Dtos.Teacher;
using EduCore.Dtos.TeacherSubject;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Assignment;
using EduCore.Services.Interfaces;
using Microsoft.Extensions.Options;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;

namespace EduCore.Services;

public class TeacherService(
    ITeacherRepository teachers,
    ITeacherSubjectRepository teacherSubjects,
    ISubjectRepository subjects,
    ISchoolRepository schools,
    IUserRepository userRepository,
    IUserService users,
    ICurrentUser currentUser,
    IOptions<AssignmentOptions> assignmentOptions,
    IMapper mapper) : ITeacherService
{
    public async Task<IEnumerable<TeacherListDto>> GetAllAsync(CancellationToken ct) =>
        WithVisibleAccounts(mapper.Map<IEnumerable<TeacherListDto>>(await teachers.GetAllWithUserAsync(ct)));

    public async Task<PagedResultDto<TeacherListDto>> GetPagedAsync(
        PaginationQueryDto pagination, string? search, CancellationToken ct)
    {
        var all = await GetAllAsync(ct);
        return all.Search(search,
                t => new[] { t.FirstName, t.LastName, t.Email, string.Join(" ", t.Subjects) })
            .ToPagedResult(pagination);
    }

    public async Task<TeacherDetailDto> GetByIdAsync(Guid teacherId, CancellationToken ct)
    {
        var teacher = await teachers.GetWithSubjectsAsync(teacherId, ct)
                      ?? throw new NotFoundException("Teacher", teacherId);

        return mapper.Map<TeacherDetailDto>(teacher);
    }

    public async Task<IEnumerable<TeacherListDto>> GetBySubjectAsync(Guid subjectId, CancellationToken ct) =>
        WithVisibleAccounts(mapper.Map<IEnumerable<TeacherListDto>>(await teachers.GetBySubjectAsync(subjectId, ct)));

    public async Task<TeacherDetailDto> CreateAsync(TeacherCreateDto dto, CancellationToken ct)
    {
        var user = await users.BuildLoginAsync(
            dto.FirstName, dto.LastName, dto.Email, dto.Password, UserRole.Teacher, ct);

        var teacher = new Teacher
        {
            User = user,
            SchoolId = currentUser.RequireSchoolId()
        };

        await teachers.AddAsync(teacher);

        await teachers.SaveChangesAsync(ct);

        return await GetByIdAsync(teacher.Id, ct);
    }

    public async Task<TeacherDetailDto> UpdateAsync(Guid teacherId, TeacherEditDto dto, CancellationToken ct)
    {
        var teacher = await teachers.GetByIdAsync(teacherId, ct)
                      ?? throw new NotFoundException("Teacher", teacherId);

        var user = await userRepository.GetByIdAsync(teacher.UserId, ct)
                   ?? throw new NotFoundException("User", teacher.UserId);

        await users.ApplyLoginEditAsync(user, dto.FirstName, dto.LastName, dto.Email, ct);

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        return await GetByIdAsync(teacherId, ct);
    }

    public async Task<IEnumerable<TeacherSubjectListDto>> GetSubjectsAsync(Guid teacherId, CancellationToken ct)
    {
        _ = await teachers.GetByIdAsync(teacherId, ct) ?? throw new NotFoundException("Teacher", teacherId);
        return await teacherSubjects.GetByTeacherAsync(teacherId, ct);
    }

    public async Task<TeacherSubjectListDto> AddSubjectAsync(TeacherSubjectCreateDto dto, CancellationToken ct)
    {
        var teacher = await teachers.GetByIdAsync(dto.TeacherId, ct)
            ?? throw new NotFoundException("Teacher", dto.TeacherId);

        var subject = await subjects.GetByIdAsync(dto.SubjectId, ct)
                      ?? throw new NotFoundException("Subject", dto.SubjectId);

        if (await teacherSubjects.ExistsAsync(dto.TeacherId, dto.SubjectId, ct))
            throw new ConflictException("This teacher is already qualified for that subject.");

        var school = await schools.GetByIdAsync(teacher.SchoolId, ct);

        if (school?.Type != SchoolType.Elementary)
        {
            var currentCount = (await teacherSubjects.GetByTeacherAsync(dto.TeacherId, ct)).Count();

            if (currentCount >= assignmentOptions.Value.MaxSubjectsPerTeacher)
                throw new BusinessException(
                    $"A teacher may be qualified for at most {assignmentOptions.Value.MaxSubjectsPerTeacher} subjects.");
        }

        var qualification = mapper.Map<TeacherSubject>(dto);
        qualification.SchoolId = currentUser.RequireSchoolId();

        await teacherSubjects.AddAsync(qualification);
        await teacherSubjects.SaveChangesAsync(ct);

        return new TeacherSubjectListDto(qualification.Id, subject.Id, subject.Name);
    }

    public async Task RemoveSubjectAsync(Guid teacherId, Guid subjectId, CancellationToken ct)
    {
        var qualification = await teacherSubjects.GetAsync(teacherId, subjectId, ct)
                            ?? throw new NotFoundException(
                                "This teacher has no qualification recorded for that subject.");

        if (await teacherSubjects.IsInUseAsync(teacherId, subjectId, ct))
            throw new ConflictException(
                "This qualification is already used by a teaching assignment or a logged lesson and cannot be removed.");

        teacherSubjects.Remove(qualification);
        await teacherSubjects.SaveChangesAsync(ct);
    }

    private IEnumerable<TeacherListDto> WithVisibleAccounts(IEnumerable<TeacherListDto> rows) =>
        currentUser.CanManageAccounts() ? rows : rows.Select(row => row with { Account = null });
}
