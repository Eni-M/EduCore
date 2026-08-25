using EduCore.Dtos.Attendance;
using EduCore.Models;

namespace EduCore.Repositories.Interfaces;

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(
        Guid studentId, DateOnly? from, DateOnly? to, CancellationToken ct);

}
