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
            TypedResults.Ok(await service.GetAllAsync(ct)));

        // What a class of this grade may be assigned a teacher for. The assignment picker
        // reads this so it offers exactly the subjects the API will accept.
        group.MapGet("/grade/{grade:int}", async (int grade, ICurriculumService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetSubjectsForGradeAsync(grade, ct)));

        group.MapPost("/", async (GradeCurriculumAddDto dto, ICurriculumService service, CancellationToken ct) =>
            TypedResults.Ok(await service.AddAsync(dto, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ICurriculumService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(id, ct);
            return TypedResults.NoContent();
        });
    }
}
