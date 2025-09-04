
namespace NcaaTranslator.Library
{
    public class NcaaToday
    {
        public string? inputMD5Sum { get; set; }
        public string? description { get; set; }
        public string? today { get; set; }
    }

    public class NcaaScoreboard
    {
        public string? inputMD5Sum { get; set; }
        public string? updated_at { get; set; }
        public List<Game> games { get; set; } = new List<Game>();
        public List<Game> nonConferenceGames { get; set; } = new List<Game>();
        public List<Game> nonConferenceSorted { get; set; } = new List<Game>();
        public List<Game> conferenceGames { get; set; } = new List<Game>();
        public List<Game> displayGames { get; set; } = new List<Game>();
        public List<Game> homeGames { get; set; } = new List<Game>();
        public List<Game> top25Games { get; set; } = new List<Game>();
        public List<ConferenceGames> filteredGames { get; set; } = new List<ConferenceGames>();
    }
    public class Away
    {
        public string? score { get; set; }
        public Names? names { get; set; }
        public bool winner { get; set; }
        public string? seed { get; set; }
        public string? description { get; set; }
        public string? rank { get; set; }
        public List<Conference>? conferences { get; set; }
    }

    public class Conference
    {
        public string? conferenceName { get; set; }
        public string? customConferenceName { get; set; }
        public string? conferenceSeo { get; set; }
    }

    public class ConferenceGames : Conference
    {
        public List<Game> games { get; set; } = new List<Game>();
    }

    public class Game
    {
        public GameData? game { get; set; }
    }

    public class GameData
    {
        public string? gameID { get; set; }
        public Away? away { get; set; }
        public string conferenceDisplayName { get; set; } = "";
        public string? finalMessage { get; set; }
        public string? bracketRound { get; set; }
        public string? title { get; set; }
        public string? contestName { get; set; }
        public string? url { get; set; }
        public string? network { get; set; }
        public Home? home { get; set; }
        public bool liveVideoEnabled { get; set; }
        public string? startTime { get; set; }
        public string ctStateTime
        {
            get
            {
                try
                {
                    if (startTime == "TBA")
                        return startTime!;

                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dateTime = dateTime.AddSeconds(int.Parse(startTimeEpoch!)).ToLocalTime();

                    return dateTime.ToString("h:mm tt");
                }
                catch
                {
                    return startTime!;
                }

            }
            set { }
        }
        public string? startTimeEpoch { get; set; }
        public string? bracketId { get; set; }
        public string? gameState { get; set; }
        public string? startDate { get; set; }
        public string? currentPeriod { get; set; }
        public string? videoState { get; set; }
        public string? bracketRegion { get; set; }
        public string? contestClock { get; set; }
        public string displayClock
        {
            get
            {
                if ( gameState == "pre")
                {
                    return ctStateTime;
                }
                if (gameState == "final")
                {
                    return finalMessage!.Replace("2OT", "SO");
                }

                return string.Format("{0}     {1}", currentPeriod!.Replace("2OT", "SO"), contestClock!);
            }
            set { }
        }
        public string displayClockDefault
        {
            get
            {
                if (gameState == "pre")
                {
                    return ctStateTime;
                }
                if (gameState == "final")
                {
                    return finalMessage!;
                }

                return string.Format("{0}     {1}", currentPeriod!, contestClock!);
            }
            set { }
        }
    }

    public class Home
    {
        public string? score { get; set; }
        public Names? names { get; set; }
        public bool winner { get; set; }
        public string? seed { get; set; }
        public string? description { get; set; }
        public string? rank { get; set; }
        public List<Conference>? conferences { get; set; }
    }

    public class Names
    {
        public string? char6 { get; set; }
        public string? @short { get; set; }
        public string? shortOriginal { get; set; }
        public string? seo { get; set; }
        public string? full { get; set; }
    }

    
}