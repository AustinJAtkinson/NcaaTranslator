using System.Collections.Generic;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Timers;

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

            NameConverters.Load("NcaaNameConverter.txt");

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
                            catch
                            {
                            }
                        }
                    }
                    finally
                    {
                        client.Dispose();
                    }


                    NcaaScoreboard ncaaGames = JsonSerializer.Deserialize<NcaaScoreboard>(json: responseBody);

                    List<Game> nonConferenceGames = new List<Game>();
                    Game undGameId = new Game();

                    foreach (var gameData in ncaaGames.games)
                    {
                        FixNames(gameData);

                        if (gameData.game.home.conferences[0].conferenceName != SportsList[i].ConferenceName ||
                            gameData.game.away.conferences[0].conferenceName != SportsList[i].ConferenceName)
                        {
                            nonConferenceGames.Add(gameData);
                        }
                        else
                        {
                            if (gameData.game.home.names.char6 == "NO DAK" || gameData.game.home.names.char6 == "NO DAK")
                            {
                                undGameId = gameData;
                            }
                        }
                    }
                    Console.WriteLine(String.Format("{0} - {1} Total Games", ncaaGames.games.Count, SportsList[i].SportName));

                    ncaaGames.games.RemoveAll(x => nonConferenceGames.Contains(x) || x == undGameId);
                    ncaaGames.games.Sort((x, y) => int.Parse(x.game.gameID).CompareTo(int.Parse(y.game.gameID)));

                    Console.WriteLine(String.Format("{0} - {1} Conferance Games", ncaaGames.games.Count, SportsList[i].SportName));
                    Console.WriteLine(String.Format("{0} - {1} NonConferance Games", nonConferenceGames.Count, SportsList[i].SportName));

                    string conferenceGames = JsonSerializer.Serialize<NcaaScoreboard>(ncaaGames);
                    File.WriteAllText(string.Format("{0}-{1}Games.json", SportsList[i].SportName, SportsList[i].ConferenceName.Replace(" ", "")), conferenceGames);


                    ncaaGames.games.Clear();
                    ncaaGames.games.AddRange(nonConferenceGames);
                    ncaaGames.games.Sort((x, y) => int.Parse(x.game.gameID).CompareTo(int.Parse(y.game.gameID)));


                    string top25Games = JsonSerializer.Serialize<NcaaScoreboard>(ncaaGames);
                    File.WriteAllText(string.Format("{0}-NonConferenceGames.json", SportsList[i].SportName), top25Games);

                    if (undGameId.game != null)
                    {
                        ncaaGames.games.Clear();
                        ncaaGames.games.Add(undGameId);

                        string undGame = JsonSerializer.Serialize<NcaaScoreboard>(ncaaGames);
                        File.WriteAllText(string.Format("{0}-UndGame.json", SportsList[i].SportName), undGame);
                    }

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
            var homeShortLookup = NameConverters.Lookup(gameData.game.home.names.char6);
            var awayShortLookup = NameConverters.Lookup(gameData.game.away.names.char6);

            gameData.game.home.names.shortOriginal = gameData.game.home.names.@short;
            gameData.game.away.names.shortOriginal = gameData.game.away.names.@short;

            gameData.game.home.names.@short = homeShortLookup != "" ? homeShortLookup : gameData.game.home.names.shortOriginal;
            gameData.game.away.names.@short = awayShortLookup != "" ? awayShortLookup : gameData.game.away.names.shortOriginal;
        }
    }
}