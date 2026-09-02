using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NcaaTranslator.Library
{
    public class NcaaProcessor
    {
        internal const string NcaaContestsGraphQlUrl = "https://sdataprod.ncaa.com/?meta=GetContests_web&extensions={\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"7287cda610a9326931931080cb3a604828febe6fe3c9016a7e4a36db99efdb7c\"}}";

        private static HttpClient _httpClient = CreateHttpClient();

        internal static HttpClient HttpClient
        {
            get => _httpClient;
            set => _httpClient = value ?? CreateHttpClient();
        }

        internal static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NcaaTranslator", "1.0"));
            return client;
        }

        public static void FixNames(Contest gameData)
        {
            var home = gameData.HomeTeam;
            var away = gameData.AwayTeam;

            if (home != null)
            {
                home.customName = NameConverters.LookupTeam(new Names { name6Char = home.name6Char, nameShort = home.nameShort, seoname = home.seoname });
                home.customConferenceName = NameConverters.LookupConf(new Conference { conferenceSeo = home.conferenceSeo });
            }

            if (away != null)
            {
                away.customName = NameConverters.LookupTeam(new Names { name6Char = away.name6Char, nameShort = away.nameShort, seoname = away.seoname });
                away.customConferenceName = NameConverters.LookupConf(new Conference { conferenceSeo = away.conferenceSeo });
            }
        }

        public static string GetUrl(Sport sport)
        {
            int seasonYear = GetSeasonYear(sport);
            int? week = sport.Week;
            string? contestDate = week.HasValue ? null : FormatContestDate(DateTime.Now);
            return GetUrl(sport, week, contestDate, seasonYear);
        }

        public static string GetUrl(Sport sport, int? week, string? contestDate, int seasonYear)
        {
            var variables = new
            {
                sportCode = sport.SportCode,
                division = sport.Division,
                seasonYear = seasonYear,
                week = week,
                contestDate = contestDate
            };
            string variablesJson = JsonSerializer.Serialize(variables);
            return $"{NcaaContestsGraphQlUrl}&variables={variablesJson}";
        }

        public static int GetSeasonYear(Sport sport, DateTime? asOf = null)
        {
            if (sport.SeasonYear.HasValue)
                return sport.SeasonYear.Value;

            var date = asOf ?? DateTime.Now;
            return date.Month >= 8 ? date.Year : date.Year - 1;
        }

        internal static string FormatContestDate(DateTime date) =>
            date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

        public static async Task<string> NcaaResponse(string url)
        {
            try
            {
                return await HttpClient.GetStringAsync(url).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NCAA request failed: {ex.Message}");
                return "";
            }
        }

        public static void UpdateOos(NcaaScoreboard ncaaScoreboard, OosUpdater updater)
        {
            var displayGames = ncaaScoreboard.data?.displayGames;
            if (displayGames == null || displayGames.Count == 0)
                return;

            Console.WriteLine();
            var numberOfGames = displayGames.Count;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ClsGFXTemplate));
            var gameNeeded = 0;
            for (var i = 1; i <= updater.NumberOfOutScores; i++)
            {
                ClsGFXTemplate outScore;
                using (Stream reader = new FileStream(Path.Combine(updater.OosFilePath!, updater.OosFileName! + i + ".tmp"), FileMode.Open))
                {
                    outScore = (ClsGFXTemplate)xmlSerializer.Deserialize(reader)!;
                }
                for (var j = 1; j <= updater.NumberOfTeamsPer; j++)
                {
                    var gameData = displayGames[gameNeeded];
                    var home = gameData.HomeTeam;
                    var away = gameData.AwayTeam;
                    var homeTeam = home?.customName ?? "";
                    var awayTeam = away?.customName ?? "";
                    var homeScore = home?.score?.ToString() ?? "";
                    var awayScore = away?.score?.ToString() ?? "";
                    var clock = gameData.displayClockDefault;

                    Console.WriteLine("{0}\t{1}\tVS\t{2}\t{3}\tCLOCK\t{4}", homeTeam.Replace(".", "").PadRight($"{homeTeam}:".Length + (15 - $"{homeTeam}:".Length)),
                                                                             homeScore.Replace(".", "").PadRight($"{homeScore}:".Length + (2 - $"{homeScore}:".Length)),
                                                                             awayTeam.Replace(".", "").PadRight($"{awayTeam}:".Length + (15 - $"{awayTeam}:".Length)),
                                                                             awayScore.Replace(".", "").PadRight($"{awayScore}:".Length + (2 - $"{awayScore}:".Length)),
                                                                             clock);

                    SetGraphicText(outScore, string.Format("G{0} - V Team", j), awayTeam);
                    SetGraphicText(outScore, string.Format("G{0} - V Score", j), awayScore);
                    SetGraphicText(outScore, string.Format("G{0} - H Team", j), homeTeam);
                    SetGraphicText(outScore, string.Format("G{0} - H Score", j), homeScore);

                    if (gameData.gameState == "P" || gameData.gameState == "F")
                    {
                        SetGraphicText(outScore, string.Format("G{0} - Time", j), "");
                        SetGraphicText(outScore, string.Format("G{0} - Quarter", j), clock);
                    }
                    else
                    {
                        SetGraphicText(outScore, string.Format("G{0} - Time", j), gameData.contestClock ?? "");
                        SetGraphicText(outScore, string.Format("G{0} - Quarter", j), gameData.currentPeriod ?? "");
                    }
                    gameNeeded++;
                    if (gameNeeded >= numberOfGames)
                    {
                        gameNeeded = 0;
                    }
                }

                using (TextWriter writer = new StreamWriter(Path.Combine(updater.OosFilePath!, updater.OosFileName! + i + ".tmp")))
                {
                    xmlSerializer.Serialize(writer, outScore);
                }
            }
            Console.WriteLine();
        }

        private static void SetGraphicText(ClsGFXTemplate outScore, string graphicObjName, string text)
        {
            var element = outScore.GfxElements?.ClsGFXElement?.FirstOrDefault(x => x.GraphicObjName == graphicObjName);
            if (element == null)
            {
                Debug.WriteLine($"GFX element '{graphicObjName}' was not found; skipping.");
                return;
            }
            element.GraphicObjText = text;
        }

        internal static void CategorizeGames(NcaaScoreboard ncaaGames, Sport sport)
        {
            var displayList = Settings.GetDisplayTeams();
            // Always fill displayGames so the Main-tab Live/All/Display toggle can
            // re-filter a cached scoreboard without another NCAA fetch.
            ncaaGames.data!.conferenceGames ??= new List<Contest>();
            ncaaGames.data.nonConferenceGames ??= new List<Contest>();
            ncaaGames.data.displayGames ??= new List<Contest>();
            ncaaGames.data.homeGames ??= new List<Contest>();
            ncaaGames.data.top25Games ??= new List<Contest>();

            foreach (var gameData in ncaaGames.data.contests!)
            {
                FixNames(gameData);

                var homeTeamObj = gameData.HomeTeam;
                var awayTeamObj = gameData.AwayTeam;

                if (SameNonEmpty(homeTeamObj?.customConferenceName, awayTeamObj?.customConferenceName))
                {
                    gameData.conferenceDisplayName = homeTeamObj?.customConferenceName;
                }
                else
                {
                    gameData.conferenceDisplayName = sport.SportShortName;
                }

                if (SameNonEmpty(homeTeamObj?.customConferenceName, sport.ConferenceName) ||
                    SameNonEmpty(awayTeamObj?.customConferenceName, sport.ConferenceName))
                {
                    if (homeTeamObj?.name6Char == Settings.homeTeam || awayTeamObj?.name6Char == Settings.homeTeam)
                    {
                        ncaaGames.data!.homeGames.Add(gameData);
                    }
                    else
                    {
                        ncaaGames.data!.conferenceGames.Add(gameData);
                        ncaaGames.data!.displayGames!.Add(gameData);
                    }
                }
                else
                {
                    ncaaGames.data!.nonConferenceGames.Add(gameData);
                    if (displayList != null && displayList.Any(x => IsDisplayTeam(x, homeTeamObj) || IsDisplayTeam(x, awayTeamObj)))
                        ncaaGames.data!.displayGames!.Add(gameData);
                }

                var homeRank = homeTeamObj?.teamRank;
                var awayRank = awayTeamObj?.teamRank;
                bool homeTop25 = homeRank.HasValue && homeRank >= 1 && homeRank <= 25;
                bool awayTop25 = awayRank.HasValue && awayRank >= 1 && awayRank <= 25;
                if (sport.ListsNeeded.top25Games && (homeTop25 || awayTop25))
                    ncaaGames.data!.top25Games!.Add(gameData);
            }

            ncaaGames.data!.nonConferenceGames = ncaaGames.data!.nonConferenceGames
                .OrderBy(g => string.Equals(g.conferenceDisplayName, sport.SportShortName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(g => g.conferenceDisplayName)
                .ThenBy(g => g.startTimeEpoch)
                .ToList();

            ncaaGames.data!.displayGames ??= new List<Contest>();
            ncaaGames.data.displayGames = ncaaGames.data.homeGames
                .Concat(ncaaGames.data.displayGames)
                .ToList();

            ncaaGames.data!.contests!.Clear();
            ncaaGames.data.contests = null;

            if ((ncaaGames.data.displayGames?.Count ?? 0) == 0) ncaaGames.data.displayGames = null;
            if ((ncaaGames.data.top25Games?.Count ?? 0) == 0) ncaaGames.data.top25Games = null;
        }

        private static bool SameNonEmpty(string? left, string? right) =>
            !string.IsNullOrWhiteSpace(left) &&
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static bool IsDisplayTeam(DisplayTeam displayTeam, ContestTeam? team)
        {
            if (team == null || string.IsNullOrWhiteSpace(displayTeam.NcaaTeamName))
                return false;

            var wanted = displayTeam.NcaaTeamName;
            return string.Equals(wanted, team.name6Char, StringComparison.Ordinal) ||
                   string.Equals(wanted, team.nameShort, StringComparison.Ordinal);
        }

        public static async Task<NcaaScoreboard> ConvertNcaaScoreboard(Sport sport)
        {
            var result = await ConvertNcaaScoreboard(sport, DateTime.Now, fetchExtras: true, cache: null).ConfigureAwait(false);
            return result.Current;
        }

        internal static async Task<PeriodConversionResult> ConvertNcaaScoreboard(
            Sport sport,
            DateTime asOf,
            bool fetchExtras = true,
            ExtraPeriodCache? cache = null)
        {
            var result = new PeriodConversionResult();
            if (!sport.Enabled)
                return result;

            cache ??= new ExtraPeriodCache();
            var lookBack = Math.Max(0, sport.LookBack);
            var lookForward = Math.Max(0, sport.LookForward);
            asOf = asOf.Date;

            if (fetchExtras)
                cache.RemoveBySport(sport.SportName ?? "");

            if (sport.Week.HasValue)
                await ConvertWeekSport(sport, asOf, lookBack, lookForward, fetchExtras, cache, result).ConfigureAwait(false);
            else
                await ConvertDateSport(sport, asOf, lookBack, lookForward, fetchExtras, cache, result).ConfigureAwait(false);

            if (sport.OosUpdater.Enabled)
                UpdateOos(result.Current, sport.OosUpdater);

            return result;
        }

        internal static string CurrentGamesFileName(Sport sport) => $"{sport.SportName}-Games.json";
        internal static string PrevGamesFileName(Sport sport) => $"{sport.SportName}-Prev-Games.json";
        internal static string PostGamesFileName(Sport sport) => $"{sport.SportName}-Post-Games.json";

        internal static async Task<NcaaScoreboard?> FetchScoreboard(Sport sport, int? week, string? contestDate, DateTime seasonAsOf)
        {
            var seasonYear = GetSeasonYear(sport, seasonAsOf);
            var url = GetUrl(sport, week, contestDate, seasonYear);
            var body = await NcaaResponse(url).ConfigureAwait(false);
            if (string.IsNullOrEmpty(body))
                return null;
            return JsonSerializer.Deserialize<NcaaScoreboard>(body);
        }

        internal static NcaaScoreboard CategorizeAndExport(IEnumerable<Contest>? contests, Sport sport, string fileName)
        {
            var copy = (contests ?? Enumerable.Empty<Contest>()).ToList();
            copy.Sort((x, y) => x.startTimeEpoch.CompareTo(y.startTimeEpoch));
            var board = new NcaaScoreboard { data = new Data { contests = copy } };
            if (copy.Count > 0)
                CategorizeGames(board, sport);
            else
            {
                board.data.contests = null;
                board.data.conferenceGames = new List<Contest>();
                board.data.nonConferenceGames = new List<Contest>();
                board.data.displayGames = null;
                board.data.homeGames = new List<Contest>();
                board.data.top25Games = null;
            }

            WriteExport(board, sport, fileName);
            return board;
        }

        private static void WriteExport(NcaaScoreboard ncaaGames, Sport sport, string fileName)
        {
            var data = ncaaGames.data ?? new Data();
            var exportData = new NcaaScoreboard
            {
                data = new Data
                {
                    contests = data.contests,
                    nonConferenceGames = sport.ListsNeeded.nonConferenceGames ? data.nonConferenceGames : null,
                    conferenceGames = sport.ListsNeeded.conferenceGames ? data.conferenceGames : null,
                    displayGames = data.displayGames,
                    homeGames = data.homeGames,
                    top25Games = data.top25Games
                }
            };
            File.WriteAllText(fileName, JsonSerializer.Serialize(exportData));
        }

        private static async Task ConvertDateSport(
            Sport sport,
            DateTime asOf,
            int lookBack,
            int lookForward,
            bool fetchExtras,
            ExtraPeriodCache cache,
            PeriodConversionResult result)
        {
            var contestDate = FormatContestDate(asOf);
            var payload = await FetchScoreboard(sport, week: null, contestDate, asOf).ConfigureAwait(false);
            var currentContests = payload?.data?.contests;
            if (currentContests != null)
            {
                result.Current = CategorizeAndExport(currentContests, sport, CurrentGamesFileName(sport));
                result.CurrentDateRange = ContestClustering.FormatDateRange(currentContests)
                    ?? ContestClustering.FormatSingleDate(asOf);
            }
            else
            {
                result.Current = payload ?? new NcaaScoreboard();
                result.CurrentDateRange = ContestClustering.FormatSingleDate(asOf);
            }

            var prevContests = new List<Contest>();
            var prevDates = new List<DateTime>();
            for (var i = 1; i <= lookBack; i++)
            {
                var date = asOf.AddDays(-i);
                prevDates.Add(date);
                var dayContests = await GetCachedOrFetchDate(sport, date, fetchExtras, cache).ConfigureAwait(false);
                prevContests.AddRange(dayContests);
            }

            prevDates.Reverse();
            result.Prev = CategorizeAndExport(prevContests, sport, PrevGamesFileName(sport));
            result.PrevDateRange = ContestClustering.FormatDateRange(prevContests)
                ?? ContestClustering.FormatDateRange(prevDates);

            var postContests = new List<Contest>();
            var postDates = new List<DateTime>();
            for (var i = 1; i <= lookForward; i++)
            {
                var date = asOf.AddDays(i);
                postDates.Add(date);
                var dayContests = await GetCachedOrFetchDate(sport, date, fetchExtras, cache).ConfigureAwait(false);
                postContests.AddRange(dayContests);
            }

            result.Post = CategorizeAndExport(postContests, sport, PostGamesFileName(sport));
            result.PostDateRange = ContestClustering.FormatDateRange(postContests)
                ?? ContestClustering.FormatDateRange(postDates);
        }

        private static async Task ConvertWeekSport(
            Sport sport,
            DateTime asOf,
            int lookBack,
            int lookForward,
            bool fetchExtras,
            ExtraPeriodCache cache,
            PeriodConversionResult result)
        {
            var payload = await FetchScoreboard(sport, sport.Week, contestDate: null, asOf).ConfigureAwait(false);
            var contests = payload?.data?.contests?.ToList();
            var clusters = ContestClustering.ClusterContests(contests);

            if (contests != null &&
                contests.Count > 0 &&
                ContestClustering.ShouldAutoIncrementWeek(clusters, contests, asOf))
            {
                sport.Week = sport.Week!.Value + 1;
                if (Settings.SettingsList != null)
                    Settings.Save();
                fetchExtras = true;
                cache.RemoveBySport(sport.SportName ?? "");
                payload = await FetchScoreboard(sport, sport.Week, contestDate: null, asOf).ConfigureAwait(false);
                contests = payload?.data?.contests?.ToList();
                clusters = ContestClustering.ClusterContests(contests);
            }

            var currentIndex = ContestClustering.PickCurrentClusterIndex(clusters, asOf);
            List<Contest> currentContests = currentIndex >= 0
                ? clusters[currentIndex].ToList()
                : new List<Contest>();

            if (contests != null)
            {
                result.Current = CategorizeAndExport(currentContests, sport, CurrentGamesFileName(sport));
                result.CurrentDateRange = ContestClustering.FormatDateRange(currentContests);
            }
            else
            {
                result.Current = payload ?? new NcaaScoreboard();
            }

            var prevClusters = new List<List<Contest>>();
            var postClusters = new List<List<Contest>>();
            var remainingPrev = lookBack;
            var remainingPost = lookForward;

            if (currentIndex >= 0)
            {
                if (lookBack > 0)
                {
                    var before = clusters.Take(currentIndex).ToList();
                    var take = TakeFromEnd(before, remainingPrev);
                    prevClusters.AddRange(take);
                    remainingPrev -= take.Count;
                }

                if (lookForward > 0)
                {
                    var after = clusters.Skip(currentIndex + 1).ToList();
                    var take = TakeFromStart(after, remainingPost);
                    postClusters.AddRange(take);
                    remainingPost -= take.Count;
                }
            }

            var week = sport.Week!.Value;
            var prevAttempts = remainingPrev;
            for (var offset = 1; remainingPrev > 0 && offset <= prevAttempts; offset++)
            {
                var extraContests = await GetCachedOrFetchWeek(sport, week - offset, asOf, fetchExtras, cache).ConfigureAwait(false);
                if (extraContests.Count == 0)
                    continue;

                var extraClusters = ContestClustering.ClusterContests(extraContests);
                var take = TakeFromEnd(extraClusters, remainingPrev);
                prevClusters.InsertRange(0, take);
                remainingPrev -= take.Count;
            }

            var postAttempts = remainingPost;
            for (var offset = 1; remainingPost > 0 && offset <= postAttempts; offset++)
            {
                var extraContests = await GetCachedOrFetchWeek(sport, week + offset, asOf, fetchExtras, cache).ConfigureAwait(false);
                if (extraContests.Count == 0)
                    continue;

                var extraClusters = ContestClustering.ClusterContests(extraContests);
                var take = TakeFromStart(extraClusters, remainingPost);
                postClusters.AddRange(take);
                remainingPost -= take.Count;
            }

            var prevContests = prevClusters.SelectMany(c => c).ToList();
            var postContests = postClusters.SelectMany(c => c).ToList();
            result.Prev = CategorizeAndExport(prevContests, sport, PrevGamesFileName(sport));
            result.Post = CategorizeAndExport(postContests, sport, PostGamesFileName(sport));
            result.PrevDateRange = ContestClustering.FormatDateRange(prevContests);
            result.PostDateRange = ContestClustering.FormatDateRange(postContests);
        }

        private static List<List<Contest>> TakeFromEnd(List<List<Contest>> clusters, int count)
        {
            if (count <= 0 || clusters.Count == 0)
                return new List<List<Contest>>();
            var start = Math.Max(0, clusters.Count - count);
            return clusters.Skip(start).ToList();
        }

        private static List<List<Contest>> TakeFromStart(List<List<Contest>> clusters, int count)
        {
            if (count <= 0 || clusters.Count == 0)
                return new List<List<Contest>>();
            return clusters.Take(count).ToList();
        }

        private static async Task<List<Contest>> GetCachedOrFetchWeek(
            Sport sport,
            int week,
            DateTime asOf,
            bool fetchExtras,
            ExtraPeriodCache cache)
        {
            var key = ExtraPeriodCache.WeekKey(sport.SportName ?? "", week);
            return await cache.GetOrFetch(key, fetchExtras, async () =>
            {
                var board = await FetchScoreboard(sport, week, contestDate: null, asOf).ConfigureAwait(false);
                return board?.data?.contests?.ToList() ?? new List<Contest>();
            }).ConfigureAwait(false);
        }

        private static async Task<List<Contest>> GetCachedOrFetchDate(
            Sport sport,
            DateTime date,
            bool fetchExtras,
            ExtraPeriodCache cache)
        {
            var contestDate = FormatContestDate(date);
            var key = ExtraPeriodCache.DateKey(sport.SportName ?? "", contestDate);
            return await cache.GetOrFetch(key, fetchExtras, async () =>
            {
                var board = await FetchScoreboard(sport, week: null, contestDate, date).ConfigureAwait(false);
                return board?.data?.contests?.ToList() ?? new List<Contest>();
            }).ConfigureAwait(false);
        }

        public static void ConvertXmlToJson(XmlToJson xmlToJson)
        {
            if (!xmlToJson.Enabled)
                return;

            foreach (var filePath in xmlToJson.FilePaths!)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath.Path!);

                var jsonText = JsonConvert.SerializeXmlNode(doc);
                File.WriteAllText(Path.ChangeExtension(filePath.Path!, ".json"), jsonText);
            }
        }
    }

    internal sealed class PeriodConversionResult
    {
        public NcaaScoreboard Current { get; set; } = new();
        public NcaaScoreboard Prev { get; set; } = new();
        public NcaaScoreboard Post { get; set; } = new();
        public string? CurrentDateRange { get; set; }
        public string? PrevDateRange { get; set; }
        public string? PostDateRange { get; set; }
    }

    internal sealed class ExtraPeriodCache
    {
        private readonly Dictionary<string, List<Contest>> _items = new(StringComparer.Ordinal);

        public static string WeekKey(string sportName, int week) => $"{sportName}|w|{week}";
        public static string DateKey(string sportName, string contestDate) => $"{sportName}|d|{contestDate}";

        public void RemoveBySport(string sportName)
        {
            var prefix = sportName + "|";
            var keys = _items.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            foreach (var key in keys)
                _items.Remove(key);
        }

        public async Task<List<Contest>> GetOrFetch(string key, bool allowFetch, Func<Task<List<Contest>>> fetch)
        {
            if (!allowFetch)
            {
                return _items.TryGetValue(key, out var cached)
                    ? cached.ToList()
                    : new List<Contest>();
            }

            var data = await fetch().ConfigureAwait(false) ?? new List<Contest>();
            _items[key] = data;
            return data.ToList();
        }
    }
}
