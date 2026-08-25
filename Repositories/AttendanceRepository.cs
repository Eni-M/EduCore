using EduCore.Data;
using EduCore.Dtos.Attendance;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Repositories;

public class AttendanceRepository(AppDbContext context)
    : Repository<Attendance>(context), IAttendanceRepository
{
    public async Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(
        Guid studentId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct)
    {
        var query =
            from a in _context.Attendances.AsNoTracking()
            join d in _context.DiaryEntries on a.DiaryEntryId equals d.Id
            join s in _context.Subjects on d.SubjectId equals s.Id
            join c in _context.Classes on a.ClassId equals c.Id
            join e in _context.Enrollments on a.EnrollmentId equals e.Id
            join st in _context.Students on e.StudentId equals st.Id
            join u in _context.Users on st.UserId equals u.Id
            where e.StudentId == studentId
                  && (fromDate == null || d.Date >= fromDate)
                  && (toDate == null || d.Date <= toDate)
            orderby d.Date descending, d.Period
            select new AttendanceListDto(
                a.Id,
                a.DiaryEntryId,
                d.Date,
                s.Name,
                c.Name,
                a.EnrollmentId,
                u.FirstName + " " + u.LastName,
                a.IsPresent,
                a.Comment,
                a.BehaviorMark);

        return await query.ToListAsync(ct);
    }
}
