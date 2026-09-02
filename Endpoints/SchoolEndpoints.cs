using EduCore.Dtos.Directorate;
using EduCore.Dtos.School;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class SchoolEndpoints
{
    public static void MapSchoolEndpoints(this IEndpointRouteBuilder api)
    {
        var schools = api.MapGroup("/schools").WithTags("Schools");

        schools.MapGet("/me", async (ISchoolService service, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(currentUser.RequireSchoolId(), ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        var managedSchools = schools.RequireAuthorization(Policies.RegionalOrState);

        managedSchools.MapGet("/", async (bool? activeOnly, ISchoolService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(activeOnly ?? false, ct)));

        managedSchools.MapGet("/{schoolId:guid}", async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(schoolId, ct)));

        managedSchools.MapPost("/", async (SchoolCreateDto dto, ISchoolService service, CancellationToken ct) =>
        {
            var school = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/schools/{school.Id}", school);
        });

        managedSchools.MapPut("/{schoolId:guid}",
            async (Guid schoolId, SchoolEditDto dto, ISchoolService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(schoolId, dto, ct)));

        managedSchools.MapPost("/{schoolId:guid}/deactivate",
            async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            {
                await service.SetActiveAsync(schoolId, false, ct);
                return Results.NoContent();
            });

        managedSchools.MapPost("/{schoolId:guid}/activate",
            async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            {
                await service.SetActiveAsync(schoolId, true, ct);
                return Results.NoContent();
            });

        var directorates = api.MapGroup("/directorates")
            .RequireAuthorization(Policies.RegionalAdmin)
            .WithTags("Directorates");

        directorates.MapGet("/", async (IDirectorateService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        directorates.MapGet("/{directorateId:guid}",
            async (Guid directorateId, IDirectorateService service, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(directorateId, ct)));

        directorates.MapPost("/", async (DirectorateCreateDto dto, IDirectorateService service, CancellationToken ct) =>
        {
            var directorate = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/directorates/{directorate.Id}", directorate);
        });

        directorates.MapPut("/{directorateId:guid}",
            async (Guid directorateId, DirectorateEditDto dto, IDirectorateService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(directorateId, dto, ct)));
    }
}
