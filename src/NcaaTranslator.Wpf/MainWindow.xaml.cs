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

namespace NcaaTranslator.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private System.Timers.Timer? aTimer;
    private DateTime StartTime = DateTime.Now;
    private List<Team> _originalTeams = new List<Team>();
    private List<Conferences> _originalConferences = new List<Conferences>();
    private List<Sport> _originalSports = new List<Sport>();

    public MainWindow()
    {
        InitializeComponent();
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
            AppendOutput($"\nPress the Start button to begin monitoring NCAA scores...\n");
            AppendOutput($"The application started at {StartTime:HH:mm:ss.fff}");
            AppendOutput(String.Format("{0}\t{1}\t{2}\t{3}\t{4}", "Sport".PadRight("Sport".Length + (15 - "Sport".Length)), "Total", "Conf", "NonConf", "Display"));
        }
        catch (Exception ex)
        {
            AppendOutput($"Error loading data: {ex.Message}");
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        // Run the conversion once immediately
        await PerformConversion(DateTime.Now);

        // Then start the timer for periodic runs
        aTimer!.Enabled = true;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        StatusText.Text = "Status: Running";
        AppendOutput("Timer started...");
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        aTimer!.Enabled = false;
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        StatusText.Text = "Status: Stopped";
        AppendOutput("Timer stopped...");
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
            AppendOutput($"The scores were last updated at {signalTime:HH:mm:ss.fff}");
        });

        var sportsList = Settings.GetSports()!;

        foreach (var sport in sportsList)
        {
            try
            {
                var result = await NcaaProcessor.ConvertNcaaScoreboard(sport);
                Dispatcher.Invoke(() =>
                {
                    AppendOutput(String.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                        sport.SportName!.PadRight($"{sport.SportName}:".Length + (15 - $"{sport.SportName}:".Length)),
                        result.games.Count, result.conferenceGames.Count, result.nonConferenceGames.Count, result.displayGames.Count));
                });
            }
            catch (Exception err)
            {
                Dispatcher.Invoke(() =>
                {
                    AppendOutput($"Message :{err.Message} ");
                });
            }
        }

        try
        {
            NcaaProcessor.ConvertXmlToJson(Settings.XmlToJson!);
            Dispatcher.Invoke(() =>
            {
                AppendOutput("XML to JSON conversion completed.");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                AppendOutput($"XML conversion error: {ex.Message}");
            });
        }
    }

    private void AppendOutput(string text)
    {
        OutputTextBox.AppendText(text + "\n");
        OutputTextBox.ScrollToEnd();
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
                (s.SportNameShort?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
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
                    AppendOutput("Sport name cannot be empty.");
                    e.Cancel = true;
                    return;
                }
                sport.SportName = newName;
            }
        }
        else if (e.Column.Header.ToString() == "Short Name")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                sport.SportNameShort = textBox.Text.Trim();
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
        else if (e.Column.Header.ToString() == "NCAA URL")
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                string url = textBox.Text.Trim();
                if (!string.IsNullOrEmpty(url) && !url.EndsWith("/"))
                {
                    url += "/";
                }
                sport.NcaaUrl = url;
                // Update the TextBox to show the corrected value
                textBox.Text = url;
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
            // Log error instead of showing MessageBox
            AppendOutput($"Error saving settings: {ex.Message}");
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
                    s.SportName == sportToRemove.SportName &&
                    s.SportNameShort == sportToRemove.SportNameShort);

                if (sportInList != null)
                {
                    Settings.SettingsList.Sports.Remove(sportInList);
                    // Update the original sports list for search functionality
                    _originalSports.RemoveAll(s => s.SportName == sportToRemove.SportName && s.SportNameShort == sportToRemove.SportNameShort);
                    // Refresh the DataGrid
                    SportsDataGrid.Items.Refresh();
                    AutoSaveSettings();
                }
            }
        }
    }



    private void LoadSettingsUI()
    {
        // General settings
        GeneralSettingsPanel.Children.Clear();

        // Timer setting
        var timerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        timerPanel.Children.Add(new TextBlock { Text = "Timer (seconds): ", Width = 120, VerticalAlignment = VerticalAlignment.Center });
        var timerTextBox = new TextBox { Text = (Settings.SettingsList!.Timer).ToString(), Width = 100, Name = "TimerTextBox" };
        timerTextBox.TextChanged += TimerTextBox_TextChanged;
        timerPanel.Children.Add(timerTextBox);
        GeneralSettingsPanel.Children.Add(timerPanel);

        // Home team setting
        var homeTeamPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        homeTeamPanel.Children.Add(new TextBlock { Text = "Home Team: ", Width = 120, VerticalAlignment = VerticalAlignment.Center });
        var homeTeamTextBox = new TextBox { Text = Settings.homeTeam, Width = 200, Name = "HomeTeamTextBox" };
        homeTeamTextBox.TextChanged += HomeTeamTextBox_TextChanged;
        homeTeamPanel.Children.Add(homeTeamTextBox);
        GeneralSettingsPanel.Children.Add(homeTeamPanel);

        // Sports
        var sports = Settings.GetSports();
        SportsDataGrid.ItemsSource = sports;
        _originalSports = new List<Sport>(sports!);

        // Set conference dropdown items
        if (NameConverters.NameList == null)
        {
            NameConverters.Load();
        }
        var conferences = NameConverters.GetConferences();
        var conferenceNames = conferences.Select(c => c.conferenceName).ToList();
        ConferenceColumn.ItemsSource = conferenceNames;

        // Set up event handlers for DataGrid validation
        SportsDataGrid.CellEditEnding += SportsDataGrid_CellEditEnding;

        // Display Teams
        DisplayTeamsDataGrid.ItemsSource = Settings.GetDisplayTeams();

        // Populate Add Team ComboBox with teams from NameConverters
        if (NameConverters.NameList == null)
        {
            NameConverters.Load();
        }
        var teams = NameConverters.GetTeams();
        var teamOptions = teams.Where(t => !string.IsNullOrEmpty(t.char6)).Select(t => new
        {
            Display = string.IsNullOrEmpty(t.@short) ? (string.IsNullOrEmpty(t.shortOriginal) ? t.char6 : t.shortOriginal) : t.@short,
            Value = t.char6
        }).ToList();
        AddTeamComboBox.ItemsSource = teamOptions;
        AddTeamComboBox.DisplayMemberPath = "Display";
        AddTeamComboBox.SelectedValuePath = "Value";

        // XML to JSON
        XmlToJsonPanel.Children.Clear();
        var xmlLabel = new TextBlock { Text = "XML to JSON:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) };
        XmlToJsonPanel.Children.Add(xmlLabel);

        var enabledCheckBox = new CheckBox { IsChecked = Settings.XmlToJson!.Enabled, Content = "Enabled", Margin = new Thickness(0, 0, 0, 10) };
        enabledCheckBox.Checked += (s, e) => { Settings.XmlToJson.Enabled = true; AutoSaveSettings(); };
        enabledCheckBox.Unchecked += (s, e) => { Settings.XmlToJson.Enabled = false; AutoSaveSettings(); };
        XmlToJsonPanel.Children.Add(enabledCheckBox);

        var filePathsLabel = new TextBlock { Text = "File Paths:", Margin = new Thickness(0, 0, 0, 5) };
        XmlToJsonPanel.Children.Add(filePathsLabel);

        foreach (var path in Settings.XmlToJson.FilePaths!)
        {
            var pathPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20, 0, 0, 5) };
            pathPanel.Children.Add(new TextBlock { Text = "Path: ", Width = 50, VerticalAlignment = VerticalAlignment.Center });
            var pathTextBox = new TextBox { Text = path.Path, Width = 300 };
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
                (t.char6?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.@short?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.shortOriginal?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.seo?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.full?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
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
                        // Log error instead of showing MessageBox during UI initialization
                        AppendOutput($"Error loading settings: {ex.Message}");
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
                (c.conferenceName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.customConferenceName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.conferenceSeo?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
            ConferencesDataGrid.ItemsSource = filteredConferences;
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

                team.@short = newDisplayName;

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

    protected override void OnClosed(EventArgs e)
    {
        aTimer?.Stop();
        aTimer?.Dispose();
        base.OnClosed(e);
    }
}