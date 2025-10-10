
using System.Text.Json.Serialization;

namespace NcaaTranslator.Library
{
    public class NcaaScoreboard
    {
        public Data? data { get; set; }
    }

    public class Data
    {
        public List<Contest> contests { get; set; } = new List<Contest>();
        public List<Contest> nonConferenceGames { get; set; } = new List<Contest>();
        public List<Contest> nonConferenceSorted { get; set; } = new List<Contest>();
        public List<Contest> conferenceGames { get; set; } = new List<Contest>();
        public List<Contest> displayGames { get; set; } = new List<Contest>();
        public List<Contest> homeGames { get; set; } = new List<Contest>();
        public List<Contest> top25Games { get; set; } = new List<Contest>();
        public List<ConferenceGames> filteredGames { get; set; } = new List<ConferenceGames>();
    }

    public class Contest
    {
        public long contestId { get; set; }
        public string? gameState { get; set; }
        public string? statusCodeDisplay { get; set; }
        public string? currentPeriod { get; set; }
        public string? contestClock { get; set; }
        public string? finalMessage { get; set; }
        public long startTimeEpoch { get; set; }
        public string? startTime { get; set; }
        public string? startDate { get; set; }
        public bool hasStartTime { get; set; }
        public bool tba { get; set; }
        public List<ContestTeam> teams { get; set; } = new List<ContestTeam>();
        public string ctStateTime
        {
            get
            {
                try
                {
                    if (startTime == "TBA")
                        return startTime!;

                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dateTime = dateTime.AddSeconds(startTimeEpoch).ToLocalTime();

                    return dateTime.ToString("h:mm tt");
                }
                catch
                {
                    return startTime!;
                }
            }
        }
        public string displayClock
        {
            get
            {
                if (gameState == "P")
                {
                    return ctStateTime;
                }
                if (gameState == "F")
                {
                    return finalMessage!.Replace("2OT", "SO");
                }

                return string.Format("{0}     {1}", currentPeriod!.Replace("2OT", "SO"), contestClock!);
            }
        }
        public string displayClockDefault
        {
            get
            {
                if (gameState == "P")
                {
                    return ctStateTime;
                }
                if (gameState == "F")
                {
                    return finalMessage!;
                }

                return string.Format("{0}     {1}", currentPeriod!, contestClock!);
            }
        }

    }

    public class ContestTeam
    {
        public bool isHome { get; set; }
        public string? seoname { get; set; }
        public string? nameShort { get; set; }
        public string? name6Char { get; set; }
        public string? seed { get; set; }
        public int? teamRank { get; set; }
        public int? score { get; set; }
        public bool isWinner { get; set; }
        public string? conferenceSeo { get; set; }
        public string? customConferenceName { get; set; }
    }

    public class Names
    {
        // New properties for updated team name layout
        [JsonPropertyName("seoname")]
        public string? seoname { get; set; }

        [JsonPropertyName("nameShort")]
        public string? nameShort { get; set; }

        [JsonPropertyName("name6Char")]
        public string? name6Char { get; set; }

        [JsonPropertyName("customName")]
        public string? customName { get; set; }
    }

    public class Conference
    {
        public string? customConferenceName { get; set; }
        public string? conferenceSeo { get; set; }
    }

    public class ConferenceGames : Conference
    {
        public List<Contest> games { get; set; } = new List<Contest>();
    }
}