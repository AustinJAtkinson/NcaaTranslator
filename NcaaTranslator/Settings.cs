﻿using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace NcaaTranslator
{
    public class DisplayTeam
    {
        public string NcaaTeamName { get; set; }
    }

    public class OosUpdater
    {
        public bool Enabled { get; set; }
        public string OosFilePath { get; set; }
        public string OosFileName { get; set; }
        public int NumberOfOutScores { get; set; }
        public int NumberOfTeamsPer { get; set; }
    }

    public class XmlToJson
    {
        public bool Enabled { get; set; }
        public List<FilePath> FilePaths { get; set; }
    }

    public class FilePath
    {
        public string Path { get; set;}
    }

    public class ListsNeeded
    {
        public bool games { get; set; } = true;
        public bool nonConferenceGames { get; set; } = true;
        public bool nonConferenceSorted { get; set; } = true;
        public bool conferenceGames { get; set; } = true;
        public bool top25Games { get; set; } = true;
        public bool filteredGames { get; set; } = true;
    }

    public class Setting
    {
        public int Timer { get; set; }
        public string HomeTeam { get; set; }
        public List<Sport> Sports { get; set; }
        public List<DisplayTeam> DisplayTeams { get; set; }

        public XmlToJson XmlToJson{ get; set; }
    }

    public class Sport
    {
        public string SportName { get; set; }
        public string SportNameShort { get; set; }
        public string ConferenceName { get; set; }
        public string NcaaUrl { get; set; }
        public OosUpdater OosUpdater { get; set; } = new OosUpdater();
        public ListsNeeded ListsNeeded { get; set; } = new ListsNeeded();
    }

    public class Settings
    {
        internal static Setting SettingsList { get; set; }
        internal static string fileName = "Settings.json";

        public static void Load()
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            string jsonString = File.ReadAllText(fileName);
            SettingsList = JsonSerializer.Deserialize<Setting>(jsonString, options);
        }

        public static List<Sport> GetSports()
        {
            return SettingsList.Sports;
        }

        public static List<DisplayTeam> GetDisplayTeams()
        {
            return SettingsList.DisplayTeams;
        }

        public static int Timer{ get {return SettingsList.Timer * 1000; }}
        public static string homeTeam {get { return SettingsList.HomeTeam; }}
        public static XmlToJson XmlToJson {get { return SettingsList.XmlToJson;}}

    }

}
