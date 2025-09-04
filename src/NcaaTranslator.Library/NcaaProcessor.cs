using System.Text.Json;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NcaaTranslator.Library
{
    public class NcaaProcessor
    {
        public static void FixNames(Game gameData)
        {
            var homeShortLookup = NameConverters.LookupTeam(gameData.game!.home!.names!);
            var awayShortLookup = NameConverters.LookupTeam(gameData.game!.away!.names!);

            gameData.game!.home!.names!.shortOriginal = gameData.game!.home!.names!.@short;
            gameData.game!.away!.names!.shortOriginal = gameData.game!.away!.names!.@short;

            gameData.game!.home!.names!.@short = homeShortLookup != "" ? homeShortLookup : gameData.game!.home!.names!.shortOriginal;
            gameData.game!.home!.conferences![0].customConferenceName = NameConverters.LookupConf(gameData.game!.home!.conferences![0]);
            gameData.game!.away!.names!.@short = awayShortLookup != "" ? awayShortLookup : gameData.game!.away!.names!.shortOriginal;
            gameData.game!.away!.conferences![0].customConferenceName = NameConverters.LookupConf(gameData.game!.away!.conferences![0]);
        }

        public static async Task<string> GetUrl(string sportUrl)
        {
            var response = await NcaaResponse("https://data.ncaa.com/casablanca/schedule/" + sportUrl + "today.json");
            if (response == "")
                throw new Exception("Unable to reach " + "https://data.ncaa.com/casablanca/schedule/" + sportUrl + "today.json");
            NcaaToday responseDeserialized = JsonSerializer.Deserialize<NcaaToday>(json: response)!;
            return string.Format("https://data.ncaa.com/casablanca/scoreboard/" + sportUrl + responseDeserialized.today + "/" + "scoreboard.json");
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
            var numberOfGames = ncaaScoreboard.displayGames.Count;
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
                    var gameData = ncaaScoreboard.displayGames[gameNeeded];
                    var homeTeam = gameData.game!.home!.names!.@short!;
                    var awayTeam = gameData.game!.away!.names!.@short!;
                    var homeScore = gameData.game!.home!.score!;
                    var awayScore = gameData.game!.away!.score!;
                    var clock = gameData.game!.displayClockDefault;

                    Console.WriteLine("{0}\t{1}\tVS\t{2}\t{3}\tCLOCK\t{4}", homeTeam.Replace(".", "").PadRight($"{homeTeam}:".Length + (15 - $"{homeTeam}:".Length)),
                                                                            homeScore.Replace(".", "").PadRight($"{homeScore}:".Length + (2 - $"{homeScore}:".Length)),
                                                                            awayTeam.Replace(".", "").PadRight($"{awayTeam}:".Length + (15 - $"{awayTeam}:".Length)),
                                                                            awayScore.Replace(".", "").PadRight($"{awayScore}:".Length + (2 - $"{awayScore}:".Length)),
                                                                            clock);

                    outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - V Team", j))!.GraphicObjText = awayTeam;
                    outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - V Score", j))!.GraphicObjText = awayScore;
                    outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - H Team", j))!.GraphicObjText = homeTeam;
                    outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - H Score", j))!.GraphicObjText = homeScore;

                    if (gameData.game!.gameState == "pre" || gameData.game!.gameState == "final")
                    {
                        outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Time", j))!.GraphicObjText = "";
                        outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Quarter", j))!.GraphicObjText = clock;
                    }
                    else
                    {
                        outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Time", j))!.GraphicObjText = gameData.game!.contestClock;
                        outScore.GfxElements!.ClsGFXElement!.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Quarter", j))!.GraphicObjText = gameData.game!.currentPeriod;
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
            responseBody = await NcaaResponse(await GetUrl(sport.NcaaUrl!));

            if (responseBody == "")
                return new NcaaScoreboard();

            NcaaScoreboard ncaaGames = JsonSerializer.Deserialize<NcaaScoreboard>(json: responseBody)!;
            ncaaGames.games.Sort((x, y) => int.Parse(x.game!.startTimeEpoch!).CompareTo(int.Parse(y.game!.startTimeEpoch!)));

            var displayList = Settings.GetDisplayTeams();
            ncaaGames.filteredGames.Add(new ConferenceGames
            {
                conferenceName = "nonConf",
                customConferenceName = sport.SportNameShort,
                conferenceSeo = "nonConf",
                games = new List<Game>()
            });

            foreach (var gameData in ncaaGames.games)
            {
                FixNames(gameData);
                gameData.game!.conferenceDisplayName = gameData.game!.home!.conferences![0].customConferenceName! == "" ? gameData.game!.home!.conferences![0].conferenceName! : gameData.game!.home!.conferences![0].customConferenceName!;

                if (gameData.game!.home!.conferences![0].conferenceName == sport.ConferenceName ||
                    gameData.game!.away!.conferences![0].conferenceName == sport.ConferenceName)
                {
                    if (gameData.game!.home!.names!.char6 == Settings.homeTeam || gameData.game!.away!.names!.char6 == Settings.homeTeam)
                    {
                        ncaaGames.homeGames.Add(gameData);
                    }
                    else
                    {
                        ncaaGames.conferenceGames.Add(gameData);

                        if (sport.OosUpdater.Enabled)
                            ncaaGames.displayGames.Add(gameData);
                    }
                }
                else
                {
                    ncaaGames.nonConferenceGames.Add(gameData);
                    if (sport.OosUpdater.Enabled)
                    {
                        if (displayList!.Any(x => x.NcaaTeamName == gameData.game!.home!.names!.char6 || x.NcaaTeamName == gameData.game!.away!.names!.char6
                                                            || x.NcaaTeamName == gameData.game!.home!.names!.shortOriginal || x.NcaaTeamName == gameData.game!.away!.names!.shortOriginal
                                                            || x.NcaaTeamName == gameData.game!.home!.names!.@short || x.NcaaTeamName == gameData.game!.away!.names!.@short))
                            ncaaGames.displayGames.Add(gameData);
                    }

                    if (gameData.game!.home!.conferences![0].conferenceName == gameData.game!.away!.conferences![0].conferenceName && gameData.game!.home!.conferences![0].conferenceName != "DI Independent")
                    {
                        if (!ncaaGames.filteredGames.Any(x => x.conferenceName == gameData.game!.home!.conferences![0].conferenceName))
                        {
                            ncaaGames.filteredGames.Add(new ConferenceGames
                            {
                                conferenceName = gameData.game!.home!.conferences![0].conferenceName,
                                customConferenceName = gameData.game!.home!.conferences![0].customConferenceName,
                                conferenceSeo = gameData.game!.home!.conferences![0].conferenceSeo,
                                games = new List<Game>()
                            });
                        }
                        ncaaGames.filteredGames.FirstOrDefault(x => x.conferenceName == gameData.game!.home!.conferences![0].conferenceName)!.games.Add(gameData);
                    }
                    else
                    {
                        gameData.game!.conferenceDisplayName = sport.SportNameShort!;
                        ncaaGames.filteredGames.FirstOrDefault(x => x.conferenceName == "nonConf")!.games.Add(gameData);
                    }
                }
                if (gameData.game!.home!.conferences!.Any(x => x.conferenceName == "Top 25") || gameData.game!.away!.conferences!.Any(x => x.conferenceName == "Top 25"))
                    ncaaGames.top25Games.Add(gameData);
            }

            foreach (var conf in ncaaGames.filteredGames)
            {
                ncaaGames.nonConferenceSorted.AddRange(conf.games);
            }

            Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}\t{4}", sport.SportName!.PadRight($"{sport.SportName}:".Length + (15 - $"{sport.SportName}:".Length)),
                                                                 ncaaGames.games.Count, ncaaGames.conferenceGames.Count, ncaaGames.nonConferenceGames.Count, ncaaGames.displayGames.Count));

            if (!sport.ListsNeeded.games)
                ncaaGames.games.Clear();
            if (!sport.ListsNeeded.nonConferenceGames)
                ncaaGames.nonConferenceGames.Clear();
            if (!sport.ListsNeeded.nonConferenceSorted)
                ncaaGames.nonConferenceSorted.Clear();
            if (!sport.ListsNeeded.conferenceGames)
                ncaaGames.conferenceGames.Clear();
            if (!sport.ListsNeeded.top25Games)
                ncaaGames.top25Games.Clear();
            if (!sport.ListsNeeded.filteredGames)
                ncaaGames.filteredGames.Clear();

            File.WriteAllText(string.Format("{0}-Games.json", sport.SportName!), JsonSerializer.Serialize<NcaaScoreboard>(ncaaGames, new JsonSerializerOptions() { WriteIndented = true }));

            if (sport.OosUpdater.Enabled)
                UpdateOos(ncaaGames, sport.OosUpdater);

            return ncaaGames;
        }

        public static void ConvertXmlToJson(XmlToJson xmlToJson)
        {
            if (!xmlToJson.Enabled)
                return;

            Console.WriteLine("\nConverting XML to Json");
            foreach (var filePath in xmlToJson.FilePaths!)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath.Path!);

                var jsonText = JsonConvert.SerializeXmlNode(doc);
                File.WriteAllText(string.Format("{0}.json", filePath.Path!), jsonText);
                Console.WriteLine(string.Format("File {0} was converted to json in {1}", filePath.Path!, string.Format("{0}.json", filePath.Path!)));
            }
        }
    }
}