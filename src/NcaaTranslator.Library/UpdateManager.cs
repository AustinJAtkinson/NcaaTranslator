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
                    if (latestVersion != currentVersion)
                    {
                        // Update available
                        var newExePath = await DownloadAndInstallUpdateAsync(latestRelease);
                        if (newExePath != null)
                        {
                            // Launch new version and exit
                            Process.Start(newExePath);
                            Environment.Exit(0);
                        }
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
                DisplayTeams = new List<DisplayTeam>(),
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

        private static async Task<string?> DownloadAndInstallUpdateAsync(GitHubRelease release)
        {
            if (release.assets == null || !release.assets.Any())
                return null;

            // Assume the first asset is the zip
            var asset = release.assets.FirstOrDefault(a => a.name?.EndsWith(".zip") == true);
            if (asset?.browser_download_url == null)
                return null;

            var currentAppDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var tempDir = Path.Combine(currentAppDir, "update_temp");
            Directory.CreateDirectory(tempDir);

            var tempPath = Path.Combine(tempDir, asset.name);
            var extractPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(asset.name));

            try
            {
                // Download
                using var response = await _httpClient.GetAsync(asset.browser_download_url);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(tempPath, FileMode.Create);
                await response.Content.CopyToAsync(fs);

                // Unblock the downloaded file (remove restricted attributes from internet download)
                File.SetAttributes(tempPath, FileAttributes.Normal);

                // Wait for antivirus scanning to complete
                await Task.Delay(2000);

                // Copy to a new file to avoid any locks on the original
                var extractZipPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(asset.name) + "_extract.zip");
                File.Copy(tempPath, extractZipPath, true);

                // Extract with retry to handle file lock issues
                const int maxRetries = 5;
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(extractZipPath, extractPath);
                        break; // Success, exit loop
                    }
                    catch (IOException) when (i < maxRetries - 1)
                    {
                        int delayMs = (int)Math.Pow(2, i) * 1000; // Exponential backoff: 1s, 2s, 4s, 8s
                        await Task.Delay(delayMs);
                    }
                }

                // Install
                var newExePath = InstallUpdateAsync(extractPath, release);
                return newExePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update installation failed: {ex.Message}");
                return null;
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static string? InstallUpdateAsync(string extractPath, GitHubRelease release)
        {
            var currentAppDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var parentDir = Path.GetDirectoryName(currentAppDir);
            if (parentDir == null) return null;

            var latestVersion = Version.Parse(release.tag_name!.TrimStart('v'));
            var newVersionDir = Path.Combine(parentDir, $"NcaaTranslator-{latestVersion}");

            // Create new version directory
            Directory.CreateDirectory(newVersionDir);

            // Copy all files from extract to new version dir
            foreach (var file in Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(extractPath, file);
                var targetPath = Path.Combine(newVersionDir, relativePath);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                File.Copy(file, targetPath, true);
            }

            // Merge config files
            try
            {
                var currentSettingsPath = Path.Combine(currentAppDir, "Settings.json");
                var currentNameConverterPath = Path.Combine(currentAppDir, "NcaaNameConverter.json");
                var newSettingsPath = Path.Combine(newVersionDir, "Settings.json");
                var newNameConverterPath = Path.Combine(newVersionDir, "NcaaNameConverter.json");

                if (File.Exists(currentSettingsPath) && File.Exists(newSettingsPath))
                {
                    var userSettings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(currentSettingsPath));
                    var newSettings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(newSettingsPath));
                    if (userSettings != null && newSettings != null)
                    {
                        var merged = MergeSettings(userSettings, newSettings);
                        File.WriteAllText(newSettingsPath, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }

                if (File.Exists(currentNameConverterPath) && File.Exists(newNameConverterPath))
                {
                    var userNameConverter = JsonSerializer.Deserialize<NameConverter>(File.ReadAllText(currentNameConverterPath));
                    var newNameConverter = JsonSerializer.Deserialize<NameConverter>(File.ReadAllText(newNameConverterPath));
                    if (userNameConverter != null && newNameConverter != null)
                    {
                        var merged = MergeNameConverters(userNameConverter, newNameConverter);
                        File.WriteAllText(newNameConverterPath, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Config merge failed: {ex.Message}");
                // If merge fails, keep the new files
            }

            var newExePath = Path.Combine(newVersionDir, "NcaaTranslator.Wpf.exe");
            return File.Exists(newExePath) ? newExePath : null;
        }

    }
}