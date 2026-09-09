using EduCore.Dtos.Public;

namespace EduCore.Repositories.Interfaces;

public interface IPlatformStatsRepository
{
    Task<PlatformStatsDto> GetAsync(CancellationToken ct);
}
