using EduCore.Dtos.Public;

namespace EduCore.Services.Interfaces;

public interface IPlatformStatsService
{
    Task<PlatformStatsDto> GetAsync(CancellationToken ct);
}
