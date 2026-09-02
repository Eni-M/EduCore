using EduCore.Dtos.RegionalAdmin;
using EduCore.Dtos.StateAdmin;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class AccountTierEndpoints
{
    public static void MapAccountTierEndpoints(this IEndpointRouteBuilder api)
    {
        var regionalAdmins = api.MapGroup("/regional-admins")
            .RequireAuthorization(Policies.StateAdmin)
            .WithTags("Regional administrators");

        regionalAdmins.MapGet("/", async (IRegionalAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        regionalAdmins.MapGet("/{regionalAdminId:guid}",
            async (Guid regionalAdminId, IRegionalAdminService service, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(regionalAdminId, ct)));

        regionalAdmins.MapPost("/",
            async (RegionalAdminCreateDto dto, IRegionalAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/regional-admins/{created.Id}", created);
            });

        regionalAdmins.MapPut("/{regionalAdminId:guid}",
            async (Guid regionalAdminId, RegionalAdminEditDto dto, IRegionalAdminService service,
                CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(regionalAdminId, dto, ct)));

        var stateAdmins = api.MapGroup("/state-admins")
            .RequireAuthorization(Policies.StateAdmin)
            .WithTags("State administrators");

        stateAdmins.MapGet("/", async (IStateAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        stateAdmins.MapGet("/{stateAdminId:guid}",
            async (Guid stateAdminId, IStateAdminService service, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(stateAdminId, ct)));

        stateAdmins.MapPost("/",
            async (StateAdminCreateDto dto, IStateAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/state-admins/{created.Id}", created);
            });

        stateAdmins.MapPut("/{stateAdminId:guid}",
            async (Guid stateAdminId, StateAdminEditDto dto, IStateAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(stateAdminId, dto, ct)));
    }
}
