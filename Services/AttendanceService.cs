using AutoMapper;
using EduCore.Dtos.Attendance;
using EduCore.Exceptions;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class AttendanceService(
    IAttendanceRepository attendances,
    IDiaryEntryRepository diaryEntries,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : IAttendanceService
{
    public async Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(
        Guid studentId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);
        return await attendances.GetByStudentAsync(studentId, from, to, ct);
    }

    public async Task UpdateAsync(Guid attendanceId, AttendanceEditDto dto, CancellationToken ct)
    {
        var attendance = await attendances.GetByIdAsync(attendanceId, ct)
                         ?? throw new NotFoundException("Attendance", attendanceId);

        var entry = await diaryEntries.GetByIdAsync(attendance.DiaryEntryId, ct)
                    ?? throw new NotFoundException("Diary entry", attendance.DiaryEntryId);

        await access.EnsureCanTeachClassAsync(entry.ClassId, ct);

        if (currentUser.IsTeacher && entry.TeacherId != currentUser.TeacherId)
            throw new ForbiddenException("You can only mark attendance for lessons you taught.");

        mapper.Map(dto, attendance);

        attendances.Update(attendance);
        await attendances.SaveChangesAsync(ct);
    }
}
