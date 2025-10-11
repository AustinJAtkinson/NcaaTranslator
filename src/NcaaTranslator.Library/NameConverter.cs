using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using static NcaaTranslator.Library.NameConverter;

namespace NcaaTranslator.Library
{
    public class NameConverter
    {
        public List<Team> teams { get; set; } = new List<Team>();
        public List<Conferences> conferences { get; set; } = new List<Conferences>();
    }
    public class Team : Names
    {
        public Team() { }
        public Team(Names names)
        {
            this.seoname = names.seoname;
            this.nameShort = names.nameShort;
            this.name6Char = names.name6Char;
            this.customName = names.customName;
        }
    }
    public class Conferences : Conference
    {
        public Conferences() { }
        public Conferences(Conference names)
        {
            this.customConferenceName = names.customConferenceName;
            this.conferenceSeo = names.conferenceSeo;
        }
    }

    public class NameConverters
    {
        internal static Dictionary<string, Team> TeamDict { get; set; } = new Dictionary<string, Team>();
        internal static Dictionary<string, Conferences> ConfDict { get; set; } = new Dictionary<string, Conferences>();
        public static NameConverter? NameList { get; set; }
        // DO NOT CHANGE THIS PATH - it is correct as is
        internal static string FilePath = "NcaaNameConverter.json";

        public static List<Team> GetTeams()
        {
            return NameList?.teams ?? new List<Team>();
        }

        public static List<Conferences> GetConferences()
        {
            return NameList?.conferences ?? new List<Conferences>();
        }

        public static void Load()
        {
            string jsonString = File.ReadAllText(FilePath);
            NameList = JsonSerializer.Deserialize<NameConverter>(jsonString)!;
            // Use name6Char as the key
            TeamDict = NameList!.teams.ToDictionary(x => x.name6Char!, x => x);
            ConfDict = NameList!.conferences.ToDictionary(x => x.conferenceSeo!, x => x);
        }
        public static void Reload()
        {
            NameList!.teams.OrderBy(x => x.name6Char);
            NameList!.conferences.OrderBy(x => x.customConferenceName);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(NameList));
            Load();
        }

        public static string LookupTeam(Names lookupNames)
        {
            if (lookupNames.name6Char == null) return "";
            var name = new Team();
            if (TeamDict.TryGetValue(lookupNames.name6Char, out name))
            {
                // Return customName if available, otherwise nameShort
                return name.customName;
            }
            return AddNewTeam(lookupNames);
        }
        public static string AddNewTeam(Names names)
        {
            names.customName ??= names.nameShort;
            var newTeam = new Team(names);
            NameList!.teams.Add(newTeam);
            Reload();
            return "";
        }

        public static string LookupConf(Conference lookupNames)
        {
            if (lookupNames.conferenceSeo == null) return "";
            var name = new Conferences();

            return ConfDict.TryGetValue(lookupNames.conferenceSeo!, out name) ? name.customConferenceName! : AddNewConf(lookupNames);
        }
        public static string AddNewConf(Conference names)
        {
            names.customConferenceName ??= names.conferenceSeo;
            var newConf = new Conferences(names);
            NameList!.conferences.Add(newConf);
            Reload();
            return "";
        }

    }
}