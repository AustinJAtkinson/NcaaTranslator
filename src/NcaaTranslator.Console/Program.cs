using System.Linq;
using System.Text.Json;
using System.Timers;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NcaaTranslator.Library;

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

            foreach (var sport in sportsList)
            {
                try
                {
                    await NcaaProcessor.ConvertNcaaScoreboard(sport);
                }
                catch (Exception err)
                {
                    Console.WriteLine("Message :{0} ", err.Message);
                }
            }
            try
            {
                NcaaProcessor.ConvertXmlToJson(Settings.XmlToJson);
            }
            catch { }
        }
    }
}