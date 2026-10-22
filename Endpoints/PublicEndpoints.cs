using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

// The only endpoints an unauthenticated visitor can reach besides signing in. Everything here
// is whole-platform and anonymous by construction; nothing that can be narrowed to a school,
// a class or a person belongs in this group.
public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/public").WithTags("Public");

        group.MapGet("/stats", async (IPlatformStatsService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetAsync(ct)));
    }
}
