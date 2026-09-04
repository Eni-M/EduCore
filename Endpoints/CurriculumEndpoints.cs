using EduCore.Dtos.Curriculum;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class CurriculumEndpoints
{
    public static void MapCurriculumEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/curriculum")
            .RequireAuthorization(Policies.Directorate)
            .WithTags("Curriculum");

        group.MapGet("/", async (ICurriculumService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        group.MapPost("/", async (GradeCurriculumAddDto dto, ICurriculumService service, CancellationToken ct) =>
            Results.Ok(await service.AddAsync(dto, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ICurriculumService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(id, ct);
            return Results.NoContent();
        });
    }
}
