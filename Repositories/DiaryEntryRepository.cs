using EduCore.Data;
using EduCore.Dtos.Attendance;
using EduCore.Dtos.DiaryEntry;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class DiaryEntryRepository(AppDbContext context)
    : Repository<DiaryEntry>(context), IDiaryEntryRepository
{
    private IQueryable<DiaryEntryListDto> Project(IQueryable<DiaryEntry> entries) =>
        from d in entries
        join c in _context.Classes on d.ClassId equals c.Id
        join s in _context.Subjects on d.SubjectId equals s.Id
        join t in _context.Teachers on d.TeacherId equals t.Id
        join u in _context.Users on t.UserId equals u.Id
        select new DiaryEntryListDto(
            d.Id, d.ClassId, c.Name, d.SubjectId, s.Name,
            d.TeacherId, u.FirstName + " " + u.LastName,
            d.Date, d.Period, d.Topic);

    private static IQueryable<DiaryEntry> InRange(IQueryable<DiaryEntry> entries, DateOnly? from, DateOnly? to)
    {
        if (from is not null)
            entries = entries.Where(d => d.Date >= from);

        if (to is not null)
            entries = entries.Where(d => d.Date <= to);

        return entries;
    }

    public async Task<IEnumerable<DiaryEntryListDto>> GetByClassAsync(
        Guid classId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct) =>
        await Project(InRange(_context.DiaryEntries.AsNoTracking().Where(d => d.ClassId == classId), from, to)
                .OrderByDescending(d => d.Date).ThenBy(d => d.Period))
            .ToListAsync(ct);

    public async Task<IEnumerable<DiaryEntryListDto>> GetByTeacherAsync(
        Guid teacherId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct) =>
        await Project(InRange(_context.DiaryEntries.AsNoTracking().Where(d => d.TeacherId == teacherId), from, to)
                .OrderByDescending(d => d.Date).ThenBy(d => d.Period))
            .ToListAsync(ct);

    public async Task<DiaryEntryDetailDto?> GetDetailAsync(Guid diaryEntryId, CancellationToken ct) =>
        await (from d in _context.DiaryEntries.AsNoTracking()
               join c in _context.Classes on d.ClassId equals c.Id
               join s in _context.Subjects on d.SubjectId equals s.Id
               join t in _context.Teachers on d.TeacherId equals t.Id
               join u in _context.Users on t.UserId equals u.Id
               where d.Id == diaryEntryId
               select new DiaryEntryDetailDto(
                   d.Id, d.ClassId, c.Name, d.SubjectId, s.Name,
                   d.TeacherId, u.FirstName + " " + u.LastName,
                   d.Date, d.Period, d.Topic, d.Notes,
                   (from a in _context.Attendances
                    join e in _context.Enrollments on a.EnrollmentId equals e.Id
                    join st in _context.Students on e.StudentId equals st.Id
                    join su in _context.Users on st.UserId equals su.Id
                    where a.DiaryEntryId == d.Id
                    orderby su.LastName, su.FirstName
                    select new AttendanceListDto(
                        a.Id, a.DiaryEntryId, d.Date, s.Name, c.Name,
                        a.EnrollmentId, su.FirstName + " " + su.LastName,
                        a.IsPresent, a.Comment, a.BehaviorMark))
                   .ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> SlotTakenAsync(Guid classId, DateOnly date, int period, CancellationToken ct) =>
        await _context.DiaryEntries
            .AnyAsync(d => d.ClassId == classId && d.Date == date && d.Period == period, ct);
}
