using AutoMapper;
using EduCore.Dtos.Medical;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class StudentMedicalRecordService(
    IStudentMedicalRecordRepository records,
    IStudentRepository students,
    IAccessGuard access,
    IMapper mapper) : IStudentMedicalRecordService
{
    public async Task<StudentMedicalRecordDto?> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentMedicalAsync(studentId, ct);

        var record = await records.GetByStudentAsync(studentId, ct);

        return record is null ? null : mapper.Map<StudentMedicalRecordDto>(record);
    }

    public async Task<StudentMedicalRecordDto> UpsertAsync(
        Guid studentId, StudentMedicalRecordUpsertDto dto, CancellationToken ct)
    {
        // Directorate-only at the endpoint; the school-filtered read keeps it to their own school.
        var student = await students.GetByIdAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        var record = await records.GetByStudentAsync(studentId, ct);

        if (record is null)
        {
            record = mapper.Map<StudentMedicalRecord>(dto);
            record.StudentId = studentId;
            record.SchoolId = student.SchoolId;
            record.UpdatedAt = DateTime.UtcNow;
            await records.AddAsync(record);
        }
        else
        {
            mapper.Map(dto, record);
            record.UpdatedAt = DateTime.UtcNow;
            records.Update(record);
        }

        await records.SaveChangesAsync(ct);

        return mapper.Map<StudentMedicalRecordDto>(record);
    }
}
