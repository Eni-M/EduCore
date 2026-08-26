using AutoMapper;
using EduCore.Dtos.Pagination;
using EduCore.Dtos.Parent;
using EduCore.Dtos.Student;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class StudentService(
    IStudentRepository students,
    IEnrollmentRepository enrollments,
    IClassRepository classes,
    IAttendanceRepository attendances,
    IMarkRepository marks,
    IUserRepository userRepository,
    IUserService users,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : IStudentService
{
    public async Task<PagedResultDto<StudentListDto>> GetPagedAsync(
        PaginationQueryDto pagination,
        string? search,
        bool includeGraduated,
        CancellationToken ct)
    {
        var (page, pageSize) = Normalize(pagination);

        var (items, totalCount) = await students.GetPagedAsync(page, pageSize, search, includeGraduated, ct);

        return new PagedResultDto<StudentListDto>(
            mapper.Map<List<StudentListDto>>(items),
            page,
            pageSize,
            totalCount);
    }

    public async Task<IEnumerable<StudentListDto>> GetByClassAsync(Guid classId, CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);
        return mapper.Map<IEnumerable<StudentListDto>>(await students.GetByClassAsync(classId, ct));
    }

    public async Task<StudentDetailDto> GetByIdAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);

        var student = await students.GetWithParentsAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        return mapper.Map<StudentDetailDto>(student);
    }

    public async Task<StudentProfileDto> GetProfileAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);

        var student = await students.GetWithParentsAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        var enrollment = await enrollments.GetActiveForStudentAsync(studentId, ct);
        var currentClass = enrollment is null ? null : await classes.GetDtoAsync(enrollment.ClassId, ct);

        return new StudentProfileDto(
            student.Id,
            student.User.FirstName,
            student.User.LastName,
            student.User.Email,
            student.DateOfBirth,
            student.PersonalNumber,
            student.GraduationDate,
            student.Gender,
            mapper.Map<List<ParentListDto>>(student.Parents),
            currentClass?.Id,
            currentClass?.Name,
            currentClass?.Grade,
            (await attendances.GetByStudentAsync(studentId, null, null, ct)).ToList(),
            (await marks.GetByStudentAsync(studentId, null, null, ct)).ToList());
    }

    public async Task<StudentDetailDto> CreateAsync(StudentCreateDto dto, CancellationToken ct)
    {
        if (await students.PersonalNumberExistsAsync(dto.PersonalNumber, null, ct))
            throw new ConflictException("This personal number is already registered.");

        var user = await users.BuildLoginAsync(
            dto.FirstName, dto.LastName, dto.Email, dto.Password, UserRole.Student, ct);

        var student = mapper.Map<Student>(dto);
        student.User = user;
        student.SchoolId = currentUser.RequireSchoolId();

        await students.AddAsync(student);

        await students.SaveChangesAsync(ct);

        return await GetByIdAsync(student.Id, ct);
    }

    public async Task<StudentDetailDto> UpdateAsync(Guid studentId, StudentEditDto dto, CancellationToken ct)
    {
        var student = await students.GetByIdAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        if (await students.PersonalNumberExistsAsync(dto.PersonalNumber, studentId, ct))
            throw new ConflictException("This personal number is already registered.");

        var user = await userRepository.GetByIdAsync(student.UserId, ct)
                   ?? throw new NotFoundException("User", student.UserId);

        await users.ApplyLoginEditAsync(user, dto.FirstName, dto.LastName, dto.Email, ct);

        mapper.Map(dto, student);
        students.Update(student);

        await students.SaveChangesAsync(ct);

        return await GetByIdAsync(studentId, ct);
    }

    public async Task GraduateAsync(Guid studentId, StudentGraduateDto dto, CancellationToken ct)
    {
        var student = await students.GetByIdAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        if (student.GraduationDate is not null)
            throw new ConflictException("This student has already graduated.");

        var enrollment = await enrollments.GetActiveForStudentAsync(studentId, ct);
        if (enrollment is not null)
            throw new BusinessException(
                "Withdraw the student from their current class before recording a graduation.");

        student.GraduationDate = dto.GraduationDate;

        students.Update(student);
        await students.SaveChangesAsync(ct);
    }

    private static (int Page, int PageSize) Normalize(PaginationQueryDto pagination) =>
        (Math.Max(1, pagination.Page), Math.Clamp(pagination.PageSize, 1, 100));
}
