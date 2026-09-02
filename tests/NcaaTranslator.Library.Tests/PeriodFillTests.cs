using System.Globalization;
using System.Text.Json;
using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class PeriodFillTests : IDisposable
{
    private readonly TempWorkspace _workspace = new();
    private static readonly DateTime AsOf = TestHelpers.Sep1;

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public async Task WeekSport_LookBack1_UsesLeftoverClusterWithoutExtraHttp()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 1, lookForward: 0);
        var handler = HandlerForWeek(2, Week2Contests());
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(1, handler.CallCount);
        Assert.False(handler.CalledWeek(1));
        Assert.Equal(new[] { 4L, 5L, 6L }, TestHelpers.AllContestIds(result.Current).OrderBy(id => id));
        Assert.Equal(new[] { 1L, 2L, 3L }, FileIds("Football FCS-Prev-Games.json").OrderBy(id => id));
        Assert.Equal("Sep 3\u20136", result.CurrentDateRange);
        Assert.Equal("Aug 27\u201329", result.PrevDateRange);
        Assert.True(File.Exists("Football FCS-Post-Games.json"));
        Assert.Empty(FileIds("Football FCS-Post-Games.json"));
    }

    [Fact]
    public async Task WeekSport_LookBack2_FetchesPreviousWeek()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 2, lookForward: 0);
        var handler = HandlerForWeek(2, Week2Contests());
        handler.WeekResponses[1] = TestHelpers.ToScoreboardJson(
            TestHelpers.CreateDatedContest(10, "08/20/2026"),
            TestHelpers.CreateDatedContest(11, "08/22/2026"));
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(2, handler.CallCount);
        Assert.True(handler.CalledWeek(1));
        Assert.False(handler.CalledWeek(0));
        Assert.Equal(new[] { 4L, 5L, 6L }, TestHelpers.AllContestIds(result.Current).OrderBy(id => id));
        Assert.Equal(new[] { 1L, 2L, 3L, 10L, 11L }, FileIds("Football FCS-Prev-Games.json").OrderBy(id => id));
    }

    [Fact]
    public async Task WeekSport_LookBack0_DropsLeftoverFromCurrentAndPrev()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 0, lookForward: 0);
        var handler = HandlerForWeek(2, Week2Contests());
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(1, handler.CallCount);
        Assert.Equal(new[] { 4L, 5L, 6L }, TestHelpers.AllContestIds(result.Current).OrderBy(id => id));
        Assert.DoesNotContain(1L, TestHelpers.AllContestIds(result.Current));
        Assert.True(File.Exists("Football FCS-Prev-Games.json"));
        Assert.True(File.Exists("Football FCS-Post-Games.json"));
        Assert.Empty(FileIds("Football FCS-Prev-Games.json"));
        Assert.Empty(FileIds("Football FCS-Post-Games.json"));
    }

    [Fact]
    public async Task WeekSport_LookForward1_FetchesNextWeekWhenNoLeftoverAfter()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 1, lookForward: 1);
        var handler = HandlerForWeek(2, Week2Contests());
        handler.WeekResponses[3] = TestHelpers.ToScoreboardJson(
            TestHelpers.CreateDatedContest(20, "09/10/2026"),
            TestHelpers.CreateDatedContest(21, "09/12/2026"));
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(2, handler.CallCount);
        Assert.True(handler.CalledWeek(3));
        Assert.False(handler.CalledWeek(1));
        Assert.Equal(new[] { 4L, 5L, 6L }, TestHelpers.AllContestIds(result.Current).OrderBy(id => id));
        Assert.Equal(new[] { 1L, 2L, 3L }, FileIds("Football FCS-Prev-Games.json").OrderBy(id => id));
        Assert.Equal(new[] { 20L, 21L }, FileIds("Football FCS-Post-Games.json").OrderBy(id => id));
    }

    [Fact]
    public async Task WeekSport_InsideFirstCluster_PostUsesLeftoverWithoutExtraHttp()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 1, lookForward: 1);
        var handler = HandlerForWeek(2, Week2Contests());
        handler.WeekResponses[1] = TestHelpers.ToScoreboardJson(
            TestHelpers.CreateDatedContest(10, "08/20/2026"));
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var asOf = new DateTime(2026, 8, 28);
        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport, asOf);

        Assert.Equal(2, handler.CallCount);
        Assert.True(handler.CalledWeek(1));
        Assert.False(handler.CalledWeek(3));
        Assert.Equal(new[] { 1L, 2L, 3L }, TestHelpers.AllContestIds(result.Current).OrderBy(id => id));
        Assert.Equal(new[] { 4L, 5L, 6L }, FileIds("Football FCS-Post-Games.json").OrderBy(id => id));
        Assert.Equal(new[] { 10L }, FileIds("Football FCS-Prev-Games.json"));
    }

    [Fact]
    public async Task DateSport_LookBack2_MergesTwoDaysIntoPrevFile()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = DateSport(lookBack: 2, lookForward: 0);
        var handler = new FakeHttpMessageHandler
        {
            DateResponses =
            {
                ["09/01/2026"] = TestHelpers.ToScoreboardJson(TestHelpers.CreateDatedContest(1, "09/01/2026")),
                ["08/31/2026"] = TestHelpers.ToScoreboardJson(TestHelpers.CreateDatedContest(2, "08/31/2026")),
                ["08/30/2026"] = TestHelpers.ToScoreboardJson(TestHelpers.CreateDatedContest(3, "08/30/2026"))
            }
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(3, handler.CallCount);
        Assert.True(handler.CalledContestDate("09/01/2026"));
        Assert.True(handler.CalledContestDate("08/31/2026"));
        Assert.True(handler.CalledContestDate("08/30/2026"));
        Assert.Equal(new[] { 1L }, TestHelpers.AllContestIds(result.Current));
        Assert.True(File.Exists("Volleyball-Prev-Games.json"));
        Assert.Equal(new[] { 2L, 3L }, FileIds("Volleyball-Prev-Games.json").OrderBy(id => id));
        Assert.Equal("Sep 1", result.CurrentDateRange);
    }

    [Fact]
    public async Task EmptyNcaa_WritesEmptyPrevAndPostFiles()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 0, lookForward: 0);
        var handler = new FakeHttpMessageHandler { Response = """{"data":{"contests":[]}}""" };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.True(File.Exists("Football FCS-Prev-Games.json"));
        Assert.True(File.Exists("Football FCS-Post-Games.json"));
        Assert.Empty(FileIds("Football FCS-Prev-Games.json"));
        Assert.Empty(FileIds("Football FCS-Post-Games.json"));
        Assert.True(File.Exists("Football FCS-Games.json"));
    }

    [Fact]
    public async Task AutoIncrement_LastClusterPastWithoutInProgress_BumpsWeekAndSaves()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 0, lookForward: 0);
        Settings.SettingsList!.Sports!.Add(sport);
        var handler = new FakeHttpMessageHandler();
        handler.WeekResponses[2] = TestHelpers.ToScoreboardJson(
            TestHelpers.CreateDatedContest(1, "08/27/2026"),
            TestHelpers.CreateDatedContest(2, "08/29/2026"));
        handler.WeekResponses[3] = TestHelpers.ToScoreboardJson(
            TestHelpers.CreateDatedContest(8, "09/03/2026"));
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(3, sport.Week);
        Assert.Equal(3, Settings.SettingsList.Sports[0].Week);
        Assert.True(handler.CalledWeek(2));
        Assert.True(handler.CalledWeek(3));
        Assert.Equal(2, handler.CallCount);
        Assert.Equal(new[] { 8L }, TestHelpers.AllContestIds(result.Current));
        var saved = File.ReadAllText(Path.Combine(_workspace.DirectoryPath, "Settings.json"));
        Assert.Contains("\"Week\":3", saved);
    }

    [Fact]
    public async Task AutoIncrement_EmptyPayload_DoesNotBumpWeek()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 0, lookForward: 0);
        var handler = new FakeHttpMessageHandler { Response = """{"data":{"contests":[]}}""" };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(2, sport.Week);
        Assert.Equal(1, handler.CallCount);
        Assert.False(handler.CalledWeek(3));
    }

    [Fact]
    public async Task AutoIncrement_InProgressAfterMidnight_DoesNotBumpWeek()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 0, lookForward: 0);
        var live = TestHelpers.CreateDatedContest(1, "08/29/2026", gameState: "I");
        var handler = HandlerForWeek(2, new[] { live });
        NcaaProcessor.HttpClient = new HttpClient(handler);

        await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(2, sport.Week);
        Assert.Equal(1, handler.CallCount);
        Assert.False(handler.CalledWeek(3));
    }

    [Fact]
    public async Task AutoIncrement_InProgressInLeftoverCluster_DoesNotBumpWeek()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 1, lookForward: 0);
        var leftoverLive = TestHelpers.CreateDatedContest(1, "08/27/2026", gameState: "I");
        leftoverLive.currentPeriod = "4th";
        leftoverLive.contestClock = "1:00";
        var lastCluster = TestHelpers.CreateDatedContest(4, "09/03/2026", gameState: "F",
            home6: "NDSU", homeShort: "North Dakota St.", away6: "SDSU", awayShort: "South Dakota St.");
        var handler = HandlerForWeek(2, new[] { leftoverLive, lastCluster });
        NcaaProcessor.HttpClient = new HttpClient(handler);

        await NcaaProcessor.ConvertNcaaScoreboard(sport, new DateTime(2026, 9, 7));

        Assert.Equal(2, sport.Week);
        Assert.Equal(1, handler.CallCount);
        Assert.False(handler.CalledWeek(3));
        Assert.Equal(new[] { 1L }, FileIds("Football FCS-Prev-Games.json"));
    }

    [Fact]
    public async Task WeekSport_EmptyPreviousWeek_StillFetchesNextOffset()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 2, lookForward: 2);
        var handler = HandlerForWeek(2, new[]
        {
            TestHelpers.CreateDatedContest(4, "09/03/2026"),
            TestHelpers.CreateDatedContest(5, "09/05/2026")
        });
        handler.WeekResponses[1] = """{"data":{"contests":[]}}""";
        handler.WeekResponses[0] = TestHelpers.ToScoreboardJson(
            TestHelpers.CreateDatedContest(10, "08/20/2026"));
        handler.WeekResponses[3] = """{"data":{"contests":[]}}""";
        handler.WeekResponses[4] = TestHelpers.ToScoreboardJson(
            TestHelpers.CreateDatedContest(20, "09/17/2026"));
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(5, handler.CallCount);
        Assert.True(handler.CalledWeek(1));
        Assert.True(handler.CalledWeek(0));
        Assert.True(handler.CalledWeek(3));
        Assert.True(handler.CalledWeek(4));
        Assert.Equal(new[] { 10L }, FileIds("Football FCS-Prev-Games.json"));
        Assert.Equal(new[] { 20L }, FileIds("Football FCS-Post-Games.json"));
        Assert.Equal(new[] { 4L, 5L }, TestHelpers.AllContestIds(result.Current).OrderBy(id => id));
    }

    [Fact]
    public async Task DateSport_LookBack_FormatsContestDateWithInvariantCulture()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = DateSport(lookBack: 1, lookForward: 0);
        var handler = new FakeHttpMessageHandler
        {
            DateResponses =
            {
                ["09/01/2026"] = TestHelpers.ToScoreboardJson(TestHelpers.CreateDatedContest(1, "09/01/2026")),
                ["08/31/2026"] = TestHelpers.ToScoreboardJson(TestHelpers.CreateDatedContest(2, "08/31/2026"))
            }
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var original = CultureInfo.CurrentCulture;
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;
        try
        {
            var german = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = german;
            CultureInfo.DefaultThreadCurrentCulture = german;
            await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
        }

        Assert.True(handler.CalledContestDate("09/01/2026"));
        Assert.True(handler.CalledContestDate("08/31/2026"));
        Assert.All(handler.RequestUris, uri =>
        {
            Assert.DoesNotContain("\"contestDate\":\"01.09.2026\"", uri.ToString());
            Assert.DoesNotContain("\"contestDate\":\"31.08.2026\"", uri.ToString());
        });
    }

    [Fact]
    public async Task NegativeLookBack_IsClampedToZero()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: -4, lookForward: -1);
        var handler = HandlerForWeek(2, Week2Contests());
        NcaaProcessor.HttpClient = new HttpClient(handler);

        await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf);

        Assert.Equal(1, handler.CallCount);
        Assert.Empty(FileIds("Football FCS-Prev-Games.json"));
        Assert.DoesNotContain(1L, FileIds("Football FCS-Games.json"));
    }

    [Fact]
    public async Task CachedExtras_AreReusedWhenFetchExtrasIsFalse()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = WeekSport(lookBack: 2, lookForward: 0);
        var cache = new ExtraPeriodCache();
        var handler = HandlerForWeek(2, Week2Contests());
        handler.WeekResponses[1] = TestHelpers.ToScoreboardJson(
            TestHelpers.CreateDatedContest(10, "08/20/2026"));
        NcaaProcessor.HttpClient = new HttpClient(handler);

        await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf, fetchExtras: true, cache);
        Assert.Equal(2, handler.CallCount);

        await NcaaProcessor.ConvertNcaaScoreboard(sport, AsOf, fetchExtras: false, cache);
        Assert.Equal(3, handler.CallCount);
        Assert.Equal(2, handler.RequestUris.Count(u => RegexWeek(u, 2)));
        Assert.Equal(1, handler.RequestUris.Count(u => RegexWeek(u, 1)));
        Assert.Contains(10L, FileIds("Football FCS-Prev-Games.json"));
    }

    private static bool RegexWeek(Uri uri, int week) =>
        System.Text.RegularExpressions.Regex.IsMatch(uri.ToString(), $"\"week\":{week}(?!\\d)");

    private static Sport WeekSport(int lookBack, int lookForward)
    {
        var sport = TestHelpers.CreateSport();
        sport.LookBack = lookBack;
        sport.LookForward = lookForward;
        return sport;
    }

    private static Sport DateSport(int lookBack, int lookForward)
    {
        var sport = TestHelpers.CreateSport();
        sport.SportName = "Volleyball";
        sport.SportShortName = "WVB";
        sport.SportCode = "WVB";
        sport.Division = 1;
        sport.Week = null;
        sport.LookBack = lookBack;
        sport.LookForward = lookForward;
        return sport;
    }

    private static Contest[] Week2Contests() =>
    [
        TestHelpers.CreateDatedContest(1, "08/27/2026"),
        TestHelpers.CreateDatedContest(2, "08/28/2026"),
        TestHelpers.CreateDatedContest(3, "08/29/2026"),
        TestHelpers.CreateDatedContest(4, "09/03/2026", home6: "NDSU", homeShort: "North Dakota St.", away6: "SDSU", awayShort: "South Dakota St."),
        TestHelpers.CreateDatedContest(5, "09/05/2026", home6: "UVA", homeShort: "Virginia", homeConfSeo: "acc", away6: "DUKE", awayShort: "Duke", awayConfSeo: "acc"),
        TestHelpers.CreateDatedContest(6, "09/06/2026", home6: "NDSU", homeShort: "North Dakota St.", away6: "UVA", awayShort: "Virginia", awayConfSeo: "acc")
    ];

    private static FakeHttpMessageHandler HandlerForWeek(int week, IEnumerable<Contest> contests)
    {
        var json = TestHelpers.ToScoreboardJson(contests.ToArray());
        var handler = new FakeHttpMessageHandler { Response = json };
        handler.WeekResponses[week] = json;
        return handler;
    }

    private static List<long> FileIds(string fileName)
    {
        var json = File.ReadAllText(fileName);
        var board = JsonSerializer.Deserialize<NcaaScoreboard>(json);
        return TestHelpers.AllContestIds(board).ToList();
    }
}
