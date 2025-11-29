using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;
using NcaaTranslator.Library;

namespace NcaaTranslator.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.ApplySystemTheme();

        // Check for updates in background
        _ = Task.Run(() => UpdateManager.CheckForUpdatesAsync());
    }
}

