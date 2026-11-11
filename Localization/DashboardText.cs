using EduCore.Dtos.Dashboard;

namespace EduCore.Localization;

// The dashboard service writes its labels, captions and alerts in English. This puts every piece
// of text through the message catalogue on the way out, so the reader sees their own language;
// names, numbers and links have no catalogue entry and pass through unchanged.
public static class DashboardText
{
    public static DashboardDto Translate(DashboardDto dto, Func<string, string> translate)
    {
        string? T(string? text) => text is null ? null : translate(text);

        return dto with
        {
            Context = T(dto.Context),
            Stats = dto.Stats.Select(s => s with { Label = translate(s.Label), Value = translate(s.Value), Caption = T(s.Caption) }).ToList(),
            NeedsAttention = dto.NeedsAttention.Select(a => a with { Title = translate(a.Title), Detail = translate(a.Detail) }).ToList(),
            Recent = dto.Recent.Select(r => r with { Title = translate(r.Title), Detail = translate(r.Detail) }).ToList(),
        };
    }
}
