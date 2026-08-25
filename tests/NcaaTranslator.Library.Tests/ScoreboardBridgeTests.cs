using System.Text.Json;
using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class ScoreboardBridgeTests
{
    [Fact]
    public void Handle_Status_WhenIdle_IsNotRunning()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.UseSettings();

        using var doc = Handle("""{"id":"st","method":"status"}""");

        Assert.Equal("st", Id(doc));
        var result = Result(doc);
        Assert.False(result.GetProperty("running").GetBoolean());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("lastUpdate").ValueKind);

        using var board = Handle("""{"id":"g","method":"getScoreboard"}""");
        Assert.Equal(0, Result(board).GetProperty("sports").GetArrayLength());
    }

    [Fact]
    public async Task Handle_StartStopStatus_DisabledSports_DoesNotCallHttp()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = TestHelpers.CreateSport();
        sport.Enabled = false;
        Settings.SettingsList!.Sports!.Add(sport);
        Settings.SettingsList.Timer = 60;

        using (var started = Handle("""{"id":"1","method":"start"}"""))
        {
            Assert.Equal("1", Id(started));
            Assert.True(Result(started).GetProperty("running").GetBoolean());
        }

        await AppBridge.WaitForPollAsync();

        using (var status = Handle("""{"id":"2","method":"status"}"""))
        {
            var result = Result(status);
            Assert.True(result.GetProperty("running").GetBoolean());
            Assert.Equal(JsonValueKind.String, result.GetProperty("lastUpdate").ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("lastUpdate").GetString()));
        }

        using (var stopped = Handle("""{"id":"3","method":"stop"}"""))
        {
            var result = Result(stopped);
            Assert.False(result.GetProperty("running").GetBoolean());
            Assert.Equal(JsonValueKind.String, result.GetProperty("lastUpdate").ValueKind);
        }

        using (var status = Handle("""{"id":"4","method":"status"}"""))
        {
            Assert.False(Result(status).GetProperty("running").GetBoolean());
        }
    }

    [Fact]
    public async Task Handle_Start_ReturnsBeforePollCompletes()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings();
        Settings.SettingsList!.Sports!.Add(TestHelpers.CreateSport(mode: GameDisplayMode.All));
        Settings.SettingsList.Timer = 60;

        var block = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new FakeHttpMessageHandler
        {
            Response = TestHelpers.ToScoreboardJson(
                TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc",
                    gameState: "I", homeScore: 7, awayScore: 0)),
            Block = block.Task
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        try
        {
            using var started = Handle("""{"id":"s","method":"start"}""");
            Assert.True(Result(started).GetProperty("running").GetBoolean());

            using var board = Handle("""{"id":"g","method":"getScoreboard"}""");
            Assert.Equal(0, Result(board).GetProperty("sports").GetArrayLength());

            block.TrySetResult(true);
            await AppBridge.WaitForPollAsync();

            using var ready = Handle("""{"id":"g2","method":"getScoreboard"}""");
            Assert.Equal(1, Result(ready).GetProperty("sports").GetArrayLength());
        }
        finally
        {
            block.TrySetResult(true);
            Handle("""{"id":"x","method":"stop"}""");
            await AppBridge.WaitForPollAsync();
        }
    }

    [Fact]
    public async Task Handle_StopDuringPoll_ThenStart_PollsAgain()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings();
        Settings.SettingsList!.Sports!.Add(TestHelpers.CreateSport(mode: GameDisplayMode.All));
        Settings.SettingsList.Timer = 60;

        var block = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new FakeHttpMessageHandler
        {
            Response = TestHelpers.ToScoreboardJson(
                TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc",
                    gameState: "I", homeScore: 1, awayScore: 0)),
            Block = block.Task
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        try
        {
            using var started = Handle("""{"id":"1","method":"start"}""");
            Assert.True(Result(started).GetProperty("running").GetBoolean());

            await WaitUntil(() => handler.CallCount >= 1);

            using var stopped = Handle("""{"id":"2","method":"stop"}""");
            Assert.False(Result(stopped).GetProperty("running").GetBoolean());

            using var startedAgain = Handle("""{"id":"3","method":"start"}""");
            Assert.True(Result(startedAgain).GetProperty("running").GetBoolean());

            block.TrySetResult(true);
            await AppBridge.WaitForPollAsync();

            Assert.True(handler.CallCount >= 2);
            using var board = Handle("""{"id":"g","method":"getScoreboard"}""");
            Assert.Equal(1, Result(board).GetProperty("sports").GetArrayLength());
        }
        finally
        {
            block.TrySetResult(true);
            Handle("""{"id":"x","method":"stop"}""");
            await AppBridge.WaitForPollAsync();
        }
    }

    [Fact]
    public async Task Handle_GetScoreboard_MapsHomeAwayByIsHome_NotListOrder()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var sport = TestHelpers.CreateSport(mode: GameDisplayMode.All);
        Settings.SettingsList!.Sports!.Add(sport);
        Settings.SettingsList.Timer = 60;

        var contest = new Contest
        {
            contestId = 9,
            gameState = "I",
            currentPeriod = "2nd",
            contestClock = "12:34",
            startTimeEpoch = 1725000000,
            teams = new List<ContestTeam>
            {
                new()
                {
                    isHome = false,
                    name6Char = "S DAK",
                    nameShort = "South Dakota",
                    seoname = "south-dakota",
                    conferenceSeo = "mvc",
                    score = 10
                },
                new()
                {
                    isHome = true,
                    name6Char = "NO DAK",
                    nameShort = "North Dakota",
                    seoname = "north-dakota",
                    conferenceSeo = "mvc",
                    score = 21
                }
            }
        };

        var handler = new FakeHttpMessageHandler
        {
            Response = TestHelpers.ToScoreboardJson(contest)
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        using var started = Handle("""{"id":"s","method":"start"}""");
        Assert.True(Result(started).GetProperty("running").GetBoolean());
        await AppBridge.WaitForPollAsync();
        Assert.Equal(1, handler.CallCount);

        using var doc = Handle("""{"id":"g","method":"getScoreboard"}""");
        Assert.Equal("g", Id(doc));
        var sports = Result(doc).GetProperty("sports");
        Assert.Equal(1, sports.GetArrayLength());

        var sportJson = sports[0];
        Assert.Equal("Football FCS", sportJson.GetProperty("sportName").GetString());
        Assert.Equal("All", sportJson.GetProperty("gameDisplayMode").GetString());
        Assert.Equal(1, sportJson.GetProperty("homeGamesCount").GetInt32());
        Assert.Equal(0, sportJson.GetProperty("confGamesCount").GetInt32());
        Assert.Equal(0, sportJson.GetProperty("nonConfGamesCount").GetInt32());

        var games = sportJson.GetProperty("games");
        Assert.Equal(1, games.GetArrayLength());
        var game = games[0];
        Assert.Equal("UND", game.GetProperty("home").GetString());
        Assert.Equal(21, game.GetProperty("homeScore").GetInt32());
        Assert.Equal("South Dakota", game.GetProperty("away").GetString());
        Assert.Equal(10, game.GetProperty("awayScore").GetInt32());
        Assert.Equal(contest.displayClock, game.GetProperty("displayClock").GetString());

        Handle("""{"id":"x","method":"stop"}""");
    }

    [Fact]
    public async Task Handle_GetScoreboard_LiveMode_OnlyInProgressGames()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings();
        Settings.SettingsList!.Sports!.Add(TestHelpers.CreateSport(mode: GameDisplayMode.Live));
        Settings.SettingsList.Timer = 60;

        var live = TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc",
            homeScore: 14, awayScore: 7, gameState: "I");
        live.currentPeriod = "1st";
        live.contestClock = "8:00";
        var pregame = TestHelpers.CreateContest(2, "NDSU", "North Dakota St.", "mvc", "SDSU", "South Dakota St.", "mvc",
            startTimeEpoch: 1725000100, gameState: "P");

        var handler = new FakeHttpMessageHandler
        {
            Response = TestHelpers.ToScoreboardJson(live, pregame)
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        Handle("""{"id":"s","method":"start"}""");
        await AppBridge.WaitForPollAsync();

        using var doc = Handle("""{"id":"g","method":"getScoreboard"}""");
        var games = Result(doc).GetProperty("sports")[0].GetProperty("games");
        Assert.Equal(1, games.GetArrayLength());
        Assert.Equal("UND", games[0].GetProperty("home").GetString());
        Assert.Equal(14, games[0].GetProperty("homeScore").GetInt32());

        Handle("""{"id":"x","method":"stop"}""");
    }

    [Fact]
    public async Task Handle_GetScoreboard_DisplayMode_UsesDisplayGames()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings();
        Settings.SettingsList!.Sports!.Add(TestHelpers.CreateSport(mode: GameDisplayMode.Display));
        Settings.SettingsList.Timer = 60;

        var home = TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc", 100);
        var conference = TestHelpers.CreateContest(2, "NDSU", "North Dakota St.", "mvc", "SDSU", "South Dakota St.", "mvc", 200);
        var displayTeam = TestHelpers.CreateContest(3, "UVA", "Virginia", "acc", "DUKE", "Duke", "acc", 300);
        var other = TestHelpers.CreateContest(4, "BAMA", "Alabama", "sec", "AUB", "Auburn", "sec", 400);

        var handler = new FakeHttpMessageHandler
        {
            Response = TestHelpers.ToScoreboardJson(home, conference, displayTeam, other)
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        Handle("""{"id":"s","method":"start"}""");
        await AppBridge.WaitForPollAsync();

        using var doc = Handle("""{"id":"g","method":"getScoreboard"}""");
        var sportJson = Result(doc).GetProperty("sports")[0];
        Assert.Equal("Display", sportJson.GetProperty("gameDisplayMode").GetString());
        Assert.Equal(1, sportJson.GetProperty("homeGamesCount").GetInt32());
        Assert.Equal(1, sportJson.GetProperty("confGamesCount").GetInt32());
        Assert.Equal(2, sportJson.GetProperty("nonConfGamesCount").GetInt32());
        Assert.Equal(3, sportJson.GetProperty("displayGamesCount").GetInt32());

        var games = sportJson.GetProperty("games");
        Assert.Equal(3, games.GetArrayLength());
        Assert.Equal("UND", games[0].GetProperty("home").GetString());
        Assert.Equal("NDSU", games[1].GetProperty("home").GetString());
        Assert.Equal("Virginia", games[2].GetProperty("home").GetString());

        Handle("""{"id":"x","method":"stop"}""");
    }

    [Fact]
    public async Task Handle_Start_FailedSportFetch_DoesNotCrashBridge()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings();
        Settings.SettingsList!.Sports!.Add(TestHelpers.CreateSport());
        Settings.SettingsList.Timer = 60;

        var handler = new FakeHttpMessageHandler { Response = "not-json" };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        using var started = Handle("""{"id":"s","method":"start"}""");
        Assert.False(started.RootElement.TryGetProperty("error", out _));
        Assert.True(Result(started).GetProperty("running").GetBoolean());
        await AppBridge.WaitForPollAsync();

        using var board = Handle("""{"id":"g","method":"getScoreboard"}""");
        Assert.False(board.RootElement.TryGetProperty("error", out _));
        Assert.Equal(0, Result(board).GetProperty("sports").GetArrayLength());

        Handle("""{"id":"x","method":"stop"}""");
    }

    [Fact]
    public async Task Handle_GetScoreboard_SkipsDisabledSports()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings();
        var enabled = TestHelpers.CreateSport(mode: GameDisplayMode.All);
        var disabled = TestHelpers.CreateSport(mode: GameDisplayMode.All);
        disabled.SportName = "Basketball";
        disabled.SportShortName = "MBB";
        disabled.Enabled = false;
        Settings.SettingsList!.Sports!.Add(enabled);
        Settings.SettingsList.Sports.Add(disabled);
        Settings.SettingsList.Timer = 60;

        var handler = new FakeHttpMessageHandler
        {
            Response = TestHelpers.ToScoreboardJson(
                TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc",
                    gameState: "I", homeScore: 3, awayScore: 0))
        };
        NcaaProcessor.HttpClient = new HttpClient(handler);

        Handle("""{"id":"s","method":"start"}""");
        await AppBridge.WaitForPollAsync();

        using var doc = Handle("""{"id":"g","method":"getScoreboard"}""");
        var sports = Result(doc).GetProperty("sports");
        Assert.Equal(1, sports.GetArrayLength());
        Assert.Equal("Football FCS", sports[0].GetProperty("sportName").GetString());

        Handle("""{"id":"x","method":"stop"}""");
    }

    private static JsonDocument Handle(string json) => JsonDocument.Parse(AppBridge.Handle(json));

    private static string? Id(JsonDocument doc) =>
        doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;

    private static JsonElement Result(JsonDocument doc) => doc.RootElement.GetProperty("result");

    private static async Task WaitUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        var start = Environment.TickCount64;
        while (!condition())
        {
            if (Environment.TickCount64 - start > timeoutMs)
                throw new TimeoutException("Timed out waiting for condition.");
            await Task.Delay(10);
        }
    }
}
