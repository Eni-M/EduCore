using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class StudentDocumentRepository(AppDbContext context)
    : Repository<StudentDocument>(context), IStudentDocumentRepository
{
    public async Task<IEnumerable<StudentDocument>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        await _context.StudentDocuments.AsNoTracking()
            .Where(d => d.StudentId == studentId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);
}
