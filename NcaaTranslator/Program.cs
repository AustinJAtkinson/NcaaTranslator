using System.Text.Json;
using System.Timers;

namespace NcaaTranslator
{
    internal class Program
    {
        private static System.Timers.Timer aTimer;

        private static List<SportConference> SportsList = new List<SportConference>();

        class SportConference
        {
            public string ConferenceName { get; set; }
            public string SportUrl { get; set; }
            public string SportName { get; set; }
        }


        static void Main()
        {
            SportsList.Add(new SportConference { ConferenceName = "NCHC", SportUrl = "icehockey-men/d1/", SportName="Hockey" });
            //SportsList.Add(new SportConference { ConferenceName = "Summit League", SportUrl = "basketball-men/d1/" });
            //SportsList.Add(new SportConference { ConferenceName = "Summit League", SportUrl = "basketball-women/d1/" });
            //SportsList.Add(new SportConference { ConferenceName = "Summit League", SportUrl = "scoreboard/football/fcs/" });

            SetTimer();

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
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
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);

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


                    NcaaScoreboard ncaaConferenceGames = JsonSerializer.Deserialize<NcaaScoreboard>(json: responseBody);
                    NcaaScoreboard ncaaTop25Games = JsonSerializer.Deserialize<NcaaScoreboard>(json: responseBody);


                    List<string> nonConferenceGamesToRemove = new List<string>();
                    List<string> notTop25Rank = new List<string>();

                    foreach (var gameData in ncaaConferenceGames.games)
                    {
                        if(gameData.game.home.conferences[0].conferenceName != SportsList[i].ConferenceName ||
                            gameData.game.away.conferences[0].conferenceName != SportsList[i].ConferenceName)
                        {
                            nonConferenceGamesToRemove.Add(gameData.game.gameID);

                            if (gameData.game.home.rank == "" && gameData.game.away.rank == "")
                            {
                                notTop25Rank.Add(gameData.game.gameID);
                            }
                        }
                        else
                        {
                            notTop25Rank.Add(gameData.game.gameID);                         
                        }
                    }

                    ncaaConferenceGames.games.RemoveAll(x => nonConferenceGamesToRemove.Contains(x.game.gameID));
                    ncaaConferenceGames.games.Sort((x, y) => int.Parse(x.game.gameID).CompareTo(int.Parse(y.game.gameID)));
                    string conferenceGames = JsonSerializer.Serialize<NcaaScoreboard>(ncaaConferenceGames);
                    File.WriteAllText(string.Format("{0}-NCHCGames.json", SportsList[i].SportName), conferenceGames);

                    ncaaTop25Games.games.RemoveAll(x => notTop25Rank.Contains(x.game.gameID));
                    ncaaTop25Games.games.Sort((x, y) => int.Parse(x.game.gameID).CompareTo(int.Parse(y.game.gameID)));
                    string top25Games = JsonSerializer.Serialize<NcaaScoreboard>(ncaaTop25Games);
                    File.WriteAllText(string.Format("{0}-Top25NonConferenceGames.json", SportsList[i].SportName), top25Games);
                }
                catch (HttpRequestException err)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", err.Message);
                }
            }
        }
    }
}