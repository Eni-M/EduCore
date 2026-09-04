using EduCore.Dtos.Pagination;
using EduCore.Dtos.Teacher;
using EduCore.Dtos.TeacherSubject;
using EduCore.Helpers;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class TeacherEndpoints
{
    public static void MapTeacherEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/teachers").WithTags("Teachers");

        group.MapGet("/", async (Guid? subjectId, int? page, int? pageSize, string? search,
                ITeacherService service, CancellationToken ct) =>
                page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                    ? Results.Ok(subjectId is null
                        ? await service.GetAllAsync(ct)
                        : await service.GetBySubjectAsync(subjectId.Value, ct))
                    : Results.Ok(await service.GetPagedAsync(
                        new PaginationQueryDto(page ?? 1, pageSize ?? 20), subjectId, search, ct)))
            .RequireAuthorization(Policies.DirectorateOrTeacher);

        group.MapGet("/me", async (ITeacherService service, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(currentUser.RequireTeacherId(), ct)))
            .RequireAuthorization(Policies.Teacher);

        group.MapGet("/{teacherId:guid}", async (Guid teacherId, ITeacherService service, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(teacherId, ct)))
            .RequireAuthorization(Policies.DirectorateOrTeacher);

        group.MapPost("/", async (TeacherCreateDto dto, ITeacherService service, CancellationToken ct) =>
            {
                var teacher = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/teachers/{teacher.Id}", teacher);
            })
            .RequireAuthorization(Policies.Directorate);

        group.MapPut("/{teacherId:guid}",
                async (Guid teacherId, TeacherEditDto dto, ITeacherService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(teacherId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapGet("/{teacherId:guid}/subjects",
                async (Guid teacherId, ITeacherService service, CancellationToken ct) =>
                    Results.Ok(await service.GetSubjectsAsync(teacherId, ct)))
            .RequireAuthorization(Policies.DirectorateOrTeacher);

        group.MapPost("/subjects", async (TeacherSubjectCreateDto dto, ITeacherService service, CancellationToken ct) =>
                Results.Ok(await service.AddSubjectAsync(dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapDelete("/{teacherId:guid}/subjects/{subjectId:guid}",
                async (Guid teacherId, Guid subjectId, ITeacherService service, CancellationToken ct) =>
                {
                    await service.RemoveSubjectAsync(teacherId, subjectId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Directorate);

        group.MapGet("/{teacherId:guid}/assignments",
                async (Guid teacherId, bool? includeEnded, IClassSubjectTeacherService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByTeacherAsync(teacherId, includeEnded ?? false, ct)))
            .RequireAuthorization(Policies.DirectorateOrTeacher);
    }
}
