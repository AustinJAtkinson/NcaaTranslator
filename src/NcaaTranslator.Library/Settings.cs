using System.Net.Http.Headers;
using System.Runtime.InteropServices;
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

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class Setting
    {
        public int Timer { get; set; }
        public string? HomeTeam { get; set; }
        public List<Sport>? Sports { get; set; }
        public List<DisplayTeam>? DisplayTeams { get; set; }

        public XmlToJson? XmlToJson{ get; set; }
    }

    public class Sport : INotifyPropertyChanged
    {
        private bool _enabled = true;
        private GameDisplayMode _gameDisplayMode = GameDisplayMode.Live;

        public string? SportName { get; set; }

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

        public GameDisplayMode GameDisplayMode
        {
            get => _gameDisplayMode;
            set
            {
                if (_gameDisplayMode != value)
                {
                    _gameDisplayMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameDisplayMode)));
                }
            }
        }

        public string? ConferenceName { get; set; }
        public string? SportCode { get; set; }
        public int Division { get; set; }
        public int? Week { get; set; }
        public OosUpdater OosUpdater { get; set; } = new OosUpdater();
        public ListsNeeded ListsNeeded { get; set; } = new ListsNeeded();

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class Settings
    {
        public static Setting? SettingsList { get; set; }
        internal static string fileName = "Settings.json";

        public static void Load()
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            string jsonString = File.ReadAllText(fileName);
            SettingsList = JsonSerializer.Deserialize<Setting>(jsonString, options)!;
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
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string jsonString = JsonSerializer.Serialize(SettingsList, options);
            File.WriteAllText(fileName, jsonString);
        }

    }

}