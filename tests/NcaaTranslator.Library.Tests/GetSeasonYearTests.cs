using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class GetSeasonYearTests
{
    private static Sport SportWithYear(int? seasonYear = null) => new Sport
    {
        SportName = "Football FCS",
        SportShortName = "FCS",
        SportCode = "MFB",
        SeasonYear = seasonYear
    };

    [Fact]
    public void August_UsesCurrentYear()
    {
        var year = NcaaProcessor.GetSeasonYear(SportWithYear(), new DateTime(2026, 8, 1));
        Assert.Equal(2026, year);
    }

    [Fact]
    public void January_UsesPreviousYear()
    {
        var year = NcaaProcessor.GetSeasonYear(SportWithYear(), new DateTime(2026, 1, 15));
        Assert.Equal(2025, year);
    }

    [Fact]
    public void July_UsesPreviousYear()
    {
        var year = NcaaProcessor.GetSeasonYear(SportWithYear(), new DateTime(2026, 7, 31));
        Assert.Equal(2025, year);
    }

    [Fact]
    public void SeasonYearOverride_Wins()
    {
        var year = NcaaProcessor.GetSeasonYear(SportWithYear(2024), new DateTime(2026, 8, 1));
        Assert.Equal(2024, year);
    }
}
