using EduCore.Localization;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        // One endpoint for all six roles, and no role in the route. The server reads the role from
        // the token and decides what to return, so there is no URL a curious user can try in order
        // to see the ministry's view of the platform.
        //
        // No policy beyond authentication for the same reason: every role has a dashboard, and the
        // data inside it is already scoped by the services this composes.
        // Built in English, then translated into the request's language on the way out.
        group.MapGet("/dashboard", async (IDashboardService service, IMessageLocalizer localizer, CancellationToken ct) =>
            TypedResults.Ok(DashboardText.Translate(await service.GetAsync(ct), localizer.Translate)));
    }
}
