using EduCore.Dtos.Admin;
using EduCore.Dtos.School;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

// Schools and their administrators are the SuperAdmin's, who belongs to no school.
public static class SchoolEndpoints
{
    public static void MapSchoolEndpoints(this IEndpointRouteBuilder api)
    {
        var schools = api.MapGroup("/schools").WithTags("Schools");

        // Every signed-in person's own school, named for a header or a nav --
        // not the SuperAdmin-only management surface below, which lists every
        // school on the platform. A SuperAdmin belongs to none, so they are not
        // in the policy for this one.
        schools.MapGet("/me", async (ISchoolService service, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(currentUser.RequireSchoolId(), ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        var superAdminSchools = schools.RequireAuthorization(Policies.SuperAdmin);

        // activeOnly is what a picker creating a record against a school asks for. The default
        // is every school, so the management screen below and the historical reports keep
        // seeing a switched-off school -- it still owns its past.
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

        // The same shape as the account pair in AuthEndpoints, because it is the same idea:
        // switching something off is its own action, not a field on the edit form.
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
