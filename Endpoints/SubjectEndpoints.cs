using EduCore.Dtos.Subject;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class SubjectEndpoints
{
    public static void MapSubjectEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/subjects").WithTags("Subjects");

        group.MapGet("/", async (int? page, int? pageSize, string? search,
                ISubjectService service, CancellationToken ct) =>
                page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                    ? Results.Ok(await service.GetAllAsync(ct))
                    : Results.Ok(await service.GetPagedAsync(
                    new Dtos.Pagination.PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{subjectId:guid}", async (Guid subjectId, ISubjectService service, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(subjectId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPost("/", async (SubjectCreateDto dto, ISubjectService service, CancellationToken ct) =>
            {
                var subject = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/subjects/{subject.Id}", subject);
            })
            .RequireAuthorization(Policies.Directorate);

        group.MapPut("/{subjectId:guid}",
                async (Guid subjectId, SubjectEditDto dto, ISubjectService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(subjectId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapDelete("/{subjectId:guid}", async (Guid subjectId, ISubjectService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(subjectId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.Directorate);
    }
}
