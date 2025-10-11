using System.Text.Json;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NcaaTranslator.Library
{
    public class NcaaProcessor
    {
        public static void FixNames(Contest gameData)
        {
            var home = gameData.teams.FirstOrDefault(t => t.isHome);
            var away = gameData.teams.FirstOrDefault(t => !t.isHome);

            if (home != null)
            {
                home.customName = NameConverters.LookupTeam(new Names { name6Char = home.name6Char, nameShort = home.nameShort, seoname = home.seoname });;
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
            int seasonYear = DateTime.Now.Year;
            int? week = GetCurrentWeek(sport.SportCode!);
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
            return $"https://sdataprod.ncaa.com/?meta=GetContests_web&extensions={{\"persistedQuery\":{{\"version\":1,\"sha256Hash\":\"7287cda610a9326931931080cb3a604828febe6fe3c9016a7e4a36db99efdb7c\"}}}}&variables={variablesJson}";
        }

        private static int? GetCurrentWeek(string sportCode)
        {
            var sport = Settings.GetSports()?.FirstOrDefault(s => s.SportCode == sportCode);
            return sport?.Week; // Returns null if not found or Week is null
        }

        public static async Task<string> NcaaResponse(string url)
        {
            HttpClient client = new HttpClient();
            string ret = "";
            try
            {
                ret = await client.GetStringAsync(url);
            }
            catch
            {
            }
            finally
            {
                client.Dispose();
            }
            return ret;
        }

        public static void UpdateOos(NcaaScoreboard ncaaScoreboard, OosUpdater updater)
        {
            Console.WriteLine();
            var numberOfGames = ncaaScoreboard.data!.displayGames.Count;
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
                    var gameData = ncaaScoreboard.data!.displayGames[gameNeeded];
                    var home = gameData.teams.FirstOrDefault(t => t.isHome);
                    var away = gameData.teams.FirstOrDefault(t => !t.isHome);
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

                    outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - V Team", j))!.GraphicObjText = awayTeam;
                    outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - V Score", j))!.GraphicObjText = awayScore;
                    outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - H Team", j))!.GraphicObjText = homeTeam;
                    outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - H Score", j))!.GraphicObjText = homeScore;

                    if (gameData.gameState == "P" || gameData.gameState == "F")
                    {
                        outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Time", j))!.GraphicObjText = "";
                        outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Quarter", j))!.GraphicObjText = clock;
                    }
                    else
                    {
                        outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Time", j))!.GraphicObjText = gameData.contestClock;
                        outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Quarter", j))!.GraphicObjText = gameData.currentPeriod;
                    }
                    gameNeeded++;
                    if (gameNeeded >= numberOfGames)
                    {
                        gameNeeded = 0;
                    }
                    TextWriter writer = new StreamWriter(Path.Combine(updater.OosFilePath!, updater.OosFileName! + i + ".tmp"));
                    xmlSerializer.Serialize(writer, outScore);
                    writer.Close();
                }
            }
            Console.WriteLine();
        }

        public static async Task<NcaaScoreboard> ConvertNcaaScoreboard(Sport sport)
        {
            if (!sport.Enabled)
                return new NcaaScoreboard();

            var responseBody = "";
            responseBody = await NcaaResponse(GetUrl(sport));

            if (responseBody == "")
                return new NcaaScoreboard();

            NcaaScoreboard ncaaGames = JsonSerializer.Deserialize<NcaaScoreboard>(json: responseBody)!;
            ncaaGames.data!.contests.Sort((x, y) => x.startTimeEpoch.CompareTo(y.startTimeEpoch));

            var displayList = Settings.GetDisplayTeams();

            foreach (var gameData in ncaaGames.data!.contests)
            {
                FixNames(gameData);
                var home = gameData.teams.FirstOrDefault(t => t.isHome);

                var homeTeamObj = gameData.teams.FirstOrDefault(t => t.isHome);
                var awayTeamObj = gameData.teams.FirstOrDefault(t => !t.isHome);

                if (string.Equals(homeTeamObj?.customConferenceName, awayTeamObj?.customConferenceName, StringComparison.OrdinalIgnoreCase))
                {
                    gameData.conferenceDisplayName = homeTeamObj?.customConferenceName;
                }
                else
                {
                    gameData.conferenceDisplayName = sport.SportShortName;
                }

                if (string.Equals(homeTeamObj?.customConferenceName, sport.ConferenceName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(awayTeamObj?.customConferenceName, sport.ConferenceName, StringComparison.OrdinalIgnoreCase))
                {
                    if (homeTeamObj?.name6Char == Settings.homeTeam || awayTeamObj?.name6Char == Settings.homeTeam)
                    {
                        ncaaGames.data!.homeGames.Add(gameData);
                    }
                    else
                    {
                        ncaaGames.data!.conferenceGames.Add(gameData);

                        if (sport.OosUpdater.Enabled || sport.GameDisplayMode == GameDisplayMode.Display)
                            ncaaGames.data!.displayGames.Add(gameData);
                    }
                }
                else
                {
                    ncaaGames.data!.nonConferenceGames.Add(gameData);
                    if (sport.OosUpdater.Enabled || sport.GameDisplayMode == GameDisplayMode.Display)
                    {
                        var homeTeamDisp = gameData.teams.FirstOrDefault(t => t.isHome);
                        var awayTeamDisp = gameData.teams.FirstOrDefault(t => !t.isHome);
                        if (displayList!.Any(x => x.NcaaTeamName == homeTeamDisp?.name6Char || x.NcaaTeamName == awayTeamDisp?.name6Char
                                                        || x.NcaaTeamName == homeTeamDisp?.nameShort || x.NcaaTeamName == awayTeamDisp?.nameShort))
                            ncaaGames.data!.displayGames.Add(gameData);
                    }

                    var homeConf = gameData.teams.FirstOrDefault(t => t.isHome)?.conferenceSeo;
                    var awayConf = gameData.teams.FirstOrDefault(t => !t.isHome)?.conferenceSeo;

                }
                var homeRank = gameData.teams.FirstOrDefault(t => t.isHome)?.teamRank;
                var awayRank = gameData.teams.FirstOrDefault(t => !t.isHome)?.teamRank;
                bool homeTop25 = homeRank.HasValue && homeRank >= 1 && homeRank <= 25;
                bool awayTop25 = awayRank.HasValue && awayRank >= 1 && awayRank <= 25;
                if (sport.ListsNeeded.top25Games && (homeTop25 || awayTop25))
                    ncaaGames.data!.top25Games.Add(gameData);
            }

            // Sort nonConferenceGames: games with home.customConferenceName == SportShortName first, then alpha sort by conferenceDisplayName, then by startTimeEpoch
            ncaaGames.data!.nonConferenceGames = ncaaGames.data!.nonConferenceGames
                .OrderBy(g => string.Equals(g.conferenceDisplayName, sport.SportShortName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(g => g.conferenceDisplayName)
                .ThenBy(g => g.startTimeEpoch)
                .ToList();

            ncaaGames.data!.contests!.Clear();
            ncaaGames.data.contests = null;

            if (ncaaGames.data!.displayGames!.Count == 0) ncaaGames.data.displayGames = null;
            if (ncaaGames.data!.top25Games!.Count == 0) ncaaGames.data.top25Games = null;

            File.WriteAllText(string.Format("{0}-Games.json", sport.SportName!), JsonSerializer.Serialize<NcaaScoreboard>(ncaaGames));

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
                File.WriteAllText(string.Format("{0}.json", filePath.Path!), jsonText);
            }
        }
    }
}