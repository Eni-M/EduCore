using EduCore.Dtos.Admin;
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

        var superAdminSchools = schools.RequireAuthorization(Policies.SuperAdmin);

        superAdminSchools.MapGet("/", async (bool? activeOnly, ISchoolService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(activeOnly ?? false, ct)));

        superAdminSchools.MapGet("/{schoolId:guid}", async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(schoolId, ct)));

        superAdminSchools.MapPost("/", async (SchoolCreateDto dto, ISchoolService service, CancellationToken ct) =>
        {
            var school = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/schools/{school.Id}", school);
        });

        superAdminSchools.MapPut("/{schoolId:guid}",
            async (Guid schoolId, SchoolEditDto dto, ISchoolService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(schoolId, dto, ct)));

        superAdminSchools.MapPost("/{schoolId:guid}/deactivate",
            async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            {
                await service.SetActiveAsync(schoolId, false, ct);
                return Results.NoContent();
            });

        superAdminSchools.MapPost("/{schoolId:guid}/activate",
            async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            {
                await service.SetActiveAsync(schoolId, true, ct);
                return Results.NoContent();
            });

        var admins = api.MapGroup("/admins")
            .RequireAuthorization(Policies.SuperAdmin)
            .WithTags("Admins");

        admins.MapGet("/", async (Guid? schoolId, IAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(schoolId, ct)));

        admins.MapGet("/{adminId:guid}", async (Guid adminId, IAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(adminId, ct)));

        admins.MapPost("/", async (AdminCreateDto dto, IAdminService service, CancellationToken ct) =>
        {
            var admin = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/admins/{admin.Id}", admin);
        });

        admins.MapPut("/{adminId:guid}",
            async (Guid adminId, AdminEditDto dto, IAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(adminId, dto, ct)));
    }
}
