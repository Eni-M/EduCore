using EduCore.Dtos.Attendance;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAttendanceRepository : IRepository<Attendance>
{
    // Lessons recorded and lessons present, in one aggregate -- for screens that show a rate and
    // would otherwise load every attendance row the student has to count them.
    Task<(int Recorded, int Present)> CountForStudentAsync(Guid studentId, CancellationToken ct);

    Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(
        Guid studentId, DateOnly? from, DateOnly? to, CancellationToken ct);

}
