using System.Globalization;
using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class UrlBuilderTests
{
    [Fact]
    public void GetUrl_UsesSportWeekDirectly()
    {
        var sport = new Sport
        {
            SportName = "Football FCS",
            SportShortName = "FCS",
            SportCode = "MFB",
            Division = 12,
            Week = 2,
            SeasonYear = 2025
        };

        var url = NcaaProcessor.GetUrl(sport);

        Assert.Contains("\"week\":2", url);
        Assert.Contains("\"contestDate\":null", url);
        Assert.Contains(NcaaProcessor.NcaaContestsGraphQlUrl, url);
    }

    [Fact]
    public void GetUrl_SameSportCode_DifferentWeeks_ProduceDifferentUrls()
    {
        var fcs = new Sport
        {
            SportName = "Football FCS",
            SportShortName = "FCS",
            SportCode = "MFB",
            Division = 12,
            Week = 2,
            SeasonYear = 2025
        };
        var fbs = new Sport
        {
            SportName = "Football FBS",
            SportShortName = "FBS",
            SportCode = "MFB",
            Division = 11,
            Week = 5,
            SeasonYear = 2025
        };

        var fcsUrl = NcaaProcessor.GetUrl(fcs);
        var fbsUrl = NcaaProcessor.GetUrl(fbs);

        Assert.Contains("\"week\":2", fcsUrl);
        Assert.Contains("\"week\":5", fbsUrl);
        Assert.DoesNotContain("\"week\":5", fcsUrl);
        Assert.DoesNotContain("\"week\":2", fbsUrl);
        Assert.NotEqual(fcsUrl, fbsUrl);
    }

    [Fact]
    public void GetUrl_WhenWeekIsNull_UsesTodayAndNullWeek()
    {
        var sport = new Sport
        {
            SportName = "Volleyball",
            SportShortName = "WVB",
            SportCode = "WVB",
            Division = 1,
            Week = null,
            SeasonYear = 2025
        };

        var url = NcaaProcessor.GetUrl(sport);
        var today = DateTime.Now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

        Assert.Contains("\"week\":null", url);
        Assert.Contains($"\"contestDate\":\"{today}\"", url);
    }

    [Fact]
    public void GetUrl_WhenWeekIsNull_FormatsContestDateWithInvariantCulture()
    {
        var original = CultureInfo.CurrentCulture;
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;
        try
        {
            var german = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = german;
            CultureInfo.DefaultThreadCurrentCulture = german;

            var sport = new Sport
            {
                SportName = "Volleyball",
                SportShortName = "WVB",
                SportCode = "WVB",
                Division = 1,
                Week = null,
                SeasonYear = 2025
            };

            var url = NcaaProcessor.GetUrl(sport);
            var today = DateTime.Now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            var cultureSensitive = DateTime.Now.ToString("MM/dd/yyyy", german);

            Assert.Contains($"\"contestDate\":\"{today}\"", url);
            Assert.Matches("\"contestDate\":\"\\d{2}/\\d{2}/\\d{4}\"", url);
            Assert.DoesNotContain($".", today);
            if (cultureSensitive != today)
                Assert.DoesNotContain($"\"contestDate\":\"{cultureSensitive}\"", url);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
        }
    }

    [Fact]
    public void GetUrl_Overload_UsesExplicitWeekContestDateAndSeasonYear()
    {
        var sport = new Sport
        {
            SportName = "Football FCS",
            SportShortName = "FCS",
            SportCode = "MFB",
            Division = 12,
            Week = 2,
            SeasonYear = 2025
        };

        var url = NcaaProcessor.GetUrl(sport, week: 9, contestDate: "01/15/2026", seasonYear: 2024);

        Assert.Contains("\"week\":9", url);
        Assert.Contains("\"contestDate\":\"01/15/2026\"", url);
        Assert.Contains("\"seasonYear\":2024", url);
        Assert.DoesNotContain("\"week\":2", url);
        Assert.DoesNotContain("\"seasonYear\":2025", url);
        Assert.Contains(NcaaProcessor.NcaaContestsGraphQlUrl, url);
    }

    [Fact]
    public void GetUrl_Overload_WeekZeroOrNegative_StillProducesValidUrl()
    {
        var sport = new Sport
        {
            SportName = "Football FCS",
            SportShortName = "FCS",
            SportCode = "MFB",
            Division = 12,
            Week = 2,
            SeasonYear = 2025
        };

        var zero = NcaaProcessor.GetUrl(sport, week: 0, contestDate: null, seasonYear: 2025);
        var negative = NcaaProcessor.GetUrl(sport, week: -1, contestDate: null, seasonYear: 2025);

        Assert.Contains("\"week\":0", zero);
        Assert.Contains("\"contestDate\":null", zero);
        Assert.Contains("\"seasonYear\":2025", zero);
        Assert.Contains("\"week\":-1", negative);
        Assert.Contains(NcaaProcessor.NcaaContestsGraphQlUrl, zero);
        Assert.Contains(NcaaProcessor.NcaaContestsGraphQlUrl, negative);
    }
}
