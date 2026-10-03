using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public class UpdateCheckResult
    {
        public bool Available { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Version { get; set; }
        public string CurrentVersion { get; set; } = "";
    }

    public class UpdateInstallResult
    {
        public string Version { get; set; } = "";
        public string Directory { get; set; } = "";
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? ExePath { get; set; }
    }

    public static class UpdateManager
    {
        private const string GitHubRepo = "AustinJAtkinson/NcaaTranslator";
        private const string ApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
        private static readonly HttpClient _httpClient = new HttpClient();

        internal static GitHubRelease? PendingRelease { get; set; }
        internal static Func<Version>? VersionOverride { get; set; }
        internal static Func<Task<GitHubRelease?>>? FetchOverride { get; set; }
        internal static Func<GitHubRelease, Task<UpdateInstallResult?>>? InstallOverride { get; set; }

        internal static void ResetForTests()
        {
            PendingRelease = null;
            VersionOverride = null;
            FetchOverride = null;
            InstallOverride = null;
        }

        static UpdateManager()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NcaaTranslator", GetCurrentVersion().ToString()));
        }

        public static bool ShouldUpdate(Version current, Version latest)
        {
            return latest > current;
        }

        public static async Task<GitHubRelease?> GetAvailableUpdateAsync()
        {
            try
            {
                var currentVersion = GetCurrentVersion();
                var latestRelease = await GetLatestReleaseAsync();

                if (latestRelease?.tag_name != null
                    && Version.TryParse(latestRelease.tag_name.TrimStart('v'), out var latestVersion)
                    && ShouldUpdate(currentVersion, latestVersion))
                {
                    PendingRelease = latestRelease;
                    return latestRelease;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }

            PendingRelease = null;
            return null;
        }

        public static Version GetCurrentVersion()
        {
            if (VersionOverride != null)
                return VersionOverride();

            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            return assembly.GetName().Version ?? new Version(1, 0, 0);
        }

        internal static string GetInstalledExeFileName()
        {
            var name = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name
                       ?? "NcaaTranslator.Desktop";
            return OperatingSystem.IsWindows() ? name + ".exe" : name;
        }

        private static async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            if (FetchOverride != null)
                return await FetchOverride();

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

        internal static Setting MergeSettings(Setting user, Setting @new)
        {
            var merged = new Setting
            {
                Timer = user.Timer > 0 ? user.Timer : @new.Timer,
                HomeTeam = user.HomeTeam ?? @new.HomeTeam,
                XmlToJson = user.XmlToJson ?? @new.XmlToJson,
                ClockFormats = MergeClockFormats(user.ClockFormats, @new.ClockFormats),
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
                    sport.SeasonYear = userSport.SeasonYear ?? sport.SeasonYear;
                    sport.LookBack = userSport.LookBack;
                    sport.LookForward = userSport.LookForward;
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

        private static ClockFormats? MergeClockFormats(ClockFormats? user, ClockFormats? @new)
        {
            if (user == null)
                return @new;
            if (@new == null)
                return user;

            return new ClockFormats
            {
                PreGame = user.PreGame ?? @new.PreGame,
                Final = user.Final ?? @new.Final
            };
        }

        public static Task<UpdateInstallResult?> DownloadAndInstallUpdateAsync(GitHubRelease release)
        {
            if (InstallOverride != null)
                return InstallOverride(release);

            return DownloadAndInstallCoreAsync(release);
        }

        private static async Task<UpdateInstallResult?> DownloadAndInstallCoreAsync(GitHubRelease release)
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

                return InstallExtractedUpdate(extractPath, release, currentAppDir);
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

        internal static UpdateInstallResult? InstallExtractedUpdate(string extractPath, GitHubRelease release, string currentAppDir)
        {
            currentAppDir = currentAppDir.TrimEnd(Path.DirectorySeparatorChar);
            var parentDir = Path.GetDirectoryName(currentAppDir);
            if (parentDir == null || release.tag_name == null)
                return null;
            if (!Version.TryParse(release.tag_name.TrimStart('v'), out var latestVersion))
                return null;

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

            var currentWindowPath = Path.Combine(currentAppDir, WindowBounds.FileName);
            if (File.Exists(currentWindowPath))
                File.Copy(currentWindowPath, Path.Combine(newVersionDir, WindowBounds.FileName), true);

            var newExePath = Path.Combine(newVersionDir, GetInstalledExeFileName());
            return new UpdateInstallResult
            {
                Version = latestVersion.ToString(),
                Directory = newVersionDir,
                ExePath = File.Exists(newExePath) ? newExePath : null
            };
        }

    }
}