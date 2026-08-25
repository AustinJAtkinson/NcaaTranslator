using System.Net;
using System.Text;
using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

internal static class TestHelpers
{
    public static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "ncaa-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static void ResetStatics()
    {
        NameConverters.NameList = null;
        NameConverters.TeamDict = new Dictionary<string, Team>();
        NameConverters.ConfDict = new Dictionary<string, Conferences>();
        NameConverters.FilePath = "NcaaNameConverter.json";
        NameConverters.BaseDirectory = AppContext.BaseDirectory;
        Settings.SettingsList = null;
        Settings.fileName = "Settings.json";
        Settings.BaseDirectory = AppContext.BaseDirectory;
        NcaaProcessor.HttpClient = new HttpClient(new ThrowingHttpMessageHandler());
        AppBridge.ResetForTests();
    }

    public static void WriteNameConverter(string path, string json)
    {
        File.WriteAllText(path, json);
        NameConverters.Load(path);
    }

    public static void WriteDefaultNames(string directory)
    {
        var path = Path.Combine(directory, "NcaaNameConverter.json");
        WriteNameConverter(path, """
        {
          "teams": [
            { "seoname": "north-dakota", "nameShort": "North Dakota", "name6Char": "NO DAK", "customName": "UND" },
            { "seoname": "south-dakota", "nameShort": "South Dakota", "name6Char": "S DAK", "customName": "South Dakota" },
            { "seoname": "north-dakota-st", "nameShort": "North Dakota St.", "name6Char": "NDSU", "customName": "NDSU" },
            { "seoname": "south-dakota-st", "nameShort": "South Dakota St.", "name6Char": "SDSU", "customName": "SDSU" },
            { "seoname": "virginia", "nameShort": "Virginia", "name6Char": "UVA", "customName": "Virginia" },
            { "seoname": "duke", "nameShort": "Duke", "name6Char": "DUKE", "customName": "Duke" }
          ],
          "conferences": [
            { "customConferenceName": "MVFC", "conferenceSeo": "mvc" },
            { "customConferenceName": "ACC", "conferenceSeo": "acc" }
          ]
        }
        """);
    }

    public static void UseSettings(string? homeTeam = "NO DAK", List<DisplayTeam>? displayTeams = null)
    {
        Settings.SettingsList = new Setting
        {
            Timer = 20,
            HomeTeam = homeTeam,
            DisplayTeams = displayTeams ?? new List<DisplayTeam> { new DisplayTeam { NcaaTeamName = "UVA" } },
            Sports = new List<Sport>()
        };
    }

    public static Sport CreateSport(bool oosEnabled = false, GameDisplayMode mode = GameDisplayMode.Live)
    {
        return new Sport
        {
            SportName = "Football FCS",
            SportShortName = "FCS",
            Enabled = true,
            ConferenceName = "MVFC",
            SportCode = "MFB",
            Division = 12,
            Week = 2,
            SeasonYear = 2025,
            GameDisplayMode = mode,
            OosUpdater = new OosUpdater { Enabled = oosEnabled },
            ListsNeeded = new ListsNeeded { conferenceGames = true, nonConferenceGames = true, top25Games = true }
        };
    }

    public static Contest CreateContest(
        long id,
        string home6,
        string homeShort,
        string homeConfSeo,
        string away6,
        string awayShort,
        string awayConfSeo,
        long startTimeEpoch = 1725000000,
        int? homeRank = null,
        int? awayRank = null,
        int? homeScore = 0,
        int? awayScore = 0,
        string gameState = "P")
    {
        return new Contest
        {
            contestId = id,
            gameState = gameState,
            startTimeEpoch = startTimeEpoch,
            startTime = "7:00 PM ET",
            teams = new List<ContestTeam>
            {
                new ContestTeam { isHome = true, name6Char = home6, nameShort = homeShort, seoname = homeShort.ToLowerInvariant().Replace(" ", "-"), conferenceSeo = homeConfSeo, score = homeScore, teamRank = homeRank },
                new ContestTeam { isHome = false, name6Char = away6, nameShort = awayShort, seoname = awayShort.ToLowerInvariant().Replace(" ", "-"), conferenceSeo = awayConfSeo, score = awayScore, teamRank = awayRank }
            }
        };
    }

    public static NcaaScoreboard CreateScoreboard(params Contest[] contests)
    {
        return new NcaaScoreboard
        {
            data = new Data
            {
                contests = contests.ToList()
            }
        };
    }

    public static string ToScoreboardJson(params Contest[] contests)
    {
        return System.Text.Json.JsonSerializer.Serialize(CreateScoreboard(contests));
    }
}

internal sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Tests must install FakeHttpMessageHandler; live NCAA is disabled.");
    }
}

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public string Response { get; set; } = "";
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public Exception? ExceptionToThrow { get; set; }
    public Task? Block { get; set; }
    public Uri? LastRequestUri { get; private set; }

    private int _callCount;
    public int CallCount => _callCount;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        LastRequestUri = request.RequestUri;
        if (Block != null)
            await Block.ConfigureAwait(false);
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;

        return new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(Response, Encoding.UTF8, "application/json")
        };
    }
}

internal sealed class TempWorkspace : IDisposable
{
    public string DirectoryPath { get; }
    public string CwdPath { get; }
    private readonly string _originalCwd;
    private readonly bool _ownsCwd;

    public TempWorkspace(bool isolateCwd = false)
    {
        DirectoryPath = TestHelpers.CreateTempDir();
        _originalCwd = Directory.GetCurrentDirectory();
        TestHelpers.ResetStatics();
        Settings.BaseDirectory = DirectoryPath;
        NameConverters.BaseDirectory = DirectoryPath;

        if (isolateCwd)
        {
            CwdPath = TestHelpers.CreateTempDir();
            _ownsCwd = true;
        }
        else
        {
            CwdPath = DirectoryPath;
            _ownsCwd = false;
        }

        Directory.SetCurrentDirectory(CwdPath);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalCwd);
        TestHelpers.ResetStatics();
        try { Directory.Delete(DirectoryPath, true); } catch { }
        if (_ownsCwd)
            try { Directory.Delete(CwdPath, true); } catch { }
    }
}
