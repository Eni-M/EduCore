using EduCore.Dtos.Class;
using EduCore.Dtos.ClassSubjectTeacher;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class ClassEndpoints
{
    public static void MapClassEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/classes").WithTags("Classes");

        group.MapGet("/", async (Guid? academicYearId, IClassService service, CancellationToken ct) =>
                Results.Ok(await service.GetAllAsync(academicYearId, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);

        group.MapGet("/mine", async (IClassService service, CancellationToken ct) =>
                Results.Ok(await service.GetMineAsync(ct)))
            .RequireAuthorization(Policies.Teacher);

        group.MapGet("/{classId:guid}", async (Guid classId, IClassService service, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(classId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPost("/", async (ClassCreateDto dto, IClassService service, CancellationToken ct) =>
            {
                var @class = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/classes/{@class.Id}", @class);
            })
            .RequireAuthorization(Policies.Admin);

        group.MapPut("/{classId:guid}",
                async (Guid classId, ClassEditDto dto, IClassService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(classId, dto, ct)))
            .RequireAuthorization(Policies.Admin);

        // Its own route because passing null here clears the assignment.
        group.MapPut("/{classId:guid}/homeroom-teacher",
                async (Guid classId, ClassAssignHomeroomTeacherDto dto, IClassService service, CancellationToken ct) =>
                    Results.Ok(await service.AssignHomeroomTeacherAsync(classId, dto, ct)))
            .RequireAuthorization(Policies.Admin);

        group.MapGet("/{classId:guid}/students",
                async (Guid classId, IStudentService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByClassAsync(classId, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);

        group.MapGet("/{classId:guid}/enrollments",
                async (Guid classId, bool? includeLeft, IEnrollmentService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByClassAsync(classId, includeLeft ?? false, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);

        group.MapGet("/{classId:guid}/subject-teachers",
                async (Guid classId, bool? includeEnded, IClassSubjectTeacherService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByClassAsync(classId, includeEnded ?? false, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{classId:guid}/diary-entries",
                async (Guid classId, DateOnly? from, DateOnly? to, IDiaryEntryService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByClassAsync(classId, from, to, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{classId:guid}/marks",
                async (Guid classId, Guid? subjectId, Guid? gradingPeriodId, IMarkService service,
                    CancellationToken ct) =>
                    Results.Ok(await service.GetByClassAsync(classId, subjectId, gradingPeriodId, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);

        var assignments = api.MapGroup("/class-subject-teachers")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Teaching assignments");

        assignments.MapPost("/",
            async (ClassSubjectTeacherCreateDto dto, IClassSubjectTeacherService service, CancellationToken ct) =>
                Results.Ok(await service.CreateAsync(dto, ct)));

        // Ended, not deleted, so past lessons and marks keep pointing at a real assignment.
        assignments.MapPost("/{assignmentId:guid}/end",
            async (Guid assignmentId, ClassSubjectTeacherEndDto dto, IClassSubjectTeacherService service,
                CancellationToken ct) =>
            {
                await service.EndAsync(assignmentId, dto, ct);
                return Results.NoContent();
            });
    }
}
