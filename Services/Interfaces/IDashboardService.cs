using EduCore.Dtos.Dashboard;

namespace EduCore.Services.Interfaces;

public interface IDashboardService
{
    // The signed-in user's dashboard. There is no role parameter on purpose: the role comes from
    // the token, so a caller cannot ask for someone else's view of the platform.
    Task<DashboardDto> GetAsync(CancellationToken ct);
}
