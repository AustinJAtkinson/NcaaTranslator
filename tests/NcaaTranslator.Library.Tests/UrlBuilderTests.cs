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
        var today = DateTime.Now.ToString("MM/dd/yyyy");

        Assert.Contains("\"week\":null", url);
        Assert.Contains($"\"contestDate\":\"{today}\"", url);
    }
}
