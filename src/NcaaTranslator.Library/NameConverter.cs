using System.Text.Json;

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
        public static string BaseDirectory { get; set; } = AppContext.BaseDirectory;
        // DO NOT CHANGE THIS PATH - it is correct as is
        internal static string FilePath = "NcaaNameConverter.json";

        internal static string ResolvePath()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
                throw new InvalidOperationException("Name converter file path is not set.");

            return Path.IsPathRooted(FilePath)
                ? Path.GetFullPath(FilePath)
                : Path.GetFullPath(Path.Combine(BaseDirectory, FilePath));
        }

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

            var resolved = ResolvePath();
            if (!File.Exists(resolved))
                throw new FileNotFoundException($"Name converter file not found: {resolved}", resolved);

            string jsonString = File.ReadAllText(resolved);
            NameList = JsonSerializer.Deserialize<NameConverter>(jsonString);
            if (NameList == null)
                throw new InvalidDataException($"Name converter file is empty or invalid: {resolved}");
            NameList.teams ??= new List<Team>();
            NameList.conferences ??= new List<Conferences>();
            if (DedupeUnkeyedEntries(NameList))
                File.WriteAllText(resolved, JsonSerializer.Serialize(NameList));
            TeamDict = ToLastWinsDictionary(NameList.teams, x => x.name6Char);
            ConfDict = ToLastWinsDictionary(NameList.conferences, x => x.conferenceSeo);
        }

        /// <summary>
        /// NCAA omits name6Char / conferenceSeo for some schools. Those rows never
        /// enter the lookup dictionaries, so every poll used to append another copy.
        /// Keep at most one unkeyed team per seoname; drop blank conferences.
        /// </summary>
        internal static bool DedupeUnkeyedEntries(NameConverter list)
        {
            var teamsChanged = false;
            var keptTeams = new List<Team>(list.teams.Count);
            var seenSeo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in list.teams)
            {
                if (!string.IsNullOrWhiteSpace(team.name6Char))
                {
                    keptTeams.Add(team);
                    if (!string.IsNullOrWhiteSpace(team.seoname))
                        seenSeo.Add(team.seoname);
                    continue;
                }

                teamsChanged = true;
                if (string.IsNullOrWhiteSpace(team.seoname))
                    continue;
                if (!seenSeo.Add(team.seoname))
                    continue;
                keptTeams.Add(team);
            }

            var keptConfs = new List<Conferences>(list.conferences.Count);
            var confsChanged = false;
            foreach (var conference in list.conferences)
            {
                if (string.IsNullOrWhiteSpace(conference.conferenceSeo))
                {
                    confsChanged = true;
                    continue;
                }
                keptConfs.Add(conference);
            }

            list.teams = keptTeams;
            list.conferences = keptConfs;
            return teamsChanged || confsChanged;
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
            File.WriteAllText(ResolvePath(), JsonSerializer.Serialize(NameList));
            Load();
        }

        public static string LookupTeam(Names lookupNames)
        {
            if (TryFindTeam(lookupNames, out var existing) && existing != null)
                return existing.customName ?? "";

            if (string.IsNullOrWhiteSpace(lookupNames.name6Char) && string.IsNullOrWhiteSpace(lookupNames.seoname))
                return lookupNames.nameShort ?? lookupNames.customName ?? "";

            return AddNewTeam(lookupNames);
        }
        public static string AddNewTeam(Names names)
        {
            if (TryFindTeam(names, out var existing) && existing != null)
                return existing.customName ?? "";

            if (string.IsNullOrWhiteSpace(names.name6Char) && string.IsNullOrWhiteSpace(names.seoname))
                return names.nameShort ?? names.customName ?? "";

            names.customName ??= names.nameShort;
            var newTeam = new Team(names);
            NameList!.teams.Add(newTeam);
            Reload();
            return newTeam.customName ?? "";
        }

        public static string LookupConf(Conference lookupNames)
        {
            if (string.IsNullOrWhiteSpace(lookupNames.conferenceSeo))
                return "";

            return ConfDict.TryGetValue(lookupNames.conferenceSeo, out var name)
                ? name.customConferenceName ?? ""
                : AddNewConf(lookupNames);
        }
        public static string AddNewConf(Conference names)
        {
            if (string.IsNullOrWhiteSpace(names.conferenceSeo))
                return names.customConferenceName ?? "";

            if (ConfDict.TryGetValue(names.conferenceSeo, out var existing))
                return existing.customConferenceName ?? "";

            names.customConferenceName ??= names.conferenceSeo;
            var newConf = new Conferences(names);
            NameList!.conferences.Add(newConf);
            Reload();
            return newConf.customConferenceName ?? "";
        }

        private static bool TryFindTeam(Names lookupNames, out Team? team)
        {
            team = null;
            if (!string.IsNullOrWhiteSpace(lookupNames.name6Char) &&
                TeamDict.TryGetValue(lookupNames.name6Char, out team))
                return true;

            if (string.IsNullOrWhiteSpace(lookupNames.seoname) || NameList?.teams == null)
                return false;

            team = NameList.teams.FirstOrDefault(t =>
                string.Equals(t.seoname, lookupNames.seoname, StringComparison.OrdinalIgnoreCase));
            return team != null;
        }

    }
}