using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class ParentRepository(AppDbContext context) : Repository<Parent>(context), IParentRepository
{
    public async Task<IEnumerable<Parent>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        await _context.Parents
            .AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
}
