using System.Diagnostics;
using NcaaTranslator.Library;
using Photino.NET;
using Photino.NET.Server;

namespace NcaaTranslator.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            throw;
        }
    }

    static void Run(string[] args)
    {
        var appDir = AppContext.BaseDirectory;
        Settings.BaseDirectory = appDir;
        NameConverters.BaseDirectory = appDir;

        // PhotinoServer resolves "wwwroot" against CWD. Pin to the exe dir so
        // `dotnet run` serves output wwwroot, not the project-tree copy.
        Directory.SetCurrentDirectory(appDir);

        PhotinoServer
            .CreateStaticFileServer(args, out var baseUrl)
            .RunAsync();

        var window = new PhotinoWindow()
            .SetTitle("NCAA Translator")
            .SetUseOsDefaultSize(false)
            .SetSize(1000, 600)
            .Center()
            .RegisterWebMessageReceivedHandler((sender, message) =>
            {
                var photino = (PhotinoWindow)sender!;
                var response = Bridge.Handle(photino, message);
                if (response != null)
                    photino.SendWebMessage(response);
            })
            .Load($"{baseUrl}/index.html");

        // Do not block Main; prompt only after the native window exists.
        window.RegisterWindowCreatedHandler((_, _) => _ = CheckForUpdatesAsync(window));

        window.WaitForClose();
    }

    private static async Task CheckForUpdatesAsync(PhotinoWindow window)
    {
        GitHubRelease? release;
        try
        {
            release = await UpdateManager.GetAvailableUpdateAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
            return;
        }

        if (release?.tag_name == null)
            return;

        var versionText = release.tag_name.TrimStart('v');
        PhotinoDialogResult result;
        try
        {
            result = window.ShowMessage(
                "Update Available",
                $"Update to v{versionText}?",
                PhotinoDialogButtons.YesNo,
                PhotinoDialogIcon.Question);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update prompt failed: {ex.Message}");
            return;
        }

        if (result != PhotinoDialogResult.Yes)
            return;

        try
        {
            var newExePath = await UpdateManager.DownloadAndInstallUpdateAsync(release);
            if (string.IsNullOrEmpty(newExePath))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = newExePath,
                UseShellExecute = true
            });
            window.Close();
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update install failed: {ex.Message}");
        }
    }
}
