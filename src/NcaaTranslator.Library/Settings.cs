using System.Text.Json;
using System.ComponentModel;

namespace NcaaTranslator.Library
{
    public enum GameDisplayMode
    {
        Live,
        All,
        Display
    }

    public class DisplayTeam
    {
        public string? NcaaTeamName { get; set; }
    }

    public class OosUpdater : INotifyPropertyChanged
    {
        private bool _enabled;
        private string? _oosFilePath;
        private string? _oosFileName;
        private int _numberOfOutScores;
        private int _numberOfTeamsPer;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
                }
            }
        }

        public string? OosFilePath
        {
            get => _oosFilePath;
            set
            {
                if (_oosFilePath != value)
                {
                    _oosFilePath = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OosFilePath)));
                }
            }
        }

        public string? OosFileName
        {
            get => _oosFileName;
            set
            {
                if (_oosFileName != value)
                {
                    _oosFileName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OosFileName)));
                }
            }
        }

        public int NumberOfOutScores
        {
            get => _numberOfOutScores;
            set
            {
                if (_numberOfOutScores != value)
                {
                    _numberOfOutScores = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberOfOutScores)));
                }
            }
        }

        public int NumberOfTeamsPer
        {
            get => _numberOfTeamsPer;
            set
            {
                if (_numberOfTeamsPer != value)
                {
                    _numberOfTeamsPer = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberOfTeamsPer)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class XmlToJson
    {
        public bool Enabled { get; set; }
        public List<FilePath>? FilePaths { get; set; }
    }

    public class FilePath
    {
        public string? Path { get; set;}
    }

    public class ListsNeeded : INotifyPropertyChanged
    {
        private bool _top25Games = true;
        private bool _conferenceGames = true;
        private bool _nonConferenceGames = true;

        public bool top25Games
        {
            get => _top25Games;
            set
            {
                if (_top25Games != value)
                {
                    _top25Games = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(top25Games)));
                }
            }
        }

        public bool conferenceGames
        {
            get => _conferenceGames;
            set
            {
                if (_conferenceGames != value)
                {
                    _conferenceGames = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(conferenceGames)));
                }
            }
        }

        public bool nonConferenceGames
        {
            get => _nonConferenceGames;
            set
            {
                if (_nonConferenceGames != value)
                {
                    _nonConferenceGames = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(nonConferenceGames)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class ClockFormat
    {
        public const string PreGamePattern = "{dayofweek}{separator}{text}";
        public const string PreGameSeparator = ". ";
        public const string FinalPattern = "{text}{separator}{dayofweek}";
        public const string FinalSeparator = " - ";

        public bool? IncludeWeekday { get; set; }
        public bool? FullWeekday { get; set; }
        public string? Separator { get; set; }
        public string? Pattern { get; set; }

        public static ClockFormat PreGameDefaults() => new()
        {
            IncludeWeekday = true,
            FullWeekday = false,
            Separator = PreGameSeparator,
            Pattern = PreGamePattern
        };

        public static ClockFormat FinalDefaults() => new()
        {
            IncludeWeekday = true,
            FullWeekday = false,
            Separator = FinalSeparator,
            Pattern = FinalPattern
        };

        public static ClockFormat ResolvePreGame(ClockFormat? raw) => Resolve(raw, PreGameDefaults());

        public static ClockFormat ResolveFinal(ClockFormat? raw) => Resolve(raw, FinalDefaults());

        public string Apply(string text, DateTimeOffset localStart)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (IncludeWeekday != true)
                return text;

            if (localStart.Date == DateTime.Today)
                return text;

            if (string.IsNullOrEmpty(Pattern))
                return text;

            var day = FullWeekday == true
                ? localStart.ToString("dddd")
                : localStart.ToString("ddd").TrimEnd('.');

            return Pattern
                .Replace("{dayofweek}", day, StringComparison.OrdinalIgnoreCase)
                .Replace("{separator}", Separator ?? "", StringComparison.OrdinalIgnoreCase)
                .Replace("{text}", text, StringComparison.OrdinalIgnoreCase);
        }

        private static ClockFormat Resolve(ClockFormat? raw, ClockFormat defaults) => new()
        {
            IncludeWeekday = raw?.IncludeWeekday ?? defaults.IncludeWeekday,
            FullWeekday = raw?.FullWeekday ?? defaults.FullWeekday,
            Separator = raw?.Separator ?? defaults.Separator,
            Pattern = raw?.Pattern ?? defaults.Pattern
        };
    }

    public class ClockFormats
    {
        public ClockFormat? PreGame { get; set; }
        public ClockFormat? Final { get; set; }
    }

    public class Setting
    {
        public int Timer { get; set; }
        public string? HomeTeam { get; set; }
        public List<Sport>? Sports { get; set; }
        public List<DisplayTeam>? DisplayTeams { get; set; }

        public XmlToJson? XmlToJson{ get; set; }
        public ClockFormats? ClockFormats { get; set; }
    }

    public class Sport : INotifyPropertyChanged
    {
        private bool _enabled = true;
        private GameDisplayMode _gameDisplayMode = GameDisplayMode.Live;
        private string _sportName = "";
        private string _sportShortName = "";
        private string? _conferenceName;
        private string? _sportCode;
        private int _division;
        private int? _week;
        private int? _seasonYear;
        private int _lookBack;
        private int _lookForward;

        public required string SportName
        {
            get => _sportName;
            set
            {
                if (_sportName != value)
                {
                    _sportName = value;
                    Raise(nameof(SportName));
                }
            }
        }

        public required string SportShortName
        {
            get => _sportShortName;
            set
            {
                if (_sportShortName != value)
                {
                    _sportShortName = value;
                    Raise(nameof(SportShortName));
                }
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    Raise(nameof(Enabled));
                }
            }
        }

        public GameDisplayMode GameDisplayMode
        {
            get => _gameDisplayMode;
            set
            {
                if (_gameDisplayMode != value)
                {
                    _gameDisplayMode = value;
                    Raise(nameof(GameDisplayMode));
                }
            }
        }

        public string? ConferenceName
        {
            get => _conferenceName;
            set
            {
                if (_conferenceName != value)
                {
                    _conferenceName = value;
                    Raise(nameof(ConferenceName));
                }
            }
        }

        public string? SportCode
        {
            get => _sportCode;
            set
            {
                if (_sportCode != value)
                {
                    _sportCode = value;
                    Raise(nameof(SportCode));
                }
            }
        }

        public int Division
        {
            get => _division;
            set
            {
                if (_division != value)
                {
                    _division = value;
                    Raise(nameof(Division));
                }
            }
        }

        public int? Week
        {
            get => _week;
            set
            {
                if (_week != value)
                {
                    _week = value;
                    Raise(nameof(Week));
                }
            }
        }

        public int? SeasonYear
        {
            get => _seasonYear;
            set
            {
                if (_seasonYear != value)
                {
                    _seasonYear = value;
                    Raise(nameof(SeasonYear));
                }
            }
        }

        public int LookBack
        {
            get => _lookBack;
            set
            {
                if (_lookBack != value)
                {
                    _lookBack = value;
                    Raise(nameof(LookBack));
                }
            }
        }

        public int LookForward
        {
            get => _lookForward;
            set
            {
                if (_lookForward != value)
                {
                    _lookForward = value;
                    Raise(nameof(LookForward));
                }
            }
        }

        public OosUpdater OosUpdater { get; set; } = new OosUpdater();
        public ListsNeeded ListsNeeded { get; set; } = new ListsNeeded();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Raise(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Settings
    {
        public static Setting? SettingsList { get; set; }
        public static string BaseDirectory { get; set; } = AppContext.BaseDirectory;
        // DO NOT CHANGE THIS PATH - it is correct as is
        internal static string fileName = "Settings.json";

        internal static string ResolvePath()
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException("Settings file name is not set.");

            return Path.IsPathRooted(fileName)
                ? Path.GetFullPath(fileName)
                : Path.GetFullPath(Path.Combine(BaseDirectory, fileName));
        }

        public static void Load(string? path = null)
        {
            if (path != null)
                fileName = path;

            var resolved = ResolvePath();
            if (!File.Exists(resolved))
                throw new FileNotFoundException($"Settings file not found: {resolved}", resolved);

            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            string jsonString = File.ReadAllText(resolved);
            SettingsList = JsonSerializer.Deserialize<Setting>(jsonString, options);
            if (SettingsList == null)
                throw new InvalidDataException($"Settings file is empty or invalid: {resolved}");
        }

        public static List<Sport>? GetSports()
        {
            return SettingsList!.Sports;
        }

        public static List<DisplayTeam>? GetDisplayTeams()
        {
            return SettingsList!.DisplayTeams;
        }

        public static int Timer{ get {return SettingsList!.Timer * 1000; }}
        public static string? homeTeam {get { return SettingsList!.HomeTeam; }}
        public static XmlToJson? XmlToJson {get { return SettingsList!.XmlToJson;}}

        public static void Save()
        {
            File.WriteAllText(ResolvePath(), JsonSerializer.Serialize(SettingsList));
        }

    }

}