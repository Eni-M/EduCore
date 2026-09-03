using EduCore.Data;
using EduCore.Dtos.Class;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class ClassRepository(AppDbContext context) : Repository<Class>(context), IClassRepository
{
    private IQueryable<ClassListDto> Project(IQueryable<Class> classes) =>
        from c in classes
        join s in _context.Schools on c.SchoolId equals s.Id
        join y in _context.AcademicYears on c.AcademicYearId equals y.Id
        join t in _context.Teachers on c.HomeroomTeacherId equals (Guid?)t.Id into teacherMatch
        from t in teacherMatch.DefaultIfEmpty()
        join u in _context.Users on t.UserId equals u.Id into userMatch
        from u in userMatch.DefaultIfEmpty()
        join sh in _context.SchoolShifts on c.ShiftId equals (Guid?)sh.Id into shiftMatch
        from sh in shiftMatch.DefaultIfEmpty()
        select new ClassListDto(
            c.Id,
            c.Name,
            c.Grade,
            c.SchoolId,
            s.Name,
            c.AcademicYearId,
            y.Name,
            c.HomeroomTeacherId,
            u == null ? null : u.FirstName + " " + u.LastName,
            c.Capacity,
            c.ShiftId,
            sh == null ? null : sh.Name);

    public async Task<IEnumerable<ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct)
    {
        var classes = _context.Classes.AsNoTracking();

        if (academicYearId is not null)
            classes = classes.Where(c => c.AcademicYearId == academicYearId);

        return await Project(classes.OrderBy(c => c.Grade).ThenBy(c => c.Name)).ToListAsync(ct);
    }

    public async Task<ClassListDto?> GetDtoAsync(Guid classId, CancellationToken ct) =>
        await Project(_context.Classes.AsNoTracking().Where(c => c.Id == classId))
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<ClassListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var taughtClassIds = _context.ClassSubjectTeachers
            .Where(cst => cst.TeacherId == teacherId && cst.EndedOn == null)
            .Select(cst => cst.ClassId);

        var classes = _context.Classes
            .AsNoTracking()
            .Where(c => taughtClassIds.Contains(c.Id) || c.HomeroomTeacherId == teacherId);

        return await Project(classes.OrderBy(c => c.Grade).ThenBy(c => c.Name)).ToListAsync(ct);
    }

    public async Task<bool> NameExistsInYearAsync(
        Guid academicYearId,
        string name,
        Guid? excludeClassId,
        CancellationToken ct) =>
        await _context.Classes.AnyAsync(
            c => c.AcademicYearId == academicYearId
                 && c.Name.ToLower() == name.ToLower()
                 && (excludeClassId == null || c.Id != excludeClassId),
            ct);

    public async Task<IEnumerable<Class>> GetAllWithHomeroomTeacherAsync(CancellationToken ct) =>
        await _context.Classes
            .AsNoTracking()
            .Where(c => c.HomeroomTeacherId != null)
            .ToListAsync(ct);
}
