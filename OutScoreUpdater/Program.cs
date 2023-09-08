using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Timers;
using System.Xml.Serialization;

namespace OutScoreUpdater
{
    internal class Program
    {
        private static System.Timers.Timer aTimer;

        private static DateTime StartTime = DateTime.Now;

        static void Main(string[] args)
        {
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

            var fileInfo = File.ReadAllLines("FilePath.txt");
            var footballJsonInfo = fileInfo[1];
            var outScoreFileInfo = fileInfo[0].Split('\t');
            var footballScoresTxt = File.ReadAllText(footballJsonInfo);
            NcaaScoreboard footballGames = JsonSerializer.Deserialize<NcaaScoreboard>(footballScoresTxt);
            var numberOfConfGames = footballGames.conferenceGames.Count;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ClsGFXTemplate));
            var gameNeeded = 0;
            for(var i= 1; i<=8; i++) 
            {
                ClsGFXTemplate outScore;
                using (Stream reader = new FileStream(Path.Combine(outScoreFileInfo[0], outScoreFileInfo[1] + i +".tmp"), FileMode.Open))
                {
                    outScore = (ClsGFXTemplate)xmlSerializer.Deserialize(reader);
                }                
                for (var j = 1; j <= 2; j++)
                {
                    var gameData = footballGames.conferenceGames[gameNeeded];
                    var homeTeam = gameData.game.home.names.@short;
                    var awayTeam = gameData.game.away.names.@short;
                    var homeScore = gameData.game.home.score == "" ? "0" : gameData.game.home.score;
                    var awayScore = gameData.game.away.score == "" ? "0" : gameData.game.away.score;
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
                    outScore.GfxElements.ClsGFXElement.FirstOrDefault(x => x.GraphicObjName == string.Format("G{0} - Time", j)).GraphicObjText = clock;
                    
                    gameNeeded++;
                    if (gameNeeded >= numberOfConfGames)
                    {
                        gameNeeded = 0;
                    }
                    TextWriter writer = new StreamWriter(Path.Combine(outScoreFileInfo[0], outScoreFileInfo[1] + i + ".tmp"));
                    xmlSerializer.Serialize(writer, outScore);
                    writer.Close();
                }
            }
            
        }
    }
}
