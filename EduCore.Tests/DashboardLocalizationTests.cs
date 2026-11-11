using EduCore.Dtos.Dashboard;
using EduCore.Localization;
using Xunit;

namespace EduCore.Tests;

// The dashboard is built in English on the server; the endpoint translates it into the reader's
// language on the way out, the same way error messages are.
public class DashboardLocalizationTests
{
    private static readonly MessageLocalizer Localizer = new();

    private static DashboardDto Sample() => new(
        "Directorate",
        "Your school",
        [
            new DashboardStatDto("Active students", "45", "this period", DashboardTone.Neutral),
            new DashboardStatDto("Lessons today", "0", "Wednesday", DashboardTone.Neutral),
        ],
        [new DashboardAlertDto(DashboardTone.Warning, "3 classes have no homeroom teacher",
            "A class without a homeroom teacher has nobody responsible for its register.", "/classes")],
        [],
        [new DashboardEntryDto("Ana Krasniqi · Mathematics", "4 · Semestri I", "/my-marks")]);

    [Fact]
    public void Labels_captions_and_alerts_come_back_in_Albanian()
    {
        var dto = DashboardText.Translate(Sample(), m => Localizer.Translate(m, "sq"));

        Assert.Equal("Shkolla juaj", dto.Context);
        Assert.Equal("Nxënës aktivë", dto.Stats[0].Label);
        Assert.Equal("këtë periudhë", dto.Stats[0].Caption);
        Assert.Equal("e mërkurë", dto.Stats[1].Caption);
        Assert.Equal("3 klasa nuk kanë kujdestar", dto.NeedsAttention[0].Title);
    }

    [Fact]
    public void Names_and_numbers_pass_through_untouched()
    {
        var dto = DashboardText.Translate(Sample(), m => Localizer.Translate(m, "sr"));

        Assert.Equal("45", dto.Stats[0].Value);
        Assert.Equal("Ana Krasniqi · Mathematics", dto.Recent[0].Title);
        Assert.Equal("/classes", dto.NeedsAttention[0].Link);
    }

    [Fact]
    public void English_is_left_as_it_is()
    {
        var dto = DashboardText.Translate(Sample(), m => Localizer.Translate(m, "en"));

        Assert.Equal("Your school", dto.Context);
        Assert.Equal("Active students", dto.Stats[0].Label);
    }
}
