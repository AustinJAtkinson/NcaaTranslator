using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using static NcaaTranslator.NameConverter;

namespace NcaaTranslator
{
    internal class NameConverter
    {
        public List<Team> teams { get; set; } = new List<Team>();
        public List<Conferences> conferences { get; set; } = new List<Conferences>();
    }
    internal class Team : Names
    {
        public Team() { }
        public Team(Names names)
        {
            this.char6 = names.char6;
            this.shortOriginal = names.@short;
            this.@short = "";
        }
    }
    internal class Conferences : Conference
    {
        public Conferences() { }
        public Conferences(Conference names)
        {
            this.conferenceName = names.conferenceName;
            this.conferenceSeo = names.conferenceSeo;
            this.customConferenceName = "";
        }
    }

    public class NameConverters
    {
        internal static Dictionary<String, Team> TeamDict { get; set; } = new Dictionary<String, Team>();
        internal static Dictionary<String, Conferences> ConfDict { get; set; } = new Dictionary<String, Conferences>();
        internal static NameConverter NameList { get; set; }
        internal static string FilePath = "NcaaNameConverter.json";

        public static void Load()
        {
            string jsonString = File.ReadAllText(FilePath);
            NameList = JsonSerializer.Deserialize<NameConverter>(jsonString);
            TeamDict = NameList.teams.ToDictionary(x => x.char6, x => x);
            ConfDict = NameList.conferences.ToDictionary(x => x.conferenceName, x => x);
        }
        public static void Reload()
        {
            NameList.teams.OrderBy(x => x.char6);
            NameList.conferences.OrderBy(x => x.conferenceName);
            var test = JsonSerializer.Serialize<NameConverter>(NameList, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(FilePath, JsonSerializer.Serialize(NameList, new JsonSerializerOptions() { WriteIndented = true }));
            Load();
        }

        public static string LookupTeam(Names lookupNames)
        {
            var name = new Team();

            return TeamDict.TryGetValue(lookupNames.char6, out name) ? name.@short : AddNewTeam(lookupNames);
        }
        public static string AddNewTeam(Names names)
        {
            var newTeam = new Team(names);
            NameList.teams.Add(newTeam);
            Reload();
            return "";
        }

        public static string LookupConf(Conference lookupNames)
        {
            var name = new Conferences();

            return ConfDict.TryGetValue(lookupNames.conferenceName, out name) ? name.customConferenceName : AddNewConf(lookupNames);
        }
        public static string AddNewConf(Conference names)
        {
            var newConf = new Conferences(names);
            NameList.conferences.Add(newConf);
            Reload();
            return "";
        }
    }
}
