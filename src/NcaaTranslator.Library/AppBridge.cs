using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;
using Timer = System.Timers.Timer;

namespace NcaaTranslator.Library
{
    public class BridgeRequest
    {
        public string? Id { get; set; }
        public string? Method { get; set; }
        public JsonElement? Params { get; set; }
    }

    public class BridgeResponse
    {
        public string? Id { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
    }

    public class PingResult
    {
        public bool Ok { get; set; }
    }

    public class SettingsSnapshot
    {
        public int Timer { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? HomeTeam { get; set; }
        public List<SportSnapshot> Sports { get; set; } = new();
        public List<DisplayTeamSnapshot> DisplayTeams { get; set; } = new();
        public XmlToJsonSnapshot XmlToJson { get; set; } = new();
    }

    public class SportSnapshot
    {
        public string Name { get; set; } = "";
        public string Short { get; set; } = "";
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Code { get; set; }
        public bool Enabled { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? ConferenceName { get; set; }
        public int Division { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? Week { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? SeasonYear { get; set; }
        public string GameDisplayMode { get; set; } = "Live";
        public ListsNeededSnapshot? ListsNeeded { get; set; }
        public OosUpdaterSnapshot? OosUpdater { get; set; }
    }

    public class ListsNeededSnapshot
    {
        public bool ConferenceGames { get; set; }
        public bool NonConferenceGames { get; set; }
        public bool Top25Games { get; set; }
    }

    public class OosUpdaterSnapshot
    {
        public bool Enabled { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? OosFilePath { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? OosFileName { get; set; }
        public int NumberOfOutScores { get; set; }
        public int NumberOfTeamsPer { get; set; }
    }

    public class DisplayTeamSnapshot
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? NcaaTeamName { get; set; }
    }

    public class XmlToJsonSnapshot
    {
        public bool Enabled { get; set; }
        public List<string> FilePaths { get; set; } = new();
    }

    public class TeamNameSnapshot
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Name6Char { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? CustomName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Seoname { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? NameShort { get; set; }
    }

    public class ConferenceNameSnapshot
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? ConferenceSeo { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? CustomConferenceName { get; set; }
    }

    public class TeamCustomNameParams
    {
        public string? Name6Char { get; set; }
        public string? CustomName { get; set; }
    }

    public class ConferenceCustomNameParams
    {
        public string? ConferenceSeo { get; set; }
        public string? CustomConferenceName { get; set; }
    }

    public class GameDisplayModeParams
    {
        public string? SportName { get; set; }
        public string? GameDisplayMode { get; set; }
    }

    public class PickPathResult
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Path { get; set; }
    }

    public class StatusResult
    {
        public bool Running { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? LastUpdate { get; set; }
    }

    public class ScoreboardSnapshot
    {
        public List<SportScoreboardSnapshot> Sports { get; set; } = new();
    }

    public class SportScoreboardSnapshot
    {
        public string SportName { get; set; } = "";
        public string GameDisplayMode { get; set; } = "";
        public int ConfGamesCount { get; set; }
        public int NonConfGamesCount { get; set; }
        public int DisplayGamesCount { get; set; }
        public int HomeGamesCount { get; set; }
        public List<GameSnapshot> Games { get; set; } = new();
    }

    public class GameSnapshot
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Home { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? HomeScore { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Away { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? AwayScore { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? DisplayClock { get; set; }
    }

    public static class AppBridge
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly object StateLock = new();
        private static readonly SingleFlightGate ConversionGate = new();
        private static readonly Dictionary<string, NcaaScoreboard> Scoreboards = new();
        private static Timer? _timer;
        private static bool _running;
        private static bool _rerunAfter;
        private static bool _pollLoopRunning;
        private static Task _inFlight = Task.CompletedTask;
        private static string? _lastUpdate;

        public static string Handle(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Serialize(new BridgeResponse { Error = "Empty request" });

            BridgeRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<BridgeRequest>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                return Serialize(new BridgeResponse { Error = $"Invalid JSON: {ex.Message}" });
            }

            if (request == null)
                return Serialize(new BridgeResponse { Error = "Invalid request" });

            if (string.IsNullOrWhiteSpace(request.Method))
                return Serialize(new BridgeResponse { Id = request.Id, Error = "Missing method" });

            try
            {
                object result = request.Method.Trim() switch
                {
                    "ping" => new PingResult { Ok = true },
                    "getSettings" => GetSettings(),
                    "saveSettings" => SaveSettings(request.Params),
                    "getTeams" => GetTeams(),
                    "saveTeamCustomName" => SaveTeamCustomName(request.Params),
                    "getConferences" => GetConferences(),
                    "saveConferenceCustomName" => SaveConferenceCustomName(request.Params),
                    "pickFolder" => throw new InvalidOperationException("pickFolder requires the desktop host."),
                    "pickFile" => throw new InvalidOperationException("pickFile requires the desktop host."),
                    "start" => Start(),
                    "stop" => Stop(),
                    "status" => GetStatus(),
                    "getScoreboard" => GetScoreboard(),
                    "setGameDisplayMode" => SetGameDisplayMode(request.Params),
                    _ => throw new InvalidOperationException($"Unknown method '{request.Method}'")
                };

                return Serialize(new BridgeResponse { Id = request.Id, Result = result });
            }
            catch (Exception ex)
            {
                return Serialize(new BridgeResponse { Id = request.Id, Error = ex.Message });
            }
        }

        internal static Task WaitForPollAsync()
        {
            lock (StateLock)
                return _inFlight;
        }

        internal static void ResetForTests()
        {
            Timer? timer;
            lock (StateLock)
            {
                timer = _timer;
                _timer = null;
                _running = false;
                _rerunAfter = false;
                _pollLoopRunning = false;
                _lastUpdate = null;
                Scoreboards.Clear();
                _inFlight = Task.CompletedTask;
            }

            if (timer == null)
                return;

            timer.Elapsed -= OnTimerElapsed;
            timer.Stop();
            timer.Dispose();
        }

        private static SettingsSnapshot GetSettings()
        {
            EnsureSettings();

            var settings = Settings.SettingsList
                ?? throw new InvalidDataException("Settings were not loaded.");

            return new SettingsSnapshot
            {
                Timer = settings.Timer,
                HomeTeam = settings.HomeTeam,
                Sports = (settings.Sports ?? new List<Sport>()).Select(ToSportSnapshot).ToList(),
                DisplayTeams = (settings.DisplayTeams ?? new List<DisplayTeam>())
                    .Select(d => new DisplayTeamSnapshot { NcaaTeamName = d.NcaaTeamName })
                    .ToList(),
                XmlToJson = new XmlToJsonSnapshot
                {
                    Enabled = settings.XmlToJson?.Enabled ?? false,
                    FilePaths = settings.XmlToJson?.FilePaths?
                        .Select(f => f.Path ?? "")
                        .ToList() ?? new List<string>()
                }
            };
        }

        private static SettingsSnapshot SaveSettings(JsonElement? paramsElement)
        {
            EnsureSettings();
            if (paramsElement is not { } el || el.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Settings payload is required.");

            var settings = Settings.SettingsList
                ?? throw new InvalidDataException("Settings were not loaded.");

            if (el.TryGetProperty("timer", out var timerEl) && timerEl.ValueKind == JsonValueKind.Number)
                settings.Timer = timerEl.GetInt32();

            if (el.TryGetProperty("homeTeam", out var homeEl))
                settings.HomeTeam = ResolveTeamCode(homeEl.ValueKind == JsonValueKind.String ? homeEl.GetString() : null);

            if (el.TryGetProperty("sports", out var sportsEl) && sportsEl.ValueKind == JsonValueKind.Array)
            {
                var sports = sportsEl.Deserialize<List<SportSnapshot>>(JsonOptions) ?? new List<SportSnapshot>();
                settings.Sports = sports.Select(ToSport).ToList();
            }

            if (el.TryGetProperty("displayTeams", out var displayEl) && displayEl.ValueKind == JsonValueKind.Array)
            {
                var display = displayEl.Deserialize<List<DisplayTeamSnapshot>>(JsonOptions) ?? new List<DisplayTeamSnapshot>();
                settings.DisplayTeams = display.Select(d => new DisplayTeam
                {
                    NcaaTeamName = ResolveTeamCode(d.NcaaTeamName) ?? d.NcaaTeamName
                }).ToList();
            }

            if (el.TryGetProperty("xmlToJson", out var xmlEl) && xmlEl.ValueKind == JsonValueKind.Object)
            {
                var xml = xmlEl.Deserialize<XmlToJsonSnapshot>(JsonOptions) ?? new XmlToJsonSnapshot();
                settings.XmlToJson = new XmlToJson
                {
                    Enabled = xml.Enabled,
                    FilePaths = xml.FilePaths.Select(p => new FilePath { Path = p }).ToList()
                };
            }

            Settings.Save();

            lock (StateLock)
            {
                if (_timer != null)
                    _timer.Interval = Math.Max(1, Settings.Timer);
            }

            return GetSettings();
        }

        private static List<TeamNameSnapshot> GetTeams()
        {
            EnsureNameConverters();
            return NameConverters.GetTeams().Select(ToTeamSnapshot).ToList();
        }

        private static TeamNameSnapshot SaveTeamCustomName(JsonElement? paramsElement)
        {
            EnsureNameConverters();
            var incoming = ReadRequiredParams<TeamCustomNameParams>(paramsElement);
            if (string.IsNullOrWhiteSpace(incoming.Name6Char))
                throw new InvalidOperationException("name6Char is required.");
            if (string.IsNullOrWhiteSpace(incoming.CustomName))
                throw new InvalidOperationException("customName cannot be empty.");

            if (!NameConverters.TeamDict.TryGetValue(incoming.Name6Char, out var team))
                throw new InvalidOperationException($"Team '{incoming.Name6Char}' was not found.");

            team.customName = incoming.CustomName.Trim();
            NameConverters.Reload();

            if (!NameConverters.TeamDict.TryGetValue(incoming.Name6Char, out var saved))
                throw new InvalidDataException($"Team '{incoming.Name6Char}' was not found after save.");

            return ToTeamSnapshot(saved);
        }

        private static List<ConferenceNameSnapshot> GetConferences()
        {
            EnsureNameConverters();
            return NameConverters.GetConferences().Select(ToConferenceSnapshot).ToList();
        }

        private static ConferenceNameSnapshot SaveConferenceCustomName(JsonElement? paramsElement)
        {
            EnsureNameConverters();
            var incoming = ReadRequiredParams<ConferenceCustomNameParams>(paramsElement);
            if (string.IsNullOrWhiteSpace(incoming.ConferenceSeo))
                throw new InvalidOperationException("conferenceSeo is required.");
            if (string.IsNullOrWhiteSpace(incoming.CustomConferenceName))
                throw new InvalidOperationException("customConferenceName cannot be empty.");

            if (!NameConverters.ConfDict.TryGetValue(incoming.ConferenceSeo, out var conference))
                throw new InvalidOperationException($"Conference '{incoming.ConferenceSeo}' was not found.");

            conference.customConferenceName = incoming.CustomConferenceName.Trim();
            NameConverters.Reload();

            if (!NameConverters.ConfDict.TryGetValue(incoming.ConferenceSeo, out var saved))
                throw new InvalidDataException($"Conference '{incoming.ConferenceSeo}' was not found after save.");

            return ToConferenceSnapshot(saved);
        }

        private static SportSnapshot ToSportSnapshot(Sport sport)
        {
            var lists = sport.ListsNeeded ?? new ListsNeeded();
            var oos = sport.OosUpdater ?? new OosUpdater();
            return new SportSnapshot
            {
                Name = sport.SportName,
                Short = sport.SportShortName,
                Code = sport.SportCode,
                Enabled = sport.Enabled,
                ConferenceName = sport.ConferenceName,
                Division = sport.Division,
                Week = sport.Week,
                SeasonYear = sport.SeasonYear,
                GameDisplayMode = sport.GameDisplayMode.ToString(),
                ListsNeeded = new ListsNeededSnapshot
                {
                    ConferenceGames = lists.conferenceGames,
                    NonConferenceGames = lists.nonConferenceGames,
                    Top25Games = lists.top25Games
                },
                OosUpdater = new OosUpdaterSnapshot
                {
                    Enabled = oos.Enabled,
                    OosFilePath = oos.OosFilePath,
                    OosFileName = oos.OosFileName,
                    NumberOfOutScores = oos.NumberOfOutScores,
                    NumberOfTeamsPer = oos.NumberOfTeamsPer
                }
            };
        }

        private static Sport ToSport(SportSnapshot snapshot)
        {
            if (!Enum.TryParse<GameDisplayMode>(snapshot.GameDisplayMode, ignoreCase: true, out var mode))
                mode = GameDisplayMode.Live;

            var lists = snapshot.ListsNeeded == null
                ? new ListsNeeded()
                : new ListsNeeded
                {
                    conferenceGames = snapshot.ListsNeeded.ConferenceGames,
                    nonConferenceGames = snapshot.ListsNeeded.NonConferenceGames,
                    top25Games = snapshot.ListsNeeded.Top25Games
                };
            var oos = snapshot.OosUpdater == null
                ? new OosUpdater()
                : new OosUpdater
                {
                    Enabled = snapshot.OosUpdater.Enabled,
                    OosFilePath = snapshot.OosUpdater.OosFilePath,
                    OosFileName = snapshot.OosUpdater.OosFileName,
                    NumberOfOutScores = snapshot.OosUpdater.NumberOfOutScores,
                    NumberOfTeamsPer = snapshot.OosUpdater.NumberOfTeamsPer
                };
            return new Sport
            {
                SportName = snapshot.Name ?? "",
                SportShortName = snapshot.Short ?? "",
                SportCode = snapshot.Code,
                Enabled = snapshot.Enabled,
                ConferenceName = snapshot.ConferenceName,
                Division = snapshot.Division,
                Week = snapshot.Week,
                SeasonYear = snapshot.SeasonYear,
                GameDisplayMode = mode,
                ListsNeeded = lists,
                OosUpdater = oos
            };
        }

        private static TeamNameSnapshot ToTeamSnapshot(Team team) =>
            new()
            {
                Name6Char = team.name6Char,
                CustomName = team.customName,
                Seoname = team.seoname,
                NameShort = team.nameShort
            };

        private static ConferenceNameSnapshot ToConferenceSnapshot(Conferences conference) =>
            new()
            {
                ConferenceSeo = conference.conferenceSeo,
                CustomConferenceName = conference.customConferenceName
            };

        private static string? ResolveTeamCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            try
            {
                EnsureNameConverters();
            }
            catch (FileNotFoundException)
            {
                return value.Trim();
            }
            catch (InvalidDataException)
            {
                return value.Trim();
            }

            var options = TeamSelection.CreateOptions(NameConverters.GetTeams());
            return TeamSelection.ResolveName6Char(value, value, options) ?? value.Trim();
        }

        private static T ReadRequiredParams<T>(JsonElement? paramsElement)
        {
            if (paramsElement is not { } el || el.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                throw new InvalidOperationException("Request params are required.");

            var value = el.Deserialize<T>(JsonOptions);
            if (value is null)
                throw new InvalidOperationException("Request params are invalid.");
            return value;
        }

        private static StatusResult Start()
        {
            EnsureSettings();
            EnsureNameConverters();

            lock (StateLock)
            {
                _running = true;
                EnsureTimer();
                _timer!.Interval = Math.Max(1, Settings.Timer);
                _timer.Enabled = true;
            }

            QueuePoll(force: true);
            return GetStatus();
        }

        private static StatusResult Stop()
        {
            lock (StateLock)
            {
                _running = false;
                if (_timer != null)
                    _timer.Enabled = false;
            }

            return GetStatus();
        }

        private static StatusResult GetStatus()
        {
            lock (StateLock)
                return SnapshotStatus();
        }

        private static StatusResult SnapshotStatus() =>
            new() { Running = _running, LastUpdate = _lastUpdate };

        private static ScoreboardSnapshot SetGameDisplayMode(JsonElement? paramsElement)
        {
            EnsureSettings();
            var incoming = ReadRequiredParams<GameDisplayModeParams>(paramsElement);
            if (string.IsNullOrWhiteSpace(incoming.SportName))
                throw new InvalidOperationException("sportName is required.");
            if (!Enum.TryParse<GameDisplayMode>(incoming.GameDisplayMode, ignoreCase: true, out var mode))
                throw new InvalidOperationException($"Unknown gameDisplayMode '{incoming.GameDisplayMode}'.");

            var sport = (Settings.GetSports() ?? new List<Sport>())
                .FirstOrDefault(s => string.Equals(s.SportName, incoming.SportName, StringComparison.OrdinalIgnoreCase));
            if (sport == null)
                throw new InvalidOperationException($"Sport '{incoming.SportName}' was not found.");

            sport.GameDisplayMode = mode;
            Settings.Save();
            return GetScoreboard();
        }

        private static ScoreboardSnapshot GetScoreboard()
        {
            EnsureSettings();

            Dictionary<string, NcaaScoreboard> snapshot;
            lock (StateLock)
                snapshot = new Dictionary<string, NcaaScoreboard>(Scoreboards);

            var result = new ScoreboardSnapshot();
            var sports = Settings.GetSports() ?? new List<Sport>();
            foreach (var sport in sports)
            {
                if (!sport.Enabled || string.IsNullOrEmpty(sport.SportName))
                    continue;

                if (!snapshot.TryGetValue(sport.SportName, out var scoreboard) || scoreboard.data == null)
                    continue;

                result.Sports.Add(ToSportSnapshot(sport, scoreboard));
            }

            return result;
        }

        private static SportScoreboardSnapshot ToSportSnapshot(Sport sport, NcaaScoreboard? scoreboard)
        {
            var data = scoreboard?.data;
            var games = data == null
                ? new List<Contest>()
                : GetGamesToShow(data, sport.GameDisplayMode);

            return new SportScoreboardSnapshot
            {
                SportName = sport.SportName,
                GameDisplayMode = sport.GameDisplayMode.ToString(),
                ConfGamesCount = data?.conferenceGames?.Count ?? 0,
                NonConfGamesCount = data?.nonConferenceGames?.Count ?? 0,
                DisplayGamesCount = data?.displayGames?.Count ?? 0,
                HomeGamesCount = data?.homeGames?.Count ?? 0,
                Games = games.Select(ToGameSnapshot).ToList()
            };
        }

        private static GameSnapshot ToGameSnapshot(Contest contest)
        {
            var home = contest.HomeTeam;
            var away = contest.AwayTeam;
            return new GameSnapshot
            {
                Home = home?.customName,
                HomeScore = home?.score,
                Away = away?.customName,
                AwayScore = away?.score,
                DisplayClock = contest.displayClock
            };
        }

        private static List<Contest> GetGamesToShow(Data data, GameDisplayMode mode)
        {
            var conference = data.conferenceGames ?? new List<Contest>();
            var nonConference = data.nonConferenceGames ?? new List<Contest>();
            var home = data.homeGames ?? new List<Contest>();
            var allGames = new List<Contest>(conference.Count + nonConference.Count + home.Count);
            allGames.AddRange(conference);
            allGames.AddRange(nonConference);
            allGames.AddRange(home);

            return mode switch
            {
                GameDisplayMode.All => allGames,
                GameDisplayMode.Display => data.displayGames ?? new List<Contest>(),
                _ => allGames.Where(c => c.gameState == "I").ToList()
            };
        }

        private static void EnsureSettings()
        {
            if (Settings.SettingsList == null)
                Settings.Load();
        }

        private static void EnsureNameConverters()
        {
            if (NameConverters.NameList == null)
                NameConverters.Load();
        }

        private static void EnsureTimer()
        {
            if (_timer != null)
                return;

            _timer = new Timer(2000) { AutoReset = true, Enabled = false };
            _timer.Elapsed += OnTimerElapsed;
        }

        private static void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            QueuePoll(force: false);
        }

        private static void QueuePoll(bool force)
        {
            lock (StateLock)
            {
                if (_pollLoopRunning)
                {
                    if (force)
                        _rerunAfter = true;
                    return;
                }

                _pollLoopRunning = true;
                _rerunAfter = false;
                _inFlight = Task.Run(RunPollThenMaybeRerun);
            }
        }

        private static async Task RunPollThenMaybeRerun()
        {
            try
            {
                while (true)
                {
                    await ConversionGate.RunAsync(PerformConversion).ConfigureAwait(false);

                    lock (StateLock)
                    {
                        if (!_rerunAfter || !_running)
                        {
                            _rerunAfter = false;
                            _pollLoopRunning = false;
                            return;
                        }

                        _rerunAfter = false;
                    }
                }
            }
            catch
            {
                lock (StateLock)
                {
                    _pollLoopRunning = false;
                    _rerunAfter = false;
                }
                throw;
            }
        }

        private static bool IsRunning()
        {
            lock (StateLock)
                return _running;
        }

        private static async Task PerformConversion()
        {
            if (!IsRunning())
                return;

            lock (StateLock)
                _lastUpdate = DateTime.Now.ToString("HH:mm:ss.fff");

            var sportsList = Settings.GetSports();
            if (sportsList == null)
                return;

            foreach (var sport in sportsList)
            {
                if (!IsRunning())
                    return;

                try
                {
                    var result = await NcaaProcessor.ConvertNcaaScoreboard(sport).ConfigureAwait(false);
                    if (!IsRunning())
                        return;
                    if (string.IsNullOrEmpty(sport.SportName))
                        continue;

                    lock (StateLock)
                    {
                        if (!_running)
                            return;
                        Scoreboards[sport.SportName] = result;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Sport '{sport.SportName}' failed: {ex.Message}");
                }
            }

            if (!IsRunning())
                return;

            if (Settings.XmlToJson?.Enabled == true)
            {
                try
                {
                    NcaaProcessor.ConvertXmlToJson(Settings.XmlToJson);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"XML to JSON conversion failed: {ex.Message}");
                }
            }
        }

        private static string Serialize(BridgeResponse response) =>
            JsonSerializer.Serialize(response, JsonOptions);
    }
}
