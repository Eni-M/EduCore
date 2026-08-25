using EduCore.Dtos.Attendance;

namespace EduCore.Services.Interfaces;

public interface IAttendanceService
{
    Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(
        Guid studentId, DateOnly? from, DateOnly? to, CancellationToken ct);

    Task UpdateAsync(Guid attendanceId, AttendanceEditDto dto, CancellationToken ct);
}
