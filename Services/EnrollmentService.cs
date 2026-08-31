using AutoMapper;
using EduCore.Dtos.Enrollment;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class EnrollmentService(
    IEnrollmentRepository enrollments,
    IStudentRepository students,
    IClassRepository classes,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : IEnrollmentService
{
    public async Task<IEnumerable<EnrollmentListDto>> GetByClassAsync(
        Guid classId,
        bool includeLeft,
        CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);
        return await enrollments.GetByClassAsync(classId, includeLeft, ct);
    }

    public async Task<IEnumerable<EnrollmentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);
        return await enrollments.GetByStudentAsync(studentId, ct);
    }

    public async Task<EnrollmentListDto> EnrollAsync(EnrollmentCreateDto dto, CancellationToken ct)
    {
        var student = await students.GetByIdAsync(dto.StudentId, ct)
                      ?? throw new NotFoundException("Student", dto.StudentId);

        _ = await classes.GetByIdAsync(dto.ClassId, ct) ?? throw new NotFoundException("Class", dto.ClassId);

        if (student.GraduationDate is not null)
            throw new BusinessException("A graduated student cannot be enrolled in a class.");

        var active = await enrollments.GetActiveForStudentAsync(dto.StudentId, ct);
        if (active is not null)
            throw new ConflictException(
                active.ClassId == dto.ClassId
                    ? "This student is already enrolled in that class."
                    : "This student is still enrolled in another class. Withdraw them from it first.");

        var enrollment = mapper.Map<Enrollment>(dto);
        enrollment.SchoolId = currentUser.RequireSchoolId();
        enrollment.EnrolledAt = DateOnly.FromDateTime(DateTime.UtcNow);

        await enrollments.AddAsync(enrollment);
        await enrollments.SaveChangesAsync(ct);

        return await enrollments.GetDtoAsync(enrollment.Id, ct)
               ?? throw new NotFoundException("Enrollment", enrollment.Id);
    }

    public async Task WithdrawAsync(Guid enrollmentId, EnrollmentWithdrawDto dto, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(enrollmentId, ct)
                         ?? throw new NotFoundException("Enrollment", enrollmentId);

        if (enrollment.LeftAt is not null)
            throw new ConflictException("This enrollment has already been ended.");

        if (dto.LeftAt < enrollment.EnrolledAt)
            throw new BusinessException("A student cannot leave a class before they joined it.");

        enrollment.LeftAt = dto.LeftAt;
        enrollment.ExitReason = dto.ExitReason;

        enrollments.Update(enrollment);
        await enrollments.SaveChangesAsync(ct);
    }
}
