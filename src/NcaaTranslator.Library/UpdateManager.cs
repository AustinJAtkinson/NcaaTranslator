using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace NcaaTranslator.Library
{
    public class GitHubRelease
    {
        public string? tag_name { get; set; }
        public List<GitHubAsset>? assets { get; set; }
    }

    public class GitHubAsset
    {
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
    }

    public static class UpdateManager
    {
        private const string GitHubRepo = "AustinJAtkinson/NcaaTranslator";
        private const string ApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
        private static readonly HttpClient _httpClient = new HttpClient();

        static UpdateManager()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NcaaTranslator", GetCurrentVersion().ToString()));
        }

        public static async Task CheckForUpdatesAsync()
        {
#if DEBUG
            // Skip updates in debug mode
            return;
#endif

            try
            {
                var currentVersion = GetCurrentVersion();
                var latestRelease = await GetLatestReleaseAsync();

                if (latestRelease?.tag_name != null && Version.TryParse(latestRelease.tag_name.TrimStart('v'), out var latestVersion))
                {
                    if (latestVersion > currentVersion)
                    {
                        // Update available
                        await DownloadAndInstallUpdateAsync(latestRelease);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error silently
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        private static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        }

        private static async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            var response = await _httpClient.GetAsync(ApiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GitHubRelease>(json);
        }

        private static async Task DownloadAndInstallUpdateAsync(GitHubRelease release)
        {
            if (release.assets == null || !release.assets.Any())
                return;

            // Assume the first asset is the zip
            var asset = release.assets.FirstOrDefault(a => a.name?.EndsWith(".zip") == true);
            if (asset?.browser_download_url == null)
                return;

            var tempPath = Path.Combine(Path.GetTempPath(), $"NcaaTranslator_Update_{Guid.NewGuid()}.zip");
            var extractPath = Path.Combine(Path.GetTempPath(), $"NcaaTranslator_Update_{Guid.NewGuid()}");

            try
            {
                // Download
                using var response = await _httpClient.GetAsync(asset.browser_download_url);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(tempPath, FileMode.Create);
                await response.Content.CopyToAsync(fs);

                // Extract
                System.IO.Compression.ZipFile.ExtractToDirectory(tempPath, extractPath);

                // Install
                await InstallUpdateAsync(extractPath);

                // Notify user
                // Since WPF, we need to dispatch to UI thread, but for now, assume console or log
                Debug.WriteLine("Update installed. Please restart the application.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update installation failed: {ex.Message}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempPath)) File.Delete(tempPath);
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
            }
        }

        private static async Task InstallUpdateAsync(string extractPath)
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;

            // Copy all files except the exe (can't overwrite running exe)
            foreach (var file in Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(extractPath, file);
                var targetPath = Path.Combine(appDir, relativePath);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                // Skip exe
                if (Path.GetFileName(targetPath).Equals("NcaaTranslator.Wpf.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Copy(file, targetPath, true);
            }

            // For exe, copy to .new
            var exeSource = Directory.GetFiles(extractPath, "NcaaTranslator.Wpf.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (exeSource != null)
            {
                var exeTarget = Path.Combine(appDir, "NcaaTranslator.Wpf.exe.new");
                File.Copy(exeSource, exeTarget, true);
            }
        }

        // Method to apply exe update on startup
        public static void ApplyPendingUpdate()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var newExe = Path.Combine(appDir, "NcaaTranslator.Wpf.exe.new");
            var oldExe = Path.Combine(appDir, "NcaaTranslator.Wpf.exe.old");

            if (File.Exists(newExe))
            {
                // Backup current
                if (File.Exists(Path.Combine(appDir, "NcaaTranslator.Wpf.exe")))
                {
                    File.Move(Path.Combine(appDir, "NcaaTranslator.Wpf.exe"), oldExe, true);
                }

                // Apply new
                File.Move(newExe, Path.Combine(appDir, "NcaaTranslator.Wpf.exe"), true);

                // Delete old on next start or something, but for now leave it
            }
        }
    }
}