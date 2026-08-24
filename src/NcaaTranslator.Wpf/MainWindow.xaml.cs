using System.Windows;
using System.Windows.Controls;
using System.Timers;
using NcaaTranslator.Library;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NcaaTranslator.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private System.Timers.Timer? aTimer;
    private DateTime StartTime = DateTime.Now;
    private List<Team> _originalTeams = new List<Team>();
    private List<Conferences> _originalConferences = new List<Conferences>();
    private List<Sport> _originalSports = new List<Sport>();
    private List<TeamOption> _originalAddTeamOptions = new List<TeamOption>();
    private List<TeamOption> _teamOptions = new List<TeamOption>();
    private Dictionary<string, NcaaScoreboard> _sportScoreboards = new Dictionary<string, NcaaScoreboard>();
    private ObservableCollection<SportGamesViewModel> _sportTabs = new ObservableCollection<SportGamesViewModel>();
    private readonly SingleFlightGate _conversionGate = new();
    private bool _configLoaded;
    private bool _settingsUiLoaded;
    private bool _convertersUiLoaded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SportGamesViewModel> SportTabs
    {
        get => _sportTabs;
        set
        {
            _sportTabs = value;
            OnPropertyChanged(nameof(SportTabs));
        }
    }

    public GameDisplayMode[] GameDisplayModes { get; } = (GameDisplayMode[])Enum.GetValues(typeof(GameDisplayMode));
    public List<string> ConferenceNames { get; set; } = new List<string>();

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeTimer();
        SubscribeGridHandlers();
        LoadInitialData();
    }

    private void SubscribeGridHandlers()
    {
        TeamsDataGrid.CellEditEnding += TeamsDataGrid_CellEditEnding;
        ConferencesDataGrid.CellEditEnding += ConferencesDataGrid_CellEditEnding;
        AddTeamComboBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
            new TextChangedEventHandler(AddTeamComboBox_TextChanged));
    }

    private void InitializeTimer()
    {
        aTimer = new System.Timers.Timer(2000);
        aTimer.Elapsed += ConvertNcaaScoreboard;
        aTimer.AutoReset = true;
    }

    private void LoadInitialData()
    {
        try
        {
            NameConverters.Load();
            Settings.Load();
            aTimer!.Interval = Settings.Timer;
            _configLoaded = true;
        }
        catch (Exception ex)
        {
            _configLoaded = false;
            StartButton.IsEnabled = false;
            StatusText.Text = "Status: Load failed";
            MessageBox.Show($"Error loading configuration: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void StartProcess()
    {
        if (!_configLoaded || Settings.SettingsList == null)
        {
            MessageBox.Show("Configuration failed to load. Polling will not start.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        StatusText.Text = "Status: Running";

        await _conversionGate.RunAsync(() => PerformConversion(DateTime.Now));

        if (aTimer != null)
            aTimer.Enabled = true;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartProcess();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        aTimer!.Enabled = false;
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        StatusText.Text = "Status: Stopped";
    }

    private async void ConvertNcaaScoreboard(Object? source, ElapsedEventArgs e)
    {
        await _conversionGate.RunAsync(() => PerformConversion(e.SignalTime));
    }

    private async Task PerformConversion(DateTime signalTime)
    {
        Dispatcher.Invoke(() =>
        {
            LastUpdateText.Text = $"Last Update: {signalTime:HH:mm:ss.fff}";
        });

        var sportsList = Settings.GetSports();
        if (sportsList == null)
            return;

        foreach (var sport in sportsList)
        {
            try
            {
                var result = await NcaaProcessor.ConvertNcaaScoreboard(sport);
                Dispatcher.Invoke(() =>
                {
                    _sportScoreboards[sport.SportName!] = result;
                    UpdateSportsTabs();
                });
            }
            catch (Exception)
            {
                // Error processing sport - silently handle
            }
        }

        if (Settings.XmlToJson?.Enabled == true)
        {
            try
            {
                NcaaProcessor.ConvertXmlToJson(Settings.XmlToJson!);
            }
            catch (Exception)
            {
                // XML conversion error - silently handle
            }
        }
    }


    private void UpdateSportsTabs()
    {
        Dispatcher.Invoke(() =>
        {
            var enabledSports = Settings.GetSports()?.Where(s => s.Enabled) ?? Enumerable.Empty<Sport>();
            var keepNames = new HashSet<string>();
            var orderedNames = new List<string>();

            foreach (var sport in enabledSports)
            {
                if (string.IsNullOrEmpty(sport.SportName))
                    continue;

                if (!_sportScoreboards.TryGetValue(sport.SportName, out var scoreboard) || scoreboard.data == null)
                    continue;

                keepNames.Add(sport.SportName);
                orderedNames.Add(sport.SportName);

                var gamesToShow = GetGamesToShow(scoreboard.data, sport.GameDisplayMode);
                var existing = SportTabs.FirstOrDefault(t => t.SportName == sport.SportName);
                if (existing != null)
                {
                    existing.UpdateFrom(sport, scoreboard.data, gamesToShow);
                }
                else
                {
                    SportTabs.Add(new SportGamesViewModel
                    {
                        SportName = sport.SportName,
                        Sport = sport,
                        GameDisplayMode = sport.GameDisplayMode,
                        Games = gamesToShow,
                        ConfGamesCount = scoreboard.data.conferenceGames?.Count ?? 0,
                        NonConfGamesCount = scoreboard.data.nonConferenceGames?.Count ?? 0,
                        DisplayGamesCount = scoreboard.data.displayGames?.Count ?? 0,
                        HomeGamesCount = scoreboard.data.homeGames?.Count ?? 0
                    });
                }
            }

            for (int i = SportTabs.Count - 1; i >= 0; i--)
            {
                if (!keepNames.Contains(SportTabs[i].SportName))
                    SportTabs.RemoveAt(i);
            }

            for (int i = 0; i < orderedNames.Count; i++)
            {
                var currentIndex = -1;
                for (int j = 0; j < SportTabs.Count; j++)
                {
                    if (SportTabs[j].SportName == orderedNames[i])
                    {
                        currentIndex = j;
                        break;
                    }
                }

                if (currentIndex >= 0 && currentIndex != i)
                    SportTabs.Move(currentIndex, i);
            }
        });
    }

    private static List<Contest> GetGamesToShow(Data data, GameDisplayMode mode)
    {
        var conference = data.conferenceGames ?? new List<Contest>();
        var nonConference = data.nonConferenceGames ?? new List<Contest>();
        var home = data.homeGames ?? new List<Contest>();
        var allGames = new List<Contest>(conference.Count + nonConference.Count + home.Count);
        allGames.AddRange(conference);
        allGames.AddRange(nonConference);
        allGames.AddRange(home);

        return mode switch
        {
            GameDisplayMode.All => allGames,
            GameDisplayMode.Display => data.displayGames ?? new List<Contest>(),
            _ => allGames.Where(c => c.gameState == "I").ToList()
        };
    }

    public class SportGamesViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded = true;
        private GameDisplayMode _gameDisplayMode = GameDisplayMode.Live;
        private List<Contest> _games = new List<Contest>();
        private int _confGamesCount;
        private int _nonConfGamesCount;
        private int _displayGamesCount;
        private int _homeGamesCount;

        public string SportName { get; set; } = "";
        public Sport Sport { get; set; } = new Sport { SportName = "", SportShortName = "" };

        public List<Contest> Games
        {
            get => _games;
            set
            {
                _games = value;
                Raise(nameof(Games));
            }
        }

        public int ConfGamesCount
        {
            get => _confGamesCount;
            set
            {
                if (_confGamesCount != value)
                {
                    _confGamesCount = value;
                    Raise(nameof(ConfGamesCount));
                }
            }
        }

        public int NonConfGamesCount
        {
            get => _nonConfGamesCount;
            set
            {
                if (_nonConfGamesCount != value)
                {
                    _nonConfGamesCount = value;
                    Raise(nameof(NonConfGamesCount));
                }
            }
        }

        public int DisplayGamesCount
        {
            get => _displayGamesCount;
            set
            {
                if (_displayGamesCount != value)
                {
                    _displayGamesCount = value;
                    Raise(nameof(DisplayGamesCount));
                }
            }
        }

        public int HomeGamesCount
        {
            get => _homeGamesCount;
            set
            {
                if (_homeGamesCount != value)
                {
                    _homeGamesCount = value;
                    Raise(nameof(HomeGamesCount));
                }
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    Raise(nameof(IsExpanded));
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
                    Sport.GameDisplayMode = value;
                    Raise(nameof(GameDisplayMode));
                }
            }
        }

        public void UpdateFrom(Sport sport, Data data, List<Contest> games)
        {
            Sport = sport;
            Games = games;
            ConfGamesCount = data.conferenceGames?.Count ?? 0;
            NonConfGamesCount = data.nonConferenceGames?.Count ?? 0;
            DisplayGamesCount = data.displayGames?.Count ?? 0;
            HomeGamesCount = data.homeGames?.Count ?? 0;
            GameDisplayMode = sport.GameDisplayMode;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Raise(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private void TimerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
        {
            if (int.TryParse(comboBox.SelectedItem.ToString(), out int timerValue))
            {
                Settings.SettingsList!.Timer = timerValue;
                aTimer!.Interval = timerValue * 1000;
                AutoSaveSettings();
            }
        }
    }

    private void TimerComboBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            if (int.TryParse(comboBox.Text, out int timerValue))
            {
                Settings.SettingsList!.Timer = timerValue;
                aTimer!.Interval = timerValue * 1000;
                AutoSaveSettings();
            }
        }
    }

    private void PersistHomeTeam(string? selectedValue, string? text)
    {
        if (Settings.SettingsList == null)
            return;

        var options = _teamOptions.Count > 0 ? _teamOptions : TeamSelection.CreateOptions(NameConverters.GetTeams());
        var code = TeamSelection.ResolveName6Char(selectedValue, text, options);
        if (string.IsNullOrEmpty(code) || Settings.SettingsList.HomeTeam == code)
            return;

        Settings.SettingsList.HomeTeam = code;
        AutoSaveSettings();
    }

    private void HomeTeamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
            PersistHomeTeam(comboBox.SelectedValue as string, comboBox.Text);
    }

    private void HomeTeamComboBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
            PersistHomeTeam(comboBox.SelectedValue as string, comboBox.Text);
    }

    private void SportsSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterSports(SportsSearchTextBox.Text);
    }

    private void FilterSports(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            SportsDataGrid.ItemsSource = _originalSports;
        }
        else
        {
            var filteredSports = _originalSports.Where(s =>
                (s.SportName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.SportShortName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.ConferenceName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
            SportsDataGrid.ItemsSource = filteredSports;
        }
    }

    private void AutoSaveSettings()
    {
        try
        {
            Settings.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddTeamButton_Click(object sender, RoutedEventArgs e)
    {
        if (AddTeamComboBox.SelectedValue != null)
        {
            string selectedTeam = AddTeamComboBox.SelectedValue.ToString()!;
            if (!string.IsNullOrEmpty(selectedTeam))
            {
                var displayTeams = Settings.GetDisplayTeams()!;
                if (!displayTeams.Any(dt => dt.NcaaTeamName == selectedTeam))
                {
                    displayTeams.Add(new DisplayTeam { NcaaTeamName = selectedTeam });
                    DisplayTeamsDataGrid.ItemsSource = null;
                    DisplayTeamsDataGrid.ItemsSource = displayTeams;
                    AutoSaveSettings();
                }
            }
        }
    }

    private void AddSportButton_Click(object sender, RoutedEventArgs e)
    {
        var newSport = new Sport
        {
            SportName = "New Sport",
            SportShortName = "NS",
            Enabled = true,
            Division = 1,
            Week = 1
        };

        Settings.SettingsList!.Sports!.Add(newSport);
        _originalSports.Add(newSport);

        newSport.PropertyChanged += Sport_PropertyChanged;
        newSport.OosUpdater.PropertyChanged += OosUpdater_PropertyChanged;
        newSport.ListsNeeded.PropertyChanged += ListsNeeded_PropertyChanged;

        SportsDataGrid.Items.Refresh();
        AutoSaveSettings();
    }

    private void RemoveTeamButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DisplayTeam teamToRemove)
        {
            var displayTeams = Settings.GetDisplayTeams()!;
            displayTeams.Remove(teamToRemove);
            DisplayTeamsDataGrid.ItemsSource = null;
            DisplayTeamsDataGrid.ItemsSource = displayTeams;
            AutoSaveSettings();
        }
    }

    private void RemoveSportButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Sport sportToRemove)
        {
            var result = MessageBox.Show($"Are you sure you want to remove the sport '{sportToRemove.SportName}'?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var sportInList = Settings.SettingsList!.Sports!.FirstOrDefault(s =>
                    s.SportName == sportToRemove.SportName);

                try
                {
                    if (sportInList != null)
                    {
                        Settings.SettingsList.Sports.Remove(sportInList);
                        _originalSports.RemoveAll(s => s.SportName == sportToRemove.SportName);
                        SportsDataGrid.Items.Refresh();
                        AutoSaveSettings();
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
    }



    private bool HasAnyOosEnabled()
    {
        var sports = Settings.GetSports();
        return sports?.Any(s => s.OosUpdater?.Enabled == true) ?? false;
    }

    private void SetOosColumnsVisibility(bool visible)
    {
        foreach (var column in SportsDataGrid.Columns)
        {
            if (column.Header.ToString() == "OOS Path" ||
                column.Header.ToString() == "OOS File" ||
                column.Header.ToString() == "OOS Scores" ||
                column.Header.ToString() == "OOS Teams")
            {
                column.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private void LoadSettingsUI()
    {
        if (_settingsUiLoaded)
            return;

        TimerComboBox.Text = Settings.SettingsList!.Timer.ToString();

        _teamOptions = TeamSelection.CreateOptions(NameConverters.GetTeams());
        HomeTeamComboBox.ItemsSource = _teamOptions;
        HomeTeamComboBox.DisplayMemberPath = nameof(TeamOption.Display);
        HomeTeamComboBox.SelectedValuePath = nameof(TeamOption.Value);

        var currentTeam = _teamOptions.FirstOrDefault(t => t.Value == Settings.homeTeam);
        if (currentTeam != null)
        {
            HomeTeamComboBox.SelectedItem = currentTeam;
        }

        var sports = Settings.GetSports();
        SportsDataGrid.ItemsSource = sports;
        _originalSports = new List<Sport>(sports!);

        foreach (var sport in sports!)
        {
            sport.PropertyChanged += Sport_PropertyChanged;
            sport.OosUpdater.PropertyChanged += OosUpdater_PropertyChanged;
            sport.ListsNeeded.PropertyChanged += ListsNeeded_PropertyChanged;
        }

        var conferences = NameConverters.GetConferences();
        var conferenceNames = conferences.Select(c => c.customConferenceName).ToList();
        ConferenceNames = conferenceNames;

        bool hasOosEnabled = HasAnyOosEnabled();
        SetOosColumnsVisibility(hasOosEnabled);

        DisplayTeamsDataGrid.ItemsSource = Settings.GetDisplayTeams();

        _originalAddTeamOptions = _teamOptions;
        AddTeamComboBox.ItemsSource = _teamOptions;
        AddTeamComboBox.DisplayMemberPath = nameof(TeamOption.Display);
        AddTeamComboBox.SelectedValuePath = nameof(TeamOption.Value);

        XmlToJsonEnabledCheckBox.IsChecked = Settings.XmlToJson!.Enabled;
        XmlToJsonPathsItemsControl.ItemsSource = Settings.XmlToJson.FilePaths;

        _settingsUiLoaded = true;
    }

    private void LoadConvertersUI()
    {
        if (_convertersUiLoaded)
            return;

        var teams = NameConverters.GetTeams();
        TeamsDataGrid.ItemsSource = teams;

        var conferences = NameConverters.GetConferences();
        ConferencesDataGrid.ItemsSource = conferences;

        _originalTeams = new List<Team>(teams);
        _originalConferences = new List<Conferences>(conferences);

        _convertersUiLoaded = true;
    }


    private void TeamsSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterTeams(TeamsSearchTextBox.Text);
    }

    private void ConferencesSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterConferences(ConferencesSearchTextBox.Text);
    }

    private void FilterTeams(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            TeamsDataGrid.ItemsSource = _originalTeams;
        }
        else
        {
            var filteredTeams = _originalTeams.Where(t =>
                (t.name6Char?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.customName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.seoname?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.nameShort?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
            TeamsDataGrid.ItemsSource = filteredTeams;
        }
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl && tabControl.Name == "MainTabControl")
        {
            var selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab?.Header?.ToString() == "Name Converters")
            {
                if (TeamsDataGrid.ItemsSource == null || ConferencesDataGrid.ItemsSource == null)
                    LoadConvertersUI();
            }
            else if (selectedTab?.Header?.ToString() == "Settings")
            {
                if (!_configLoaded)
                    return;

                if (SportsDataGrid.ItemsSource == null)
                    LoadSettingsUI();
            }
        }
    }

    private void AutoSaveConverters()
    {
        try
        {
            if (NameConverters.NameList != null)
            {
                NameConverters.NameList.teams = _originalTeams.ToList();
                NameConverters.NameList.conferences = _originalConferences.ToList();
            }

            NameConverters.Reload();
        }
        catch (Exception)
        {
        }
    }

    private void FilterConferences(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            ConferencesDataGrid.ItemsSource = _originalConferences;
        }
        else
        {
            var filteredConferences = _originalConferences.Where(c =>
                (c.customConferenceName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.conferenceSeo?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
            ConferencesDataGrid.ItemsSource = filteredConferences;
        }
    }

    private void AddTeamComboBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterAddTeamOptions(AddTeamComboBox.Text);
    }

    private void FilterAddTeamOptions(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            AddTeamComboBox.ItemsSource = _originalAddTeamOptions;
        }
        else
        {
            var filteredOptions = _originalAddTeamOptions.Where(o =>
                (o.Display?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (o.Value?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (o.NameShort?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
            AddTeamComboBox.ItemsSource = filteredOptions;
        }
    }




    private void TeamsDataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel) return;

        var team = e.Row.Item as Team;
        if (team == null) return;

        if (e.Column.Header.ToString() == "Display Name")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                string newDisplayName = textBox.Text.Trim();
                if (string.IsNullOrEmpty(newDisplayName))
                {
                    MessageBox.Show("Display name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Cancel = true;
                    return;
                }

                team.customName = newDisplayName;

                AutoSaveConverters();
            }
        }
        else if (e.Column.Header.ToString() == "Custom Name")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                team.customName = textBox.Text.Trim();

                AutoSaveConverters();
            }
        }
    }

    private void ConferencesDataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel) return;

        var conference = e.Row.Item as Conferences;
        if (conference == null) return;

        if (e.Column.Header.ToString() == "Custom Name")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                string newCustomName = textBox.Text.Trim();
                if (string.IsNullOrEmpty(newCustomName))
                {
                    MessageBox.Show("Custom name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Cancel = true;
                    return;
                }

                conference.customConferenceName = newCustomName;

                AutoSaveConverters();
            }
        }
    }

    private void Sport_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        AutoSaveSettings();
        if (e.PropertyName == "GameDisplayMode")
        {
            UpdateSportsTabs();
        }
    }

    private void OosUpdater_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        AutoSaveSettings();
        if (e.PropertyName == "Enabled")
        {
            bool hasOosEnabled = HasAnyOosEnabled();
            SetOosColumnsVisibility(hasOosEnabled);
        }
    }

    private void ListsNeeded_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        AutoSaveSettings();
    }

    private void XmlToJsonEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        Settings.XmlToJson!.Enabled = true;
        AutoSaveSettings();
    }

    private void XmlToJsonEnabledCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        Settings.XmlToJson!.Enabled = false;
        AutoSaveSettings();
    }

    private void XmlToJsonPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is FilePath filePath)
        {
            filePath.Path = textBox.Text;
            AutoSaveSettings();
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_configLoaded)
            StartProcess();
    }


    protected override void OnClosed(EventArgs e)
    {
        aTimer?.Stop();
        aTimer?.Dispose();
        _conversionGate.Dispose();
        base.OnClosed(e);
    }
}
