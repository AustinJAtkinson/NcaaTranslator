using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Threading.Tasks;
using NcaaTranslator.Library;

namespace NcaaTranslator.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.ApplySystemTheme();

        _ = CheckForUpdatesOnStartupAsync();
    }

    private async Task CheckForUpdatesOnStartupAsync()
    {
        GitHubRelease? release;
        try
        {
            release = await Task.Run(UpdateManager.GetAvailableUpdateAsync);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
            return;
        }

        if (release?.tag_name == null)
            return;

        var versionText = release.tag_name.TrimStart('v');
        var accepted = await Dispatcher.InvokeAsync(() =>
            MessageBox.Show(
                $"Update to v{versionText}?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes);

        if (!accepted)
            return;

        try
        {
            var newExePath = await UpdateManager.DownloadAndInstallUpdateAsync(release);
            if (!string.IsNullOrEmpty(newExePath))
            {
                Process.Start(newExePath);
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update install failed: {ex.Message}");
        }
    }
}
