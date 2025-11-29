using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using NcaaTranslator.Library;

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

        private static NameConverter MergeNameConverters(NameConverter user, NameConverter @new)
        {
            var merged = new NameConverter();

            // Merge teams
            var userTeams = user.teams.ToDictionary(t => t.name6Char, t => t);
            var newTeams = @new.teams.ToDictionary(t => t.name6Char, t => t);

            foreach (var kvp in newTeams)
            {
                if (userTeams.TryGetValue(kvp.Key, out var userTeam))
                {
                    // Use user custom name if different
                    kvp.Value.customName = userTeam.customName ?? kvp.Value.customName;
                }
                merged.teams.Add(kvp.Value);
            }

            // Add user teams not in new
            foreach (var kvp in userTeams)
            {
                if (!newTeams.ContainsKey(kvp.Key))
                {
                    merged.teams.Add(kvp.Value);
                }
            }

            // Merge conferences
            var userConfs = user.conferences.ToDictionary(c => c.conferenceSeo, c => c);
            var newConfs = @new.conferences.ToDictionary(c => c.conferenceSeo, c => c);

            foreach (var kvp in newConfs)
            {
                if (userConfs.TryGetValue(kvp.Key, out var userConf))
                {
                    kvp.Value.customConferenceName = userConf.customConferenceName ?? kvp.Value.customConferenceName;
                }
                merged.conferences.Add(kvp.Value);
            }

            // Add user conferences not in new
            foreach (var kvp in userConfs)
            {
                if (!newConfs.ContainsKey(kvp.Key))
                {
                    merged.conferences.Add(kvp.Value);
                }
            }

            return merged;
        }

        private static Setting MergeSettings(Setting user, Setting @new)
        {
            var merged = new Setting
            {
                Timer = user.Timer > 0 ? user.Timer : @new.Timer,
                HomeTeam = user.HomeTeam ?? @new.HomeTeam,
                XmlToJson = user.XmlToJson ?? @new.XmlToJson,
                DisplayTeams = @new.DisplayTeams ?? new List<DisplayTeam>(),
                Sports = new List<Sport>()
            };

            // Merge display teams
            var userDisplayTeams = user.DisplayTeams?.ToDictionary(dt => dt.NcaaTeamName, dt => dt) ?? new Dictionary<string?, DisplayTeam>();
            if (@new.DisplayTeams != null)
            {
                foreach (var dt in @new.DisplayTeams)
                {
                    merged.DisplayTeams.Add(dt);
                }
            }
            if (user.DisplayTeams != null)
            {
                foreach (var dt in user.DisplayTeams)
                {
                    if (!merged.DisplayTeams.Any(mdt => mdt.NcaaTeamName == dt.NcaaTeamName))
                    {
                        merged.DisplayTeams.Add(dt);
                    }
                }
            }

            // Merge sports
            var userSports = user.Sports?.ToDictionary(s => s.SportShortName, s => s) ?? new Dictionary<string, Sport>();
            var newSports = @new.Sports?.ToDictionary(s => s.SportShortName, s => s) ?? new Dictionary<string, Sport>();

            foreach (var kvp in newSports)
            {
                var sport = kvp.Value;
                if (userSports.TryGetValue(kvp.Key, out var userSport))
                {
                    // Merge user settings
                    sport.Enabled = userSport.Enabled;
                    sport.GameDisplayMode = userSport.GameDisplayMode;
                    sport.ConferenceName = userSport.ConferenceName ?? sport.ConferenceName;
                    sport.Week = userSport.Week ?? sport.Week;
                    sport.OosUpdater = userSport.OosUpdater ?? sport.OosUpdater;
                    sport.ListsNeeded = userSport.ListsNeeded ?? sport.ListsNeeded;
                }
                merged.Sports.Add(sport);
            }

            // Add user sports not in new
            foreach (var kvp in userSports)
            {
                if (!newSports.ContainsKey(kvp.Key))
                {
                    merged.Sports.Add(kvp.Value);
                }
            }

            return merged;
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

            // Backup user config files
            var settingsPath = Path.Combine(appDir, "Settings.json");
            var nameConverterPath = Path.Combine(appDir, "NcaaNameConverter.json");
            string? settingsBackup = null;
            string? nameConverterBackup = null;

            if (File.Exists(settingsPath))
            {
                settingsBackup = Path.Combine(appDir, "Settings.json.backup");
                File.Copy(settingsPath, settingsBackup, true);
            }

            if (File.Exists(nameConverterPath))
            {
                nameConverterBackup = Path.Combine(appDir, "NcaaNameConverter.json.backup");
                File.Copy(nameConverterPath, nameConverterBackup, true);
            }

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

            // Merge config files
            try
            {
                if (settingsBackup != null && File.Exists(settingsPath))
                {
                    var userSettings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(settingsBackup));
                    var newSettings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(settingsPath));
                    if (userSettings != null && newSettings != null)
                    {
                        var merged = MergeSettings(userSettings, newSettings);
                        File.WriteAllText(settingsPath, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }

                if (nameConverterBackup != null && File.Exists(nameConverterPath))
                {
                    var userNameConverter = JsonSerializer.Deserialize<NameConverter>(File.ReadAllText(nameConverterBackup));
                    var newNameConverter = JsonSerializer.Deserialize<NameConverter>(File.ReadAllText(nameConverterPath));
                    if (userNameConverter != null && newNameConverter != null)
                    {
                        var merged = MergeNameConverters(userNameConverter, newNameConverter);
                        File.WriteAllText(nameConverterPath, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Config merge failed: {ex.Message}");
                // If merge fails, keep the new files
            }
            finally
            {
                // Cleanup backups
                if (settingsBackup != null && File.Exists(settingsBackup)) File.Delete(settingsBackup);
                if (nameConverterBackup != null && File.Exists(nameConverterBackup)) File.Delete(nameConverterBackup);
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