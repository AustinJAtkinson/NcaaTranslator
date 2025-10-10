using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using NcaaTranslator.Library;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
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
    private List<object> _originalAddTeamOptions = new List<object>();
    private Dictionary<string, NcaaScoreboard> _sportScoreboards = new Dictionary<string, NcaaScoreboard>();
    private ObservableCollection<SportGamesViewModel> _sportTabs = new ObservableCollection<SportGamesViewModel>();

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

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeTimer();
        LoadInitialData();
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
        }
        catch (Exception ex)
        {
            // Error loading data - silently handle
        }
    }

    private async void StartProcess()
    {
        // Run the conversion once immediately
        await PerformConversion(DateTime.Now);

        // Then start the timer for periodic runs
        aTimer!.Enabled = true;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        StatusText.Text = "Status: Running";
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
        await PerformConversion(e.SignalTime);
    }

    private async Task PerformConversion(DateTime signalTime)
    {
        Dispatcher.Invoke(() =>
        {
            LastUpdateText.Text = $"Last Update: {signalTime:HH:mm:ss.fff}";
        });

        var sportsList = Settings.GetSports()!;

        foreach (var sport in sportsList)
        {
            try
            {
                var result = await NcaaProcessor.ConvertNcaaScoreboard(sport);
                Dispatcher.Invoke(() =>
                {
                    _sportScoreboards[sport.SportName!] = result;
                    UpdateSportsTabs();
                    // Sport processing completed
                });
            }
            catch (Exception err)
            {
                // Error processing sport - silently handle
            }
        }

        if (Settings.XmlToJson!.Enabled)
        {
            try
            {
                NcaaProcessor.ConvertXmlToJson(Settings.XmlToJson!);
                // XML to JSON conversion completed
            }
            catch (Exception ex)
            {
                // XML conversion error - silently handle
            }
        }
    }


    private void UpdateSportsTabs()
    {
        Dispatcher.Invoke(() =>
        {
            var newSportTabs = new ObservableCollection<SportGamesViewModel>();
            var enabledSports = Settings.GetSports()?.Where(s => s.Enabled) ?? new List<Sport>();

            foreach (var sport in enabledSports)
            {
                if (_sportScoreboards.TryGetValue(sport.SportName!, out var scoreboard) && scoreboard.data != null)
                {
                    // Show all games for debugging first
                    var allGames = scoreboard.data.conferenceGames.ToList();
                    allGames.AddRange(scoreboard.data.nonConferenceGames);
                    allGames.AddRange(scoreboard.data.homeGames);

                    // Filter based on GameDisplayMode setting
                    List<Contest> gamesToShow;
                    switch (sport.GameDisplayMode)
                    {
                        case GameDisplayMode.All:
                            gamesToShow = allGames;
                            break;
                        case GameDisplayMode.Display:
                            gamesToShow = scoreboard.data.displayGames ?? new List<Contest>();
                            break;
                        case GameDisplayMode.Live:
                        default:
                            gamesToShow = allGames.Where(c => c.gameState == "I").ToList();
                            break;
                    }

                    newSportTabs.Add(new SportGamesViewModel
                    {
                        SportName = sport.SportName!,
                        Games = gamesToShow,
                        ConfGamesCount = scoreboard.data.conferenceGames.Count,
                        NonConfGamesCount = scoreboard.data.nonConferenceGames.Count,
                        HomeGamesCount = scoreboard.data.homeGames.Count,
                        GameDisplayMode = sport.GameDisplayMode,
                        Sport = sport
                    });
                }
            }

            SportTabs = newSportTabs;
        });
    }

    public class SportGamesViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded = true;
        private GameDisplayMode _gameDisplayMode = GameDisplayMode.Live;

        public string SportName { get; set; } = "";
        public List<Contest> Games { get; set; } = new List<Contest>();
        public int ConfGamesCount { get; set; }
        public int NonConfGamesCount { get; set; }
        public int HomeGamesCount { get; set; }
        public Sport Sport { get; set; } = new Sport();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
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
                    // AutoSaveSettings and UpdateSportsTabs are called via Sport.PropertyChanged
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameDisplayMode)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private void TimerTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(((TextBox)sender).Text, out int timerValue))
        {
            Settings.SettingsList!.Timer = timerValue;
            aTimer!.Interval = timerValue * 1000;
            AutoSaveSettings();
        }
    }

    private void HomeTeamTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Settings.SettingsList!.HomeTeam = ((TextBox)sender).Text;
        AutoSaveSettings();
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

    private void HomeTeamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedValue != null)
        {
            Settings.SettingsList!.HomeTeam = comboBox.SelectedValue.ToString();
            AutoSaveSettings();
        }
    }

    private void HomeTeamComboBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            Settings.SettingsList!.HomeTeam = comboBox.Text;
            AutoSaveSettings();
        }
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
                (s.ConferenceName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
            SportsDataGrid.ItemsSource = filteredSports;
        }
    }

    private void SportsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel) return;

        var sport = e.Row.Item as Sport;
        if (sport == null) return;

        // Validate and update based on column
        if (e.Column.Header.ToString() == "Sport Name")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                string newName = textBox.Text.Trim();
                if (string.IsNullOrEmpty(newName))
                {
                    e.Cancel = true;
                    return;
                }
                sport.SportName = newName;
            }
        }
        else if (e.Column.Header.ToString() == "Conference")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                sport.ConferenceName = textBox.Text.Trim();
            }
        }
        else if (e.Column.Header.ToString() == "Sport Code")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                sport.SportCode = textBox.Text.Trim();
            }
        }
        else if (e.Column.Header.ToString() == "Division")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null && int.TryParse(textBox.Text, out int division))
            {
                sport.Division = division;
            }
        }
        else if (e.Column.Header.ToString() == "Week")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null && int.TryParse(textBox.Text, out int week))
            {
                sport.Week = week;
            }
        }
        else if (e.Column.Header.ToString() == "OOS Path")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                sport.OosUpdater.OosFilePath = textBox.Text.Trim();
            }
        }
        else if (e.Column.Header.ToString() == "OOS File")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                sport.OosUpdater.OosFileName = textBox.Text.Trim();
            }
        }
        else if (e.Column.Header.ToString() == "OOS Scores")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null && int.TryParse(textBox.Text, out int value))
            {
                sport.OosUpdater.NumberOfOutScores = value;
            }
        }
        else if (e.Column.Header.ToString() == "OOS Teams")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null && int.TryParse(textBox.Text, out int value))
            {
                sport.OosUpdater.NumberOfTeamsPer = value;
            }
        }

        // Auto-save the changes
        AutoSaveSettings();

        // Refresh OOS column visibility if OOS settings were changed
        if (e.Column.Header.ToString() == "OOS Enabled")
        {
            bool hasOosEnabled = HasAnyOosEnabled();
            SetOosColumnsVisibility(hasOosEnabled);
        }
    }


    private void AutoSaveSettings()
    {
        try
        {
            Settings.Save();
            // No status message needed for auto-save
        }
        catch (Exception ex)
        {
            // Error saving settings - silently handle
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
            Enabled = true,
            Division = 1,
            Week = 1
        };

        Settings.SettingsList!.Sports!.Add(newSport);
        _originalSports.Add(newSport);

        // Subscribe to PropertyChanged events
        newSport.PropertyChanged += Sport_PropertyChanged;
        newSport.OosUpdater.PropertyChanged += OosUpdater_PropertyChanged;
        newSport.ListsNeeded.PropertyChanged += ListsNeeded_PropertyChanged;

        // Refresh the DataGrid
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
                // Find the sport in the settings list by matching properties since the Tag object might not be the same reference
                var sportInList = Settings.SettingsList!.Sports!.FirstOrDefault(s =>
                    s.SportName == sportToRemove.SportName);

                try
                {
                    if (sportInList != null)
                    {
                        Settings.SettingsList.Sports.Remove(sportInList);
                        // Update the original sports list for search functionality
                        _originalSports.RemoveAll(s => s.SportName == sportToRemove.SportName);
                        // Refresh the DataGrid
                        SportsDataGrid.Items.Refresh();
                        AutoSaveSettings();
                    }
                }
                catch (Exception ex)
                {
                    // Error finding sport - silently handle
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
        // Find OOS columns by header name and set visibility
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
        // General settings
        GeneralSettingsPanel.Children.Clear();

        // Timer setting with cool preset options
        var timerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var timerLabel = new TextBlock { Text = "Timer (seconds): ", Width = 120, VerticalAlignment = VerticalAlignment.Center };
        timerLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        timerPanel.Children.Add(timerLabel);
        var timerComboBox = new ComboBox { Text = (Settings.SettingsList!.Timer).ToString(), Width = 100, IsEditable = true, Name = "TimerComboBox" };
        timerComboBox.Style = (Style)FindResource("ModernComboBoxStyle");
        timerComboBox.ItemsSource = new List<int> { 5, 10, 15, 20, 30, 60, 120, 300 };
        timerComboBox.SelectionChanged += TimerComboBox_SelectionChanged;
        timerComboBox.LostFocus += TimerComboBox_LostFocus;
        timerPanel.Children.Add(timerComboBox);
        GeneralSettingsPanel.Children.Add(timerPanel);

        // Home team setting as dropdown
        var homeTeamPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var homeTeamLabel = new TextBlock { Text = "Home Team: ", Width = 120, VerticalAlignment = VerticalAlignment.Center };
        homeTeamLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        homeTeamPanel.Children.Add(homeTeamLabel);
        var homeTeamComboBox = new ComboBox { Text = Settings.homeTeam, Width = 200, IsEditable = true, Name = "HomeTeamComboBox" };
        homeTeamComboBox.Style = (Style)FindResource("ModernComboBoxStyle");

        // Populate with team options from NameConverters
        if (NameConverters.NameList == null)
        {
            NameConverters.Load();
        }
        var teams = NameConverters.GetTeams();
        var teamOptions = teams.Where(t => !string.IsNullOrEmpty(t.name6Char)).Select(t => new
        {
            Display = t.customName ?? t.nameShort ?? t.name6Char,
            Value = t.name6Char
        }).OrderBy(t => t.Display).ToList();
        homeTeamComboBox.ItemsSource = teamOptions;
        homeTeamComboBox.DisplayMemberPath = "Display";
        homeTeamComboBox.SelectedValuePath = "Value";

        // Set current selection if it exists
        var currentTeam = teamOptions.FirstOrDefault(t => t.Value == Settings.homeTeam);
        if (currentTeam != null)
        {
            homeTeamComboBox.SelectedItem = currentTeam;
        }

        homeTeamComboBox.SelectionChanged += HomeTeamComboBox_SelectionChanged;
        homeTeamComboBox.LostFocus += HomeTeamComboBox_LostFocus;
        homeTeamPanel.Children.Add(homeTeamComboBox);
        GeneralSettingsPanel.Children.Add(homeTeamPanel);


        // Sports
        var sports = Settings.GetSports();
        SportsDataGrid.ItemsSource = sports;
        _originalSports = new List<Sport>(sports!);

        // Subscribe to PropertyChanged events for auto-save
        foreach (var sport in sports!)
        {
            sport.PropertyChanged += Sport_PropertyChanged;
            sport.OosUpdater.PropertyChanged += OosUpdater_PropertyChanged;
            sport.ListsNeeded.PropertyChanged += ListsNeeded_PropertyChanged;
        }

        // Set conference dropdown items
        if (NameConverters.NameList == null)
        {
            NameConverters.Load();
        }
        var conferences = NameConverters.GetConferences();
        var conferenceNames = conferences.Select(c => c.customConferenceName).ToList();
        ConferenceColumn.ItemsSource = conferenceNames;

        // Conditionally show/hide OOS columns based on whether any sport has OOS enabled
        bool hasOosEnabled = HasAnyOosEnabled();
        SetOosColumnsVisibility(hasOosEnabled);

        // Set up event handlers for DataGrid validation
        SportsDataGrid.CellEditEnding += SportsDataGrid_CellEditEnding;

        // Display Teams
        DisplayTeamsDataGrid.ItemsSource = Settings.GetDisplayTeams();

        // Populate Add Team ComboBox with teams from NameConverters
        if (NameConverters.NameList == null)
        {
            NameConverters.Load();
        }
        var addTeams = NameConverters.GetTeams();
        var addTeamOptions = addTeams.Where(t => !string.IsNullOrEmpty(t.name6Char)).Select(t => new
        {
            Display = t.customName ?? t.nameShort ?? t.name6Char,
            Value = t.nameShort ?? t.name6Char
        }).OrderBy(t => t.Display).ToList();
        _originalAddTeamOptions = addTeamOptions.Cast<object>().ToList();
        AddTeamComboBox.ItemsSource = addTeamOptions;
        AddTeamComboBox.DisplayMemberPath = "Display";
        AddTeamComboBox.SelectedValuePath = "Value";
        AddTeamComboBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new System.Windows.Controls.TextChangedEventHandler(AddTeamComboBox_TextChanged));

        // XML to JSON
        XmlToJsonPanel.Children.Clear();
        var xmlLabel = new TextBlock { Text = "XML to JSON:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) };
        xmlLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        XmlToJsonPanel.Children.Add(xmlLabel);

        var enabledCheckBox = new CheckBox { IsChecked = Settings.XmlToJson!.Enabled, Content = "Enabled", Margin = new Thickness(0, 0, 0, 10) };
        enabledCheckBox.Style = (Style)FindResource("ModernCheckBoxStyle");
        enabledCheckBox.SetResourceReference(CheckBox.ForegroundProperty, "TextPrimaryBrush");
        enabledCheckBox.Checked += (s, e) => { Settings.XmlToJson.Enabled = true; AutoSaveSettings(); };
        enabledCheckBox.Unchecked += (s, e) => { Settings.XmlToJson.Enabled = false; AutoSaveSettings(); };
        XmlToJsonPanel.Children.Add(enabledCheckBox);

        var filePathsLabel = new TextBlock { Text = "File Paths:", Margin = new Thickness(0, 0, 0, 5) };
        filePathsLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        XmlToJsonPanel.Children.Add(filePathsLabel);

        foreach (var path in Settings.XmlToJson.FilePaths!)
        {
            var pathPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20, 0, 0, 5) };
            var pathLabel = new TextBlock { Text = "Path: ", Width = 50, VerticalAlignment = VerticalAlignment.Center };
            pathLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
            pathPanel.Children.Add(pathLabel);
            var pathTextBox = new TextBox { Text = path.Path, Width = 300 };
            pathTextBox.Style = (Style)FindResource("ModernTextBoxStyle");
            pathTextBox.TextChanged += (s, e) => { path.Path = pathTextBox.Text; AutoSaveSettings(); };
            pathPanel.Children.Add(pathTextBox);
            XmlToJsonPanel.Children.Add(pathPanel);
        }
    }

    private void LoadConvertersUI()
    {
        // Load teams into DataGrid
        var teams = NameConverters.GetTeams();
        TeamsDataGrid.ItemsSource = teams;

        // Load conferences into DataGrid
        var conferences = NameConverters.GetConferences();
        ConferencesDataGrid.ItemsSource = conferences;

        // Store original collections for filtering
        _originalTeams = new List<Team>(teams);
        _originalConferences = new List<Conferences>(conferences);

        // Set up event handlers for DataGrid validation
        TeamsDataGrid.CellEditEnding += TeamsDataGrid_CellEditEnding;
        ConferencesDataGrid.CellEditEnding += ConferencesDataGrid_CellEditEnding;
    }


    // ===== NEW NAME CONVERTERS EVENT HANDLERS =====

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
                // Ensure data is loaded when the Name Converters tab is selected
                if (NameConverters.NameList == null || NameConverters.NameList.teams.Count == 0 || NameConverters.NameList.conferences.Count == 0)
                {
                    try
                    {
                        NameConverters.Load();
                        LoadConvertersUI();
                    }
                    catch (Exception ex)
                    {
                        // Could show error message if needed, but for now just load what we can
                        LoadConvertersUI();
                    }
                }
                else if (TeamsDataGrid.ItemsSource == null || ConferencesDataGrid.ItemsSource == null)
                {
                    // Data is loaded but UI isn't populated yet
                    LoadConvertersUI();
                }
            }
            else if (selectedTab?.Header?.ToString() == "Settings")
            {
                // Ensure settings UI is loaded when the Settings tab is selected
                if (GeneralSettingsPanel.Children.Count == 0 || SportsDataGrid.ItemsSource == null)
                {
                    try
                    {
                        Settings.Load();
                        LoadSettingsUI();
                    }
                    catch (Exception ex)
                    {
                        // Error loading settings - silently handle
                    }
                }
            }
        }
    }

    private void AutoSaveConverters()
    {
        try
        {
            // Update the NameList with current data
            if (NameConverters.NameList != null)
            {
                NameConverters.NameList.teams = _originalTeams.ToList();
                NameConverters.NameList.conferences = _originalConferences.ToList();
            }

            NameConverters.Reload();
            // Changes are saved automatically - no status message needed
        }
        catch (Exception ex)
        {
            // Could show error message if needed, but for now just continue
            // The user will see if something goes wrong through other means
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
            {
                var display = o.GetType().GetProperty("Display")?.GetValue(o, null) as string;
                var value = o.GetType().GetProperty("Value")?.GetValue(o, null) as string;
                return (display?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (value?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
            }).ToList();
            AddTeamComboBox.ItemsSource = filteredOptions;
        }
    }




    private void TeamsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel) return;

        var team = e.Row.Item as Team;
        if (team == null) return;

        // Validate display name
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

                // Auto-save the changes
                AutoSaveConverters();
            }
        }
        else if (e.Column.Header.ToString() == "Custom Name")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                team.customName = textBox.Text.Trim();

                // Auto-save the changes
                AutoSaveConverters();
            }
        }
    }

    private void ConferencesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel) return;

        var conference = e.Row.Item as Conferences;
        if (conference == null) return;

        // Validate custom name
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

                // Auto-save the changes
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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        StartProcess();
    }


    protected override void OnClosed(EventArgs e)
    {
        aTimer?.Stop();
        aTimer?.Dispose();
        base.OnClosed(e);
    }
}