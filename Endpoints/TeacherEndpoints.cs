using EduCore.Dtos.Teacher;
using EduCore.Dtos.TeacherSubject;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class TeacherEndpoints
{
    public static void MapTeacherEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/teachers").WithTags("Teachers");

        group.MapGet("/", async (Guid? subjectId, ITeacherService service, CancellationToken ct) =>
                Results.Ok(subjectId is null
                    ? await service.GetAllAsync(ct)
                    : await service.GetBySubjectAsync(subjectId.Value, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);

        group.MapGet("/me", async (ITeacherService service, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(currentUser.RequireTeacherId(), ct)))
            .RequireAuthorization(Policies.Teacher);

        group.MapGet("/{teacherId:guid}", async (Guid teacherId, ITeacherService service, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(teacherId, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);

        group.MapPost("/", async (TeacherCreateDto dto, ITeacherService service, CancellationToken ct) =>
            {
                var teacher = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/teachers/{teacher.Id}", teacher);
            })
            .RequireAuthorization(Policies.Admin);

        group.MapPut("/{teacherId:guid}",
                async (Guid teacherId, TeacherEditDto dto, ITeacherService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(teacherId, dto, ct)))
            .RequireAuthorization(Policies.Admin);

        group.MapGet("/{teacherId:guid}/subjects",
                async (Guid teacherId, ITeacherService service, CancellationToken ct) =>
                    Results.Ok(await service.GetSubjectsAsync(teacherId, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);

        group.MapPost("/subjects", async (TeacherSubjectCreateDto dto, ITeacherService service, CancellationToken ct) =>
                Results.Ok(await service.AddSubjectAsync(dto, ct)))
            .RequireAuthorization(Policies.Admin);

        group.MapDelete("/{teacherId:guid}/subjects/{subjectId:guid}",
                async (Guid teacherId, Guid subjectId, ITeacherService service, CancellationToken ct) =>
                {
                    await service.RemoveSubjectAsync(teacherId, subjectId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Admin);

        group.MapGet("/{teacherId:guid}/assignments",
                async (Guid teacherId, bool? includeEnded, IClassSubjectTeacherService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByTeacherAsync(teacherId, includeEnded ?? false, ct)))
            .RequireAuthorization(Policies.AdminOrTeacher);
    }
}
