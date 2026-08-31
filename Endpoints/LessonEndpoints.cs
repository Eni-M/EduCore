using EduCore.Dtos.Attendance;
using EduCore.Dtos.DiaryEntry;
using EduCore.Dtos.Enrollment;
using EduCore.Dtos.Mark;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class LessonEndpoints
{
    public static void MapLessonEndpoints(this IEndpointRouteBuilder api)
    {
        var enrollments = api.MapGroup("/enrollments")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Enrollments");

        enrollments.MapPost("/", async (EnrollmentCreateDto dto, IEnrollmentService service, CancellationToken ct) =>
            Results.Ok(await service.EnrollAsync(dto, ct)));

        enrollments.MapPost("/{enrollmentId:guid}/withdraw",
            async (Guid enrollmentId, EnrollmentWithdrawDto dto, IEnrollmentService service, CancellationToken ct) =>
            {
                await service.WithdrawAsync(enrollmentId, dto, ct);
                return Results.NoContent();
            });

        var diary = api.MapGroup("/diary-entries").WithTags("Diary");

        diary.MapGet("/mine", async (DateOnly? from, DateOnly? to, IDiaryEntryService service, CancellationToken ct) =>
                Results.Ok(await service.GetMineAsync(from, to, ct)))
            .RequireAuthorization(Policies.Teacher);

        diary.MapGet("/{diaryEntryId:guid}",
                async (Guid diaryEntryId, IDiaryEntryService service, CancellationToken ct) =>
                    Results.Ok(await service.GetDetailAsync(diaryEntryId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        diary.MapPost("/", async (DiaryEntryCreateDto dto, IDiaryEntryService service, CancellationToken ct) =>
            {
                var entry = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/diary-entries/{entry.Id}", entry);
            })
            .RequireAuthorization(Policies.AdminOrTeacher);

        diary.MapPut("/{diaryEntryId:guid}",
                async (Guid diaryEntryId, DiaryEntryEditDto dto, IDiaryEntryService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(diaryEntryId, dto, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);

        var attendance = api.MapGroup("/attendance")
            .RequireAuthorization(Policies.AdminOrTeacher)
            .WithTags("Attendance");

        attendance.MapPut("/{attendanceId:guid}",
            async (Guid attendanceId, AttendanceEditDto dto, IAttendanceService service, CancellationToken ct) =>
            {
                await service.UpdateAsync(attendanceId, dto, ct);
                return Results.NoContent();
            });

        var marks = api.MapGroup("/marks")
            .RequireAuthorization(Policies.AdminOrTeacher)
            .WithTags("Marks");

        marks.MapPost("/", async (MarkUpsertDto dto, IMarkService service, CancellationToken ct) =>
            Results.Ok(await service.UpsertAsync(dto, ct)));

        api.MapGet("/audit-logs", async (int? page, int? pageSize, Guid? userId, DateTime? from, DateTime? to,
                    IAuditLogService service, CancellationToken ct) =>
                Results.Ok(await service.GetPagedAsync(
                    new Dtos.Pagination.PaginationQueryDto(page ?? 1, pageSize ?? 20), userId, from, to, ct)))
            .RequireAuthorization(Policies.SuperAdmin)
            .WithTags("Audit");
    }
}
