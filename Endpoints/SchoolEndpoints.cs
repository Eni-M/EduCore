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

        schools.MapGet("/me", async (ISchoolService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetMineAsync(ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        // MapGroup("") makes this a real nested group. Calling RequireAuthorization on `schools`
        // directly would add the convention to the whole group, including "/me" above.
        var managedSchools = schools.MapGroup("").RequireAuthorization(Policies.RegionalOrState);

        managedSchools.MapGet("/", async (bool? activeOnly, int? page, int? pageSize, string? search,
                ISchoolService service, CancellationToken ct) =>
            page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                ? Results.Ok(await service.GetAllAsync(activeOnly ?? false, ct))
                : Results.Ok(await service.GetPagedAsync(
                    activeOnly ?? false, new Dtos.Pagination.PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)));

        managedSchools.MapGet("/{schoolId:guid}", async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetByIdAsync(schoolId, ct)));

        managedSchools.MapPost("/", async (SchoolCreateDto dto, ISchoolService service, CancellationToken ct) =>
        {
            var school = await service.CreateAsync(dto, ct);
            return TypedResults.Created($"/api/schools/{school.Id}", school);
        });

        managedSchools.MapPut("/{schoolId:guid}",
            async (Guid schoolId, SchoolEditDto dto, ISchoolService service, CancellationToken ct) =>
                TypedResults.Ok(await service.UpdateAsync(schoolId, dto, ct)));

        managedSchools.MapPost("/{schoolId:guid}/deactivate",
            async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            {
                await service.SetActiveAsync(schoolId, false, ct);
                return TypedResults.NoContent();
            });

        managedSchools.MapPost("/{schoolId:guid}/activate",
            async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            {
                await service.SetActiveAsync(schoolId, true, ct);
                return TypedResults.NoContent();
            });

        var directorates = api.MapGroup("/directorates")
            .RequireAuthorization(Policies.RegionalAdmin)
            .WithTags("Directorates");

        directorates.MapGet("/", async (int? page, int? pageSize, string? search,
                IDirectorateService service, CancellationToken ct) =>
            page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                ? Results.Ok(await service.GetAllAsync(ct))
                : Results.Ok(await service.GetPagedAsync(
                    new Dtos.Pagination.PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)));

        directorates.MapGet("/{directorateId:guid}",
            async (Guid directorateId, IDirectorateService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetByIdAsync(directorateId, ct)));

        directorates.MapPost("/", async (DirectorateCreateDto dto, IDirectorateService service, CancellationToken ct) =>
        {
            var directorate = await service.CreateAsync(dto, ct);
            return TypedResults.Created($"/api/directorates/{directorate.Id}", directorate);
        });

        directorates.MapPut("/{directorateId:guid}",
            async (Guid directorateId, DirectorateEditDto dto, IDirectorateService service, CancellationToken ct) =>
                TypedResults.Ok(await service.UpdateAsync(directorateId, dto, ct)));
    }
}
