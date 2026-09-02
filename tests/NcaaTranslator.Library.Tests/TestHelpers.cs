using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
        WindowBounds.BaseDirectory = AppContext.BaseDirectory;
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
        string gameState = "P",
        string? startDate = null)
    {
        startDate ??= DateTime.Now.ToString("MM/dd/yyyy");
        return new Contest
        {
            contestId = id,
            gameState = gameState,
            startTimeEpoch = startTimeEpoch,
            startTime = "7:00 PM ET",
            startDate = startDate,
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

    public static DateTime Sep1 => new(2026, 9, 1, 12, 0, 0);

    public static long EpochOn(string startDate, int hour = 19)
    {
        var date = DateTime.ParseExact(startDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
        var local = DateTime.SpecifyKind(date.Date.AddHours(hour), DateTimeKind.Local);
        return new DateTimeOffset(local).ToUnixTimeSeconds();
    }

    public static Contest CreateDatedContest(
        long id,
        string startDate,
        string gameState = "P",
        string home6 = "NO DAK",
        string homeShort = "North Dakota",
        string homeConfSeo = "mvc",
        string away6 = "S DAK",
        string awayShort = "South Dakota",
        string awayConfSeo = "mvc")
    {
        return CreateContest(
            id,
            home6,
            homeShort,
            homeConfSeo,
            away6,
            awayShort,
            awayConfSeo,
            startTimeEpoch: EpochOn(startDate),
            gameState: gameState,
            startDate: startDate);
    }

    public static IEnumerable<long> AllContestIds(NcaaScoreboard? board)
    {
        var data = board?.data;
        if (data == null)
            yield break;
        foreach (var contest in (data.homeGames ?? new List<Contest>())
            .Concat(data.conferenceGames ?? new List<Contest>())
            .Concat(data.nonConferenceGames ?? new List<Contest>()))
        {
            yield return contest.contestId;
        }
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
    private static readonly Regex WeekRegex = new("\"week\":(?<w>-?\\d+|null)", RegexOptions.Compiled);
    private static readonly Regex DateRegex = new("\"contestDate\":(?:\"(?<d>[^\"]*)\"|null)", RegexOptions.Compiled);

    public string Response { get; set; } = "";
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public Exception? ExceptionToThrow { get; set; }
    public Task? Block { get; set; }
    public Uri? LastRequestUri { get; private set; }
    public List<Uri> RequestUris { get; } = new();
    public Dictionary<int, string> WeekResponses { get; } = new();
    public Dictionary<string, string> DateResponses { get; } = new(StringComparer.Ordinal);

    private int _callCount;
    public int CallCount => _callCount;

    public bool CalledWeek(int week) =>
        RequestUris.Any(u => Regex.IsMatch(u.ToString(), $"\"week\":{week}(?!\\d)"));

    public bool CalledContestDate(string contestDate) =>
        RequestUris.Any(u => u.ToString().Contains($"\"contestDate\":\"{contestDate}\""));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        LastRequestUri = request.RequestUri;
        if (request.RequestUri != null)
            RequestUris.Add(request.RequestUri);
        if (Block != null)
            await Block.ConfigureAwait(false);
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;

        var url = request.RequestUri?.ToString() ?? "";
        var body = ResolveBody(url);
        return new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private string ResolveBody(string url)
    {
        var weekMatch = WeekRegex.Match(url);
        if (weekMatch.Success && weekMatch.Groups["w"].Value != "null" &&
            int.TryParse(weekMatch.Groups["w"].Value, out var week) &&
            WeekResponses.TryGetValue(week, out var weekBody))
        {
            return weekBody;
        }

        var dateMatch = DateRegex.Match(url);
        if (dateMatch.Success && dateMatch.Groups["d"].Success &&
            DateResponses.TryGetValue(dateMatch.Groups["d"].Value, out var dateBody))
        {
            return dateBody;
        }

        return Response;
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
        WindowBounds.BaseDirectory = DirectoryPath;

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
