using EduCore.Dtos.Guardian;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class GuardianEndpoints
{
    public static void MapGuardianEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/guardians")
            .RequireAuthorization(Policies.Directorate)
            .WithTags("Guardians");

        group.MapGet("/", async (IGuardianService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        group.MapGet("/search", async (string? query, IGuardianService service, CancellationToken ct) =>
            string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2
                ? Results.Ok(Array.Empty<object>())
                : Results.Ok(await service.SearchAsync(query, ct)));

        group.MapGet("/{guardianId:guid}", async (Guid guardianId, IGuardianService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(guardianId, ct)));

        group.MapPost("/", async (GuardianCreateDto dto, IGuardianService service, CancellationToken ct) =>
        {
            var guardian = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/guardians/{guardian.Id}", guardian);
        });

        group.MapPut("/{guardianId:guid}",
            async (Guid guardianId, GuardianEditDto dto, IGuardianService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(guardianId, dto, ct)));

        group.MapPut("/{guardianId:guid}/children",
            async (Guid guardianId, GuardianLinksDto dto, IGuardianService service, CancellationToken ct) =>
                Results.Ok(await service.SetChildrenAsync(guardianId, dto, ct)));

        api.MapGet("/guardian/children", async (IGuardianService service, CancellationToken ct) =>
                Results.Ok(await service.GetMyChildrenAsync(ct)))
            .RequireAuthorization(Policies.Guardian)
            .WithTags("Guardian portal");
    }
}
