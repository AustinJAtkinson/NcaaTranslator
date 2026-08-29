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
        WindowBounds.BaseDirectory = appDir;

        // PhotinoServer resolves "wwwroot" against CWD. Pin to the exe dir so
        // `dotnet run` serves output wwwroot, not the project-tree copy.
        Directory.SetCurrentDirectory(appDir);

        PhotinoServer
            .CreateStaticFileServer(args, out var baseUrl)
            .RunAsync();

        var bounds = WindowBounds.Load();
        var window = new PhotinoWindow()
            .SetTitle("NCAA Translator")
            .SetUseOsDefaultSize(false)
            .SetMinSize(WindowBounds.MinWidth, WindowBounds.MinHeight)
            // Restore the normal size even when starting maximized; Photino
            // otherwise un-maximizes into a tiny default.
            .SetSize(bounds.Width, bounds.Height);

        if (bounds.Left is int left && bounds.Top is int top)
        {
            window.SetUseOsDefaultLocation(false)
                .SetLeft(left)
                .SetTop(top);
        }
        else
        {
            window.Center();
        }

        if (bounds.Maximized)
            window.SetMaximized(true);

        window
            .RegisterWebMessageReceivedHandler((sender, message) =>
            {
                var photino = (PhotinoWindow)sender!;
                var response = Bridge.Handle(photino, message);
                if (response != null)
                    photino.SendWebMessage(response);
            })
            .Load($"{baseUrl}/index.html");

        RegisterWindowBoundsHandlers(window, bounds);

        // Do not block Main; prompt only after the native window exists.
        window.RegisterWindowCreatedHandler((_, _) =>
        {
            RepositionIfOffScreen(window, bounds);
            _ = CheckForUpdatesAsync(window);
        });

        window.WaitForClose();
    }

    private static void RegisterWindowBoundsHandlers(PhotinoWindow window, WindowBounds bounds)
    {
        var gate = new object();
        System.Threading.Timer? debounce = null;
        var maximized = bounds.Maximized;

        void ScheduleSave()
        {
            lock (gate)
            {
                debounce?.Dispose();
                debounce = new System.Threading.Timer(_ =>
                {
                    try
                    {
                        bounds.Save();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Window bounds save failed: {ex.Message}");
                    }
                }, null, 250, Timeout.Infinite);
            }
        }

        window.RegisterMaximizedHandler((_, _) =>
        {
            maximized = true;
            bounds.ApplyMaximized();
            ScheduleSave();
        });

        window.RegisterRestoredHandler((_, _) =>
        {
            // Photino also fires Restored when un-minimizing a maximized window.
            if (window.Maximized)
            {
                maximized = true;
                bounds.ApplyMaximized();
                ScheduleSave();
                return;
            }

            maximized = false;
            bounds.ApplyRestored(window.Width, window.Height, window.Left, window.Top);
            ScheduleSave();
        });

        window.RegisterMinimizedHandler((_, _) =>
        {
            bounds.ApplyMinimized();
        });

        window.RegisterSizeChangedHandler((_, _) =>
        {
            if (window.Minimized)
                return;

            bounds.ApplySizeChange(
                window.Width,
                window.Height,
                window.Left,
                window.Top,
                maximized || window.Maximized);
            ScheduleSave();
        });

        window.RegisterLocationChangedHandler((_, _) =>
        {
            if (window.Minimized || maximized || window.Maximized)
                return;

            bounds.ApplySizeChange(
                window.Width,
                window.Height,
                window.Left,
                window.Top,
                maximized: false);
            ScheduleSave();
        });

        window.RegisterWindowClosingHandler((_, _) =>
        {
            lock (gate)
            {
                debounce?.Dispose();
                debounce = null;
            }

            try
            {
                bounds.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Window bounds save failed: {ex.Message}");
            }

            return false;
        });
    }

    private static void RepositionIfOffScreen(PhotinoWindow window, WindowBounds bounds)
    {
        List<DisplayRect> displays;
        try
        {
            displays = window.Monitors
                .Select(monitor =>
                {
                    var area = monitor.WorkArea;
                    return new DisplayRect(area.X, area.Y, area.Width, area.Height);
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Monitor list failed: {ex.Message}");
            return;
        }

        var left = bounds.Left;
        var top = bounds.Top;
        bounds.Normalize(displays);
        if (bounds.Left is not null || left is null || top is null)
            return;

        window.Center();
        if (!bounds.Maximized)
            window.SetSize(bounds.Width, bounds.Height);
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
