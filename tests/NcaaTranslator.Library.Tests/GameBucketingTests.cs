using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class GameBucketingTests : IDisposable
{
    private readonly TempWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private NcaaScoreboard Bucket(GameDisplayMode mode, bool oosEnabled)
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = TestHelpers.CreateSport(oosEnabled, mode);

        var scoreboard = TestHelpers.CreateScoreboard(
            TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc", 100),
            TestHelpers.CreateContest(2, "NDSU", "North Dakota St.", "mvc", "SDSU", "South Dakota St.", "mvc", 200),
            TestHelpers.CreateContest(3, "UVA", "Virginia", "acc", "DUKE", "Duke", "acc", 300),
            TestHelpers.CreateContest(4, "DUKE", "Duke", "acc", "UVA", "Virginia", "acc", 400, homeRank: 3)
        );

        NcaaProcessor.CategorizeGames(scoreboard, sport);
        return scoreboard;
    }

    [Fact]
    public void ConferenceVsNonConferenceVsHome()
    {
        var scoreboard = Bucket(GameDisplayMode.Display, oosEnabled: false);
        var data = scoreboard.data!;

        Assert.Single(data.homeGames);
        Assert.Equal(1, data.homeGames[0].contestId);

        Assert.Single(data.conferenceGames!);
        Assert.Equal(2, data.conferenceGames![0].contestId);

        Assert.Equal(2, data.nonConferenceGames!.Count);
        Assert.Contains(data.nonConferenceGames, g => g.contestId == 3);
        Assert.Contains(data.nonConferenceGames, g => g.contestId == 4);
    }

    [Fact]
    public void HomeGamesAppearInDisplayGames_WhenDisplayMode()
    {
        var scoreboard = Bucket(GameDisplayMode.Display, oosEnabled: false);
        var display = scoreboard.data!.displayGames!;

        Assert.True(display.Count >= 2);
        Assert.Equal(1, display[0].contestId);
        Assert.Contains(display, g => g.contestId == 2);
        Assert.Contains(display, g => g.contestId == 3 || g.contestId == 4);
    }

    [Fact]
    public void HomeGamesAppearInDisplayGames_WhenOosEnabled()
    {
        var scoreboard = Bucket(GameDisplayMode.Live, oosEnabled: true);
        var display = scoreboard.data!.displayGames!;

        Assert.Equal(1, display[0].contestId);
        Assert.Contains(display, g => g.contestId == 1);
        Assert.Contains(display, g => g.contestId == 2);
    }

    [Fact]
    public async Task ConvertNcaaScoreboard_UsesFixtureHttp_AndDoesNotCallLiveNcaa()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();

        var handler = new FakeHttpMessageHandler
        {
            Response = TestHelpers.ToScoreboardJson(
                TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc")
            )
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(TestHelpers.CreateSport(oosEnabled: false, GameDisplayMode.Display));

        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("sdataprod.ncaa.com", handler.LastRequestUri!.Host);
        Assert.Single(result.data!.homeGames);
        Assert.Single(result.data.displayGames!);
        Assert.Equal(1, result.data.displayGames![0].contestId);
        Assert.True(File.Exists("Football FCS-Games.json"));
    }

    [Fact]
    public async Task ConvertNcaaScoreboard_FailedHttp_ReturnsEmptyScoreboard()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();

        var handler = new FakeHttpMessageHandler
        {
            ExceptionToThrow = new HttpRequestException("NCAA unavailable")
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        var result = await NcaaProcessor.ConvertNcaaScoreboard(TestHelpers.CreateSport());

        Assert.NotNull(result);
        Assert.Null(result.data);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ConvertNcaaScoreboard_DisabledSport_DoesNotCallHttp()
    {
        var handler = new FakeHttpMessageHandler { Response = "{}" };
        NcaaProcessor.HttpClient = new HttpClient(handler);
        var sport = TestHelpers.CreateSport();
        sport.Enabled = false;

        var result = await NcaaProcessor.ConvertNcaaScoreboard(sport);

        Assert.Equal(0, handler.CallCount);
        Assert.Null(result.data);
    }

    [Fact]
    public void DisplayTeams_MatchesByNameShort()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings(displayTeams: new List<DisplayTeam>
        {
            new() { NcaaTeamName = "Virginia" }
        });
        var sport = TestHelpers.CreateSport(oosEnabled: false, GameDisplayMode.Display);
        var scoreboard = TestHelpers.CreateScoreboard(
            TestHelpers.CreateContest(1, "UVA", "Virginia", "acc", "BAMA", "Alabama", "sec", 100),
            TestHelpers.CreateContest(2, "BAMA", "Alabama", "sec", "AUB", "Auburn", "sec", 200)
        );

        NcaaProcessor.CategorizeGames(scoreboard, sport);

        Assert.Single(scoreboard.data!.displayGames!);
        Assert.Equal(1, scoreboard.data.displayGames![0].contestId);
    }

    [Fact]
    public void DisplayTeams_MixedName6CharAndNameShort()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings(displayTeams: new List<DisplayTeam>
        {
            new() { NcaaTeamName = "UVA" },
            new() { NcaaTeamName = "Duke" }
        });
        var sport = TestHelpers.CreateSport(oosEnabled: false, GameDisplayMode.Display);
        var scoreboard = TestHelpers.CreateScoreboard(
            TestHelpers.CreateContest(1, "UVA", "Virginia", "acc", "BAMA", "Alabama", "sec", 100),
            TestHelpers.CreateContest(2, "BAMA", "Alabama", "sec", "DUKE", "Duke", "acc", 200),
            TestHelpers.CreateContest(3, "BAMA", "Alabama", "sec", "AUB", "Auburn", "sec", 300)
        );

        NcaaProcessor.CategorizeGames(scoreboard, sport);

        Assert.Equal(2, scoreboard.data!.displayGames!.Count);
        Assert.Contains(scoreboard.data.displayGames, g => g.contestId == 1);
        Assert.Contains(scoreboard.data.displayGames, g => g.contestId == 2);
        Assert.DoesNotContain(scoreboard.data.displayGames, g => g.contestId == 3);
    }

    [Fact]
    public async Task ConvertNcaaScoreboard_NullContests_DoesNotThrow()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();

        var handler = new FakeHttpMessageHandler
        {
            Response = """{"data":{"contests":null}}"""
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        NcaaScoreboard? result = null;
        var ex = await Record.ExceptionAsync(async () =>
            result = await NcaaProcessor.ConvertNcaaScoreboard(TestHelpers.CreateSport()));

        Assert.Null(ex);
        Assert.NotNull(result);
        Assert.NotNull(result!.data);
        Assert.Null(result.data!.contests);
        Assert.False(File.Exists("Football FCS-Games.json"));
    }

    [Fact]
    public async Task ConvertNcaaScoreboard_NullData_DoesNotThrow()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();

        var handler = new FakeHttpMessageHandler
        {
            Response = """{"data":null}"""
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        NcaaScoreboard? result = null;
        var ex = await Record.ExceptionAsync(async () =>
            result = await NcaaProcessor.ConvertNcaaScoreboard(TestHelpers.CreateSport()));

        Assert.Null(ex);
        Assert.NotNull(result);
        Assert.Null(result!.data);
        Assert.False(File.Exists("Football FCS-Games.json"));
    }

    [Fact]
    public async Task ConvertNcaaScoreboard_EmptyContests_DoesNotThrow()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        TestHelpers.UseSettings();

        var handler = new FakeHttpMessageHandler
        {
            Response = """{"data":{"contests":[]}}"""
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        NcaaScoreboard? result = null;
        var ex = await Record.ExceptionAsync(async () =>
            result = await NcaaProcessor.ConvertNcaaScoreboard(TestHelpers.CreateSport()));

        Assert.Null(ex);
        Assert.NotNull(result);
        Assert.NotNull(result!.data);
        Assert.Null(result.data!.contests);
        Assert.Empty(result.data.homeGames);
        Assert.Null(result.data.displayGames);
    }
}
