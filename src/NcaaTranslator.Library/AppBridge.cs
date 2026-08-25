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
                    "start" => Start(),
                    "stop" => Stop(),
                    "status" => GetStatus(),
                    "getScoreboard" => GetScoreboard(),
                    _ => throw new InvalidOperationException($"Unknown method '{request.Method}'")
                };

                return Serialize(new BridgeResponse { Id = request.Id, Result = result });
            }
            catch (Exception ex)
            {
                return Serialize(new BridgeResponse { Id = request.Id, Error = ex.Message });
            }
        }

        internal static void ResetForTests()
        {
            Timer? timer;
            lock (StateLock)
            {
                timer = _timer;
                _timer = null;
                _running = false;
                _lastUpdate = null;
                Scoreboards.Clear();
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
                HomeTeam = settings.HomeTeam
            };
        }

        private static StatusResult Start()
        {
            EnsureSettings();
            EnsureNameConverters();

            lock (StateLock)
            {
                if (_running)
                    return SnapshotStatus();

                _running = true;
                EnsureTimer();
                _timer!.Interval = Math.Max(1, Settings.Timer);
            }

            RunPollBlocking();

            lock (StateLock)
            {
                if (_running && _timer != null)
                    _timer.Enabled = true;
            }

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

                snapshot.TryGetValue(sport.SportName, out var scoreboard);
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
            _ = ConversionGate.RunAsync(PerformConversion);
        }

        private static void RunPollBlocking()
        {
            ConversionGate.RunAsync(PerformConversion).GetAwaiter().GetResult();
        }

        private static async Task PerformConversion()
        {
            lock (StateLock)
                _lastUpdate = DateTime.Now.ToString("HH:mm:ss.fff");

            var sportsList = Settings.GetSports();
            if (sportsList == null)
                return;

            foreach (var sport in sportsList)
            {
                try
                {
                    var result = await NcaaProcessor.ConvertNcaaScoreboard(sport).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(sport.SportName))
                        continue;

                    lock (StateLock)
                        Scoreboards[sport.SportName] = result;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Sport '{sport.SportName}' failed: {ex.Message}");
                }
            }

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
