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
        var schools = api.MapGroup("/schools")
            .RequireAuthorization(Policies.SuperAdmin)
            .WithTags("Schools");

        schools.MapGet("/", async (ISchoolService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

    

        schools.MapGet("/{schoolId:guid}", async (Guid schoolId, ISchoolService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(schoolId, ct)));

        schools.MapPost("/", async (SchoolCreateDto dto, ISchoolService service, CancellationToken ct) =>
        {
            var school = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/schools/{school.Id}", school);
        });

        schools.MapPut("/{schoolId:guid}",
            async (Guid schoolId, SchoolEditDto dto, ISchoolService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(schoolId, dto, ct)));

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
