using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class StudentMedicalRecordRepository(AppDbContext context)
    : Repository<StudentMedicalRecord>(context), IStudentMedicalRecordRepository
{
    // Tracked on purpose: the upsert path mutates this instance and saves it, and the rowversion
    // concurrency token is a shadow property that only a tracked entity carries.
    public async Task<StudentMedicalRecord?> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        await _context.StudentMedicalRecords.FirstOrDefaultAsync(m => m.StudentId == studentId, ct);
}
