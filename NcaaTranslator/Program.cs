using System.Linq;
using System.Text.Json;
using System.Timers;
using System.Xml.Serialization;

namespace NcaaTranslator
{
    internal class Program
    {
        private static System.Timers.Timer aTimer;
        private static DateTime StartTime = DateTime.Now;

        static void Main()
        {
            SetTimer();

            Console.ReadLine();
            aTimer.Stop();
            aTimer.Dispose();

            Console.WriteLine("Terminating the application...");
        }
        private static void SetTimer()
        {
            aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += ConvertNcaaScoreboard;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        private static void Load()
        {
            NameConverters.Load();
            Settings.Load();
            aTimer.Interval = Settings.Timer;
            Console.Clear();
            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", StartTime);
        }
        private static async void ConvertNcaaScoreboard(Object source, ElapsedEventArgs e)
        {
            Load();
            Console.WriteLine("The scores were last updated at {0:HH:mm:ss.fff}", e.SignalTime);
            Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}\t{4}", "Sport".PadRight("Sport".Length + (15 - "Sport".Length)), "Total", "Conf", "NonConf", "Display"));

            var sportsList = Settings.GetSports();

            for (var i = 0; i < sportsList.Count; i++)
            {
                try
                {
                    var responseBody = "";
                    responseBody = await NcaaResponse(await GetUrl(sportsList[i].NcaaUrl));

                    if (responseBody == "")
                        continue;

                    NcaaScoreboard ncaaGames = JsonSerializer.Deserialize<NcaaScoreboard>(json: responseBody);
                    ncaaGames.games.Sort((x, y) => int.Parse(x.game.startTimeEpoch).CompareTo(int.Parse(y.game.startTimeEpoch)));

                    var displayList = Settings.GetDisplayTeams();
                    ncaaGames.filteredGames.Add(new ConferenceGames
                    {
                        conferenceName = "nonConf",
                        customConferenceName = "HKY",
                        conferenceSeo = "nonConf",
                        games = new List<GameData>()
                    });

                    foreach (var gameData in ncaaGames.games)
                    {
                        FixNames(gameData);
                        if (gameData.game.home.conferences[0].conferenceName == sportsList[i].ConferenceName ||
                            gameData.game.away.conferences[0].conferenceName == sportsList[i].ConferenceName)
                        {
                            if (gameData.game.home.names.char6 == Settings.homeTeam || gameData.game.away.names.char6 == Settings.homeTeam)
                            {
                                ncaaGames.homeGames.Add(gameData);
                            }
                            else
                            {
                                ncaaGames.conferenceGames.Add(gameData);

                                if (sportsList[i].OosUpdater.Enabled)
                                    ncaaGames.displayGames.Add(gameData);
                            }
                        }
                        else
                        {
                            ncaaGames.nonConferenceGames.Add(gameData);
                            if (sportsList[i].OosUpdater.Enabled)
                            {
                                if (displayList.Any(x => x.NcaaTeamName == gameData.game.home.names.char6 || x.NcaaTeamName == gameData.game.away.names.char6
                                                            || x.NcaaTeamName == gameData.game.home.names.shortOriginal || x.NcaaTeamName == gameData.game.away.names.shortOriginal
                                                            || x.NcaaTeamName == gameData.game.home.names.@short || x.NcaaTeamName == gameData.game.away.names.@short))
                                    ncaaGames.displayGames.Add(gameData);
                            }
                        }
                        if (gameData.game.home.conferences[0].conferenceName == gameData.game.away.conferences[0].conferenceName)
                        {
                            if (!ncaaGames.filteredGames.Any(x => x.conferenceName == gameData.game.home.conferences[0].conferenceName))
                            {
                                ncaaGames.filteredGames.Add(new ConferenceGames
                                {
                                    conferenceName = gameData.game.home.conferences[0].conferenceName,
                                    customConferenceName = gameData.game.home.conferences[0].customConferenceName,
                                    conferenceSeo = gameData.game.home.conferences[0].conferenceSeo,
                                    games = new List<GameData>()
                                });
                            }
                            ncaaGames.filteredGames.FirstOrDefault(x => x.conferenceName == gameData.game.home.conferences[0].conferenceName).games.Add(gameData.game);
                        }
                        else
                            ncaaGames.filteredGames.FirstOrDefault(x => x.conferenceName == "nonConf").games.Add(gameData.game);

                        if (gameData.game.home.conferences.Any(x => x.conferenceName == "Top 25") || gameData.game.away.conferences.Any(x => x.conferenceName == "Top 25"))
                            ncaaGames.top25Games.Add(gameData);
                    }

                    Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}\t{4}", sportsList[i].SportName.PadRight($"{sportsList[i].SportName}:".Length + (15 - $"{sportsList[i].SportName}:".Length)),
                                                                                ncaaGames.games.Count, ncaaGames.conferenceGames.Count, ncaaGames.nonConferenceGames.Count, ncaaGames.displayGames.Count));

