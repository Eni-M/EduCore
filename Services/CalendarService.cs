using AutoMapper;
using EduCore.Dtos.Calendar;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class CalendarService(
    ICalendarEventRepository events,
    IClassRepository classes,
    IEnrollmentRepository enrollments,
    IGuardianRepository guardians,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : ICalendarService
{
    public async Task<IEnumerable<CalendarEventListDto>> GetMineAsync(
        DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        // The Directorate runs the school's calendar, so they see all of it.
        if (currentUser.IsDirectorate)
            return await events.GetForViewerAsync(true, [], [], currentUser.UserId, from, to, ct);

        var classIds = new HashSet<Guid>();
        var studentIds = new HashSet<Guid>();

        if (currentUser.IsStudent)
        {
            var studentId = currentUser.RequireStudentId();
            studentIds.Add(studentId);
            await AddActiveClassAsync(studentId, classIds, ct);
        }
        else if (currentUser.IsTeacher)
        {
            foreach (var @class in await classes.GetByTeacherAsync(currentUser.RequireTeacherId(), ct))
                classIds.Add(@class.Id);
        }
        else if (currentUser.IsGuardian)
        {
            var guardian = await guardians.GetWithChildrenAsync(currentUser.RequireGuardianId(), ct);

            foreach (var link in guardian?.Children ?? [])
            {
                studentIds.Add(link.StudentId);
                await AddActiveClassAsync(link.StudentId, classIds, ct);
            }
        }
        else
        {
            // Regional and state administrators have no single school, so no personal calendar.
            return [];
        }

        return await events.GetForViewerAsync(
            false, classIds.ToArray(), studentIds.ToArray(), currentUser.UserId, from, to, ct);
    }

    public async Task<CalendarEventListDto> CreateAsync(CalendarEventCreateDto dto, CancellationToken ct)
    {
        var schoolId = currentUser.RequireSchoolId();

        Guid? classId = null;
        Guid? studentId = null;

        switch (dto.Audience)
        {
            case CalendarAudience.School:
                if (!currentUser.IsDirectorate)
                    throw new ForbiddenException("Only the school office can post a school-wide event.");
                break;

            case CalendarAudience.Class:
                classId = dto.ClassId ?? throw new BusinessException("Choose the class this event is for.");
                _ = await classes.GetByIdAsync(classId.Value, ct)
                    ?? throw new NotFoundException("Class", classId.Value);
                // A Directorate passes; a teacher must actually teach the class.
                await access.EnsureCanTeachClassAsync(classId.Value, ct);
                break;

            case CalendarAudience.Student:
                studentId = dto.StudentId ?? throw new BusinessException("Choose the student this event is for.");
                // A Directorate passes; a teacher must teach a class the student is enrolled in.
                await access.EnsureCanViewStudentAsync(studentId.Value, ct);
                break;

            default:
                throw new BusinessException("Unknown audience.");
        }

        var now = DateTime.UtcNow;

        var ev = mapper.Map<CalendarEvent>(dto);
        ev.ClassId = classId;
        ev.StudentId = studentId;
        ev.SchoolId = schoolId;
        ev.CreatedByUserId = currentUser.UserId;
        ev.CreatedAt = now;
        ev.UpdatedAt = now;

        await events.AddAsync(ev);
        await events.SaveChangesAsync(ct);

        return await SingleAsync(ev.Id, ev.Date, ct);
    }

    public async Task<CalendarEventListDto> UpdateAsync(
        Guid eventId, CalendarEventEditDto dto, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(eventId, ct) ?? throw new NotFoundException("Calendar event", eventId);

        EnsureOwnerOrDirectorate(ev);

        mapper.Map(dto, ev);
        ev.UpdatedAt = DateTime.UtcNow;

        events.Update(ev);
        await events.SaveChangesAsync(ct);

        return await SingleAsync(eventId, ev.Date, ct);
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(eventId, ct) ?? throw new NotFoundException("Calendar event", eventId);

        EnsureOwnerOrDirectorate(ev);

        events.Remove(ev);
        await events.SaveChangesAsync(ct);
    }

    private async Task AddActiveClassAsync(Guid studentId, HashSet<Guid> classIds, CancellationToken ct)
    {
        var enrollment = await enrollments.GetActiveForStudentAsync(studentId, ct);

        if (enrollment is not null)
            classIds.Add(enrollment.ClassId);
    }

    private void EnsureOwnerOrDirectorate(CalendarEvent ev)
    {
        if (!currentUser.IsDirectorate && ev.CreatedByUserId != currentUser.UserId)
            throw new ForbiddenException("You can only change events you created.");
    }

    // Reads a single event back through the list projection (which resolves class/student/author
    // names). seeAll is safe here: the result is filtered to the one id, narrowed to its own day.
    private async Task<CalendarEventListDto> SingleAsync(Guid eventId, DateOnly date, CancellationToken ct) =>
        (await events.GetForViewerAsync(true, [], [], currentUser.UserId, date, date, ct))
            .FirstOrDefault(e => e.Id == eventId)
        ?? throw new NotFoundException("Calendar event", eventId);
}
