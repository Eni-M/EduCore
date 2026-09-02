using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class SchoolingHistoryRepository(AppDbContext context)
    : Repository<SchoolingHistory>(context), ISchoolingHistoryRepository
{
    public async Task<IEnumerable<SchoolingHistory>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        await _context.SchoolingHistories.AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.FromDate)
            .ToListAsync(ct);
}