                    File.WriteAllText(string.Format("{0}-Games.json", sportsList[i].SportName), JsonSerializer.Serialize<NcaaScoreboard>(ncaaGames, new JsonSerializerOptions() { WriteIndented = true }));

                    if (sportsList[i].OosUpdater.Enabled)
                        UpdateOos(ncaaGames, sportsList[i].OosUpdater);

                }
                catch (Exception err)
                {
                    Console.WriteLine("Message :{0} ", err.Message);
                }
            }
        }
        private static void FixNames(Game gameData)
        {
            var homeShortLookup = NameConverters.LookupTeam(gameData.game.home.names);
            var awayShortLookup = NameConverters.LookupTeam(gameData.game.away.names);

            gameData.game.home.names.shortOriginal = gameData.game.home.names.@short;
            gameData.game.away.names.shortOriginal = gameData.game.away.names.@short;

            gameData.game.home.names.@short = homeShortLookup != "" ? homeShortLookup : gameData.game.home.names.shortOriginal;
            gameData.game.home.conferences[0].customConferenceName = NameConverters.LookupConf(gameData.game.home.conferences[0]);
            gameData.game.away.names.@short = awayShortLookup != "" ? awayShortLookup : gameData.game.away.names.shortOriginal;
            gameData.game.away.conferences[0].customConferenceName = NameConverters.LookupConf(gameData.game.away.conferences[0]);
        }
        private static async Task<string> GetUrl(string sportUrl)
        {
            var response = await NcaaResponse("https://data.ncaa.com/casablanca/schedule/" + sportUrl + "today.json");
            if (response == "")
                throw new Exception("Unable to reach " + "https://data.ncaa.com/casablanca/schedule/" + sportUrl + "today.json");
            NcaaToday responseDeserialized = JsonSerializer.Deserialize<NcaaToday>(json: response);
            return string.Format("https://data.ncaa.com/casablanca/scoreboard/" + sportUrl + responseDeserialized.today + "/" + "scoreboard.json");
        }
        private static async Task<string> NcaaResponse(string url)
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
        private static void UpdateOos(NcaaScoreboard ncaaScoreboard, OosUpdater updater)
        {
            Console.WriteLine();
            var numberOfGames = ncaaScoreboard.displayGames.Count;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ClsGFXTemplate));
            var gameNeeded = 0;
            for (var i = 1; i <= updater.NumberOfOutScores; i++)
            {
                ClsGFXTemplate outScore;
                using (Stream reader = new FileStream(Path.Combine(updater.OosFilePath, updater.OosFileName + i + ".tmp"), FileMode.Open))
                {
                    outScore = (ClsGFXTemplate)xmlSerializer.Deserialize(reader);
                }
                for (var j = 1; j <= updater.NumberOfTeamsPer; j++)
                {
                    var gameData = ncaaScoreboard.displayGames[gameNeeded];
                    var homeTeam = gameData.game.home.names.@short;
                    var awayTeam = gameData.game.away.names.@short;
                    var homeScore = gameData.game.home.score;
                    var awayScore = gameData.game.away.score;
                    var clock = gameData.game.displayClockDefault;

                    Console.WriteLine("{0}\t{1}\tVS\t{2}\t{3}\tCLOCK\t{4}", homeTeam.Replace(".", "").PadRight($"{homeTeam}:".Length + (15 - $"{homeTeam}:".Length)),
                                                                            homeScore.Replace(".", "").PadRight($"{homeScore}:".Length + (2 - $"{homeScore}:".Length)),
                                                                            awayTeam.Replace(".", "").PadRight($"{awayTeam}:".Length + (15 - $"{awayTeam}:".Length)),
                                                                            awayScore.Replace(".", "").PadRight($"{awayScore}:".Length + (2 - $"{awayScore}:".Length)),
                                                                            clock);

                    outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - V Team", j)).GraphicObjText = awayTeam;
                    outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - V Score", j)).GraphicObjText = awayScore;
                    outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - H Team", j)).GraphicObjText = homeTeam;
                    outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - H Score", j)).GraphicObjText = homeScore;

                    if (gameData.game.gameState == "pre" || gameData.game.gameState == "final")
                    {
                        outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Time", j)).GraphicObjText = "";
                        outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Quarter", j)).GraphicObjText = clock;
                    }
                    else
                    {
                        outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Time", j)).GraphicObjText = gameData.game.contestClock;
                        outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Quarter", j)).GraphicObjText = gameData.game.currentPeriod;
                    }
                    gameNeeded++;
                    if (gameNeeded >= numberOfGames)
                    {
                        gameNeeded = 0;
                    }
                    TextWriter writer = new StreamWriter(Path.Combine(updater.OosFilePath, updater.OosFileName + i + ".tmp"));
                    xmlSerializer.Serialize(writer, outScore);
                    writer.Close();
                }
            }
            Console.WriteLine();
        }
    }
}