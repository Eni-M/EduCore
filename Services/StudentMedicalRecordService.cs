using AutoMapper;
using EduCore.Dtos.Medical;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class StudentMedicalRecordService(
    IStudentMedicalRecordRepository records,
    IStudentRepository students,
    IGuardianRepository guardians,
    INotificationService notifications,
    IAccessGuard access,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IMapper mapper) : IStudentMedicalRecordService
{
    public async Task<StudentMedicalRecordDto?> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentMedicalAsync(studentId, ct);

        var record = await records.GetByStudentAsync(studentId, ct);

        // Health data about a child: every view is recorded, including views of an empty record.
        await audit.RecordAsync($"Read StudentMedicalRecord of Student ({studentId})", ct, schoolId: record?.SchoolId);

        return record is null ? null : mapper.Map<StudentMedicalRecordDto>(record);
    }

    public async Task<StudentMedicalRecordDto> UpsertAsync(
        Guid studentId, StudentMedicalRecordUpsertDto dto, CancellationToken ct)
    {
        // Directorate-only at the endpoint; the school-filtered read keeps it to their own school.
        var student = await students.GetByIdAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        var record = await StageAsync(studentId, student.SchoolId, dto, ct);
        await records.SaveChangesAsync(ct);
        return mapper.Map<StudentMedicalRecordDto>(record);
    }

    public async Task<StudentMedicalRecordDto> UpsertByGuardianAsync(
        Guid studentId, StudentMedicalRecordUpsertDto dto, CancellationToken ct)
    {
        var guardianId = currentUser.RequireGuardianId();
        if (!await guardians.LinkExistsAsync(guardianId, studentId, ct))
            throw new ForbiddenException("You can only update your own child's medical record.");

        var student = await students.GetWithGuardiansAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        var record = await StageAsync(studentId, student.SchoolId, dto, ct);

        // Surface the change to the school's directorate for review -- in the same save as the
        // change, so the record is never updated without the office being told.
        var studentName = $"{student.User.FirstName} {student.User.LastName}";
        await notifications.StageForRoleAsync(
            student.SchoolId,
            UserRole.Directorate,
            $"A guardian updated the medical record for {studentName}.",
            $"/students/{studentId}",
            ct);

        await records.SaveChangesAsync(ct);
        return mapper.Map<StudentMedicalRecordDto>(record);
    }

    // Adds or updates the record without saving; the caller saves, together with anything else
    // that belongs to the same change.
    private async Task<StudentMedicalRecord> StageAsync(
        Guid studentId, Guid schoolId, StudentMedicalRecordUpsertDto dto, CancellationToken ct)
    {
        var record = await records.GetByStudentAsync(studentId, ct);

        if (record is null)
        {
            record = mapper.Map<StudentMedicalRecord>(dto);
            record.StudentId = studentId;
            record.SchoolId = schoolId;
            record.UpdatedAt = DateTime.UtcNow;
            await records.AddAsync(record);
        }
        else
        {
            mapper.Map(dto, record);
            record.UpdatedAt = DateTime.UtcNow;
            records.Update(record);
        }

        return record;
    }
}
