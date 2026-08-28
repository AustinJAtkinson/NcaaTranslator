using System.Diagnostics;
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
            string? contestDate = week.HasValue ? null : DateTime.Now.ToString("MM/dd/yyyy");
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
            if (!sport.Enabled)
                return new NcaaScoreboard();

            var responseBody = await NcaaResponse(GetUrl(sport));

            if (responseBody == "")
                return new NcaaScoreboard();

            NcaaScoreboard ncaaGames = JsonSerializer.Deserialize<NcaaScoreboard>(json: responseBody)!;
            if (ncaaGames.data?.contests == null)
                return ncaaGames;

            ncaaGames.data.contests.Sort((x, y) => x.startTimeEpoch.CompareTo(y.startTimeEpoch));

            CategorizeGames(ncaaGames, sport);

            var exportData = new NcaaScoreboard
            {
                data = new Data
                {
                    contests = ncaaGames.data.contests,
                    nonConferenceGames = sport.ListsNeeded.nonConferenceGames ? ncaaGames.data.nonConferenceGames : null,
                    conferenceGames = sport.ListsNeeded.conferenceGames ? ncaaGames.data.conferenceGames : null,
                    displayGames = ncaaGames.data.displayGames,
                    homeGames = ncaaGames.data.homeGames,
                    top25Games = ncaaGames.data.top25Games
                }
            };

            File.WriteAllText(string.Format("{0}-Games.json", sport.SportName!), JsonSerializer.Serialize<NcaaScoreboard>(exportData));

            if (sport.OosUpdater.Enabled)
                UpdateOos(ncaaGames, sport.OosUpdater);

            return ncaaGames;
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
}
