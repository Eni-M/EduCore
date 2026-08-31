using AutoMapper;
using EduCore.Dtos.Pagination;
using EduCore.Dtos.Parent;
using EduCore.Dtos.Student;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Helpers;
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
    IGradingPeriodRepository gradingPeriods,
    IClassSubjectTeacherRepository assignments,
    IUserRepository userRepository,
    IUserService users,
    ILocationRepository locations,
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
            WithVisibleAccounts(mapper.Map<List<StudentListDto>>(items)).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<IEnumerable<StudentListDto>> GetByClassAsync(Guid classId, CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);

        return WithVisibleAccounts(
            mapper.Map<IEnumerable<StudentListDto>>(await students.GetByClassAsync(classId, ct)));
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

        if (dto.CityId is { } cityId && await locations.GetCityAsync(cityId, ct) is null)
            throw new NotFoundException("City", cityId);

        var student = mapper.Map<Student>(dto);
        student.User = user;
        student.SchoolId = currentUser.RequireSchoolId();

        await students.AddAsync(student);

        if (!string.IsNullOrWhiteSpace(dto.GuardianName)
            && !string.IsNullOrWhiteSpace(dto.GuardianPhone))
        {
            student.Parents.Add(new Parent
            {
                Name = dto.GuardianName.Trim(),
                PhoneNumber = dto.GuardianPhone.Trim(),
                SchoolId = student.SchoolId
            });
        }

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

        if (dto.CityId is { } cityId && await locations.GetCityAsync(cityId, ct) is null)
            throw new NotFoundException("City", cityId);

        await users.ApplyLoginEditAsync(user, dto.FirstName, dto.LastName, dto.Email, ct);

        mapper.Map(dto, student);
        students.Update(student);

        await students.SaveChangesAsync(ct);

        return await GetByIdAsync(studentId, ct);
    }

    public async Task<GraduationEligibilityDto> GetGraduationEligibilityAsync(
        Guid studentId, CancellationToken ct)
    {
        var student = await students.GetByIdAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        return await EvaluateGraduationAsync(student, ct);
    }

    public async Task GraduateAsync(Guid studentId, StudentGraduateDto dto, CancellationToken ct)
    {
        var student = await students.GetByIdAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        if (student.GraduationDate is not null)
            throw new ConflictException("This student has already graduated.");

        var eligibility = await EvaluateGraduationAsync(student, ct);

        if (!eligibility.CanGraduate)
            throw new BusinessException(eligibility.Reason!);

        var enrollment = (await enrollments.GetActiveForStudentAsync(studentId, ct))!;

        if (dto.GraduationDate < enrollment.EnrolledAt)
            throw new BusinessException(
                $"They joined this class on {enrollment.EnrolledAt:yyyy-MM-dd}; a graduation " +
                "cannot be dated before that.");

        enrollment.LeftAt = dto.GraduationDate;
        enrollment.ExitReason = EnrollmentExitReason.Graduated;
        enrollments.Update(enrollment);

        student.GraduationDate = dto.GraduationDate;
        students.Update(student);

        await students.SaveChangesAsync(ct);
    }

    private async Task<GraduationEligibilityDto> EvaluateGraduationAsync(
        Models.Student student, CancellationToken ct)
    {
        if (student.GraduationDate is not null)
            return new GraduationEligibilityDto(false, "This student has already graduated.");

        var enrollment = await enrollments.GetActiveForStudentAsync(student.Id, ct);

        if (enrollment is null)
            return new GraduationEligibilityDto(
                false, "This student is not currently enrolled in a class.");

        var studentClass = await classes.GetDtoAsync(enrollment.ClassId, ct);

        if (studentClass is null)
            return new GraduationEligibilityDto(false, "This student's class could not be found.");

        if (!GraduationRule.IsFinalGrade(studentClass.Grade))
            return new GraduationEligibilityDto(
                false,
                $"Only a student in grade {GraduationRule.FinalGradesText} can graduate -- those " +
                $"are the last grade of each school. {studentClass.Name} is grade {studentClass.Grade}.");

        var periods = (await gradingPeriods.GetByAcademicYearAsync(studentClass.AcademicYearId, ct))
            .ToList();

        if (periods.Count < 2)
            return new GraduationEligibilityDto(
                false,
                $"{studentClass.AcademicYearName} does not have both of its terms set up yet, so a " +
                "full year of marks cannot be confirmed.");

        var subjectIds = (await assignments.GetByClassAsync(enrollment.ClassId, false, ct))
            .Select(a => a.SubjectId)
            .Distinct()
            .ToList();

        if (subjectIds.Count == 0)
            return new GraduationEligibilityDto(
                false, $"{studentClass.Name} has no subjects assigned, so there is nothing to complete.");

        var graded = (await marks.GetByStudentAsync(student.Id, null, null, ct))
            .Where(m => m.FinalMark is not null)
            .Select(m => (m.SubjectId, m.GradingPeriodId))
            .ToHashSet();

        var missing = (from subjectId in subjectIds
                       from period in periods
                       where !graded.Contains((subjectId, period.Id))
                       select subjectId).Count();

        if (missing > 0)
            return new GraduationEligibilityDto(
                false,
                $"{missing} subject-term pair{(missing == 1 ? "" : "s")} across " +
                $"{studentClass.AcademicYearName} still {(missing == 1 ? "needs" : "need")} both " +
                "marks. A student graduates on a complete year, not a partial one.");

        return new GraduationEligibilityDto(true, null);
    }

    private static (int Page, int PageSize) Normalize(PaginationQueryDto pagination) =>
        (Math.Max(1, pagination.Page), Math.Clamp(pagination.PageSize, 1, 100));

    private IEnumerable<StudentListDto> WithVisibleAccounts(IEnumerable<StudentListDto> rows) =>
        currentUser.CanManageAccounts() ? rows : rows.Select(row => row with { Account = null });
}
