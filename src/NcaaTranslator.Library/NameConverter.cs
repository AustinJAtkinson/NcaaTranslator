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

        public static void Load(string? path = null)
        {
            if (path != null)
                FilePath = path;

            string jsonString = File.ReadAllText(FilePath);
            NameList = JsonSerializer.Deserialize<NameConverter>(jsonString)!;
            TeamDict = ToLastWinsDictionary(NameList!.teams, x => x.name6Char);
            ConfDict = ToLastWinsDictionary(NameList!.conferences, x => x.conferenceSeo);
        }

        private static Dictionary<string, T> ToLastWinsDictionary<T>(List<T> items, Func<T, string?> keySelector)
        {
            var dict = new Dictionary<string, T>();
            foreach (var item in items)
            {
                var key = keySelector(item);
                if (string.IsNullOrEmpty(key))
                    continue;
                dict[key] = item;
            }
            return dict;
        }

        public static void Reload()
        {
            NameList!.teams = NameList.teams.OrderBy(x => x.name6Char).ToList();
            NameList!.conferences = NameList.conferences.OrderBy(x => x.customConferenceName).ToList();
            File.WriteAllText(FilePath, JsonSerializer.Serialize(NameList));
            Load();
        }

        public static string LookupTeam(Names lookupNames)
        {
            if (lookupNames.name6Char == null) return "";
            var name = new Team();
            if (TeamDict.TryGetValue(lookupNames.name6Char, out name))
            {
                return name.customName ?? "";
            }
            return AddNewTeam(lookupNames);
        }
        public static string AddNewTeam(Names names)
        {
            names.customName ??= names.nameShort;
            var newTeam = new Team(names);
            NameList!.teams.Add(newTeam);
            Reload();
            return newTeam.customName ?? "";
        }

        public static string LookupConf(Conference lookupNames)
        {
            if (lookupNames.conferenceSeo == null) return "";
            var name = new Conferences();

            return ConfDict.TryGetValue(lookupNames.conferenceSeo!, out name) ? name.customConferenceName ?? "" : AddNewConf(lookupNames);
        }
        public static string AddNewConf(Conference names)
        {
            names.customConferenceName ??= names.conferenceSeo;
            var newConf = new Conferences(names);
            NameList!.conferences.Add(newConf);
            Reload();
            return newConf.customConferenceName ?? "";
        }

    }
}