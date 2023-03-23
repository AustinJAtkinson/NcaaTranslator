using System.Text.Json;
using System.Timers;
using System.Xml.Serialization;

namespace NcaaTranslator
{
    internal class Program
    {
        private static System.Timers.Timer aTimer;

        private static List<SportConference> SportsList = new List<SportConference>();

        private static DateTime StartTime = DateTime.Now;

        class SportConference
        {
            public string ConferenceName { get; set; }
            public string SportUrl { get; set; }
            public string SportName { get; set; }
        }


        static void Main()
        {
            SportsList.Add(new SportConference { ConferenceName = "NCHC", SportUrl = "icehockey-men/d1/", SportName = "Hockey" });
            SportsList.Add(new SportConference { ConferenceName = "Summit League", SportUrl = "basketball-men/d1/", SportName = "MBB" });
            SportsList.Add(new SportConference { ConferenceName = "Summit League", SportUrl = "basketball-women/d1/", SportName = "WBB" });
            //SportsList.Add(new SportConference { ConferenceName = "Summit League", SportUrl = "scoreboard/football/fcs/" });

            NameConverters.Load();

            SetTimer();

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", StartTime);
            Console.ReadLine();
            aTimer.Stop();
            aTimer.Dispose();

            Console.WriteLine("Terminating the application...");
        }
        private static void SetTimer()
        {
            // Create a timer with a 20 second interval.
            aTimer = new System.Timers.Timer(20000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEventAsync;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static async void OnTimedEventAsync(Object source, ElapsedEventArgs e)
        {
            Console.Clear();
            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff} \n", StartTime);
            Console.WriteLine("The scores were last updated at {0:HH:mm:ss.fff}", e.SignalTime);

            var dateYear = DateTime.Now.Year;
            var dateMonth = DateTime.Now.ToString("MM");
            var dateDay = DateTime.Now.ToString("dd");

            for (var i = 0; i < SportsList.Count; i++)
            {
                string url = "https://data.ncaa.com/casablanca/scoreboard/";
                string fileName = "scoreboard.json";

                try
                {
                    HttpClient client = new HttpClient();
                    var tempUrl = string.Format(url + SportsList[i].SportUrl + dateYear + "/" + dateMonth + "/" + dateDay + "/" + fileName);
                    string responseBody = "";

                    try//try to get today first
                    {
                        responseBody = await client.GetStringAsync(tempUrl);
                    }
                    catch
                    {
                        tempUrl = string.Format(url + SportsList[i].SportUrl + dateYear + "/" + dateMonth + "/" + DateTime.Today.AddDays(-1).ToString("dd") + "/" + fileName);

                        try//then try yesterday
                        {
                            responseBody = await client.GetStringAsync(tempUrl);
                        }
                        catch
                        {
                            tempUrl = string.Format(url + SportsList[i].SportUrl + dateYear + "/" + dateMonth + "/" + DateTime.Today.AddDays(+1).ToString("dd") + "/" + fileName);

                            try//last try tomorrow
                            {
                                responseBody = await client.GetStringAsync(tempUrl);
                            }
                            catch { }
                        }
                    }
                    finally
                    {
                        client.Dispose();
                    }

                    NcaaScoreboard ncaaGames = JsonSerializer.Deserialize<NcaaScoreboard>(json: responseBody);
                    if(!ncaaGames.games.Any(x=> x.game.gameID == ""))
                    {
                        ncaaGames.games.Sort((x, y) => int.Parse(x.game.gameID).CompareTo(int.Parse(y.game.gameID)));
                    }
                    else if(!ncaaGames.games.Any(x => x.game.bracketId == ""))
                    {
                        ncaaGames.games.Sort((x, y) => int.Parse(x.game.bracketId).CompareTo(int.Parse(y.game.bracketId)));
                    }
                        

                    foreach (var gameData in ncaaGames.games)
                    {
                        FixNames(gameData);

                        if (gameData.game.home.conferences[0].conferenceName != SportsList[i].ConferenceName ||
                            gameData.game.away.conferences[0].conferenceName != SportsList[i].ConferenceName)
                        {
                            ncaaGames.nonConferenceGames.Add(gameData);
                        }
                        else
                        {
                            if (gameData.game.home.names.char6 == "NO DAK" || gameData.game.away.names.char6 == "NO DAK")
                            {
                                ncaaGames.undGames.Add(gameData);
                            }
                            else
                            {
                                ncaaGames.conferenceGames.Add(gameData);
                            }
                        }
                    }

                    Console.WriteLine(String.Format("{0} - {1} Total Games", ncaaGames.games.Count, SportsList[i].SportName));
                    Console.WriteLine(String.Format("{0} - {1} Conferance Games", ncaaGames.conferenceGames.Count, SportsList[i].SportName));
                    Console.WriteLine(String.Format("{0} - {1} NonConferance Games", ncaaGames.nonConferenceGames.Count, SportsList[i].SportName));

                    File.WriteAllText(string.Format("{0}-Games.json", SportsList[i].SportName), JsonSerializer.Serialize<NcaaScoreboard>(ncaaGames, new JsonSerializerOptions() { WriteIndented = true }));

                    TextWriter writer = new StreamWriter(string.Format("{0}-Games.xml", SportsList[i].SportName));
                    XmlSerializer x = new XmlSerializer(ncaaGames.GetType());
                    x.Serialize(writer, ncaaGames);
                    writer.Close();
                }
                catch (HttpRequestException err)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", err.Message);
                }
            }
        }
        private static void FixNames(Game gameData)
        {
            var homeShortLookup = NameConverters.Lookup(gameData.game.home.names);
            var awayShortLookup = NameConverters.Lookup(gameData.game.away.names);

            gameData.game.home.names.shortOriginal = gameData.game.home.names.@short;
            gameData.game.away.names.shortOriginal = gameData.game.away.names.@short;

            gameData.game.home.names.@short = homeShortLookup != "" ? homeShortLookup : gameData.game.home.names.shortOriginal;
            gameData.game.away.names.@short = awayShortLookup != "" ? awayShortLookup : gameData.game.away.names.shortOriginal;
        }
    }
}