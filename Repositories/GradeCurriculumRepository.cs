using EduCore.Data;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class GradeCurriculumRepository(AppDbContext context)
    : Repository<GradeCurriculum>(context), IGradeCurriculumRepository
{
    public async Task<IReadOnlyList<Subject>> GetSubjectsForGradeAsync(int grade, CancellationToken ct) =>
        await _context.GradeCurricula
            .AsNoTracking()
            .Where(gc => gc.Grade == grade)
            .Select(gc => gc.Subject)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GradeCurriculum>> GetAllAsync(CancellationToken ct) =>
        await _context.GradeCurricula
            .AsNoTracking()
            .Include(gc => gc.Subject)
            .OrderBy(gc => gc.Grade)
            .ThenBy(gc => gc.Subject.Name)
            .ToListAsync(ct);

    public async Task<GradeCurriculum?> GetAsync(int grade, Guid subjectId, CancellationToken ct) =>
        await _context.GradeCurricula
            .FirstOrDefaultAsync(gc => gc.Grade == grade && gc.SubjectId == subjectId, ct);
}
