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

    public class UpdateCheckResult
    {
        public bool Available { get; set; }
        public string? Version { get; set; }
        public string CurrentVersion { get; set; } = "";
    }

    public class UpdateInstallResult
    {
        public string Version { get; set; } = "";
        public string Directory { get; set; } = "";
        public string? ExePath { get; set; }
    }

    public static class UpdateManager
    {
        private const string GitHubRepo = "AustinJAtkinson/NcaaTranslator";
        private const string ApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly SingleFlightGate InstallGate = new();

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

        internal static string? ResolveInstalledExe(string versionDir)
        {
            foreach (var name in new[] { "NcaaTranslator.Desktop.exe", "NcaaTranslator.Wpf.exe" })
            {
                var path = Path.Combine(versionDir, name);
                if (File.Exists(path))
                    return path;
            }

            return null;
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

        private static string? FindLaunchExe(string versionDir)
        {
            var preferred = ResolveInstalledExe(versionDir);
            if (preferred != null)
                return preferred;

            var fallback = Path.Combine(versionDir, GetInstalledExeFileName());
            return File.Exists(fallback) ? fallback : null;
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

        private static Dictionary<string, T> ToLastWins<T>(IEnumerable<T>? items, Func<T, string?> keySelector)
        {
            var dict = new Dictionary<string, T>(StringComparer.Ordinal);
            if (items == null)
                return dict;

            foreach (var item in items)
            {
                var key = keySelector(item);
                if (string.IsNullOrEmpty(key))
                    continue;
                dict[key] = item;
            }

            return dict;
        }

        private static NameConverter MergeNameConverters(NameConverter user, NameConverter @new)
        {
            var merged = new NameConverter();
            var userTeams = ToLastWins(user.teams, team => team.name6Char);
            var newTeams = ToLastWins(@new.teams, team => team.name6Char);

            foreach (var kvp in newTeams)
            {
                if (userTeams.TryGetValue(kvp.Key, out var userTeam))
                    kvp.Value.customName = userTeam.customName ?? kvp.Value.customName;
                merged.teams.Add(kvp.Value);
            }

            foreach (var kvp in userTeams)
            {
                if (!newTeams.ContainsKey(kvp.Key))
                    merged.teams.Add(kvp.Value);
            }

            var userConfs = ToLastWins(user.conferences, conference => conference.conferenceSeo);
            var newConfs = ToLastWins(@new.conferences, conference => conference.conferenceSeo);

            foreach (var kvp in newConfs)
            {
                if (userConfs.TryGetValue(kvp.Key, out var userConf))
                    kvp.Value.customConferenceName = userConf.customConferenceName ?? kvp.Value.customConferenceName;
                merged.conferences.Add(kvp.Value);
            }

            foreach (var kvp in userConfs)
            {
                if (!newConfs.ContainsKey(kvp.Key))
                    merged.conferences.Add(kvp.Value);
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

            if (@new.DisplayTeams != null)
            {
                foreach (var dt in @new.DisplayTeams)
                    merged.DisplayTeams.Add(dt);
            }
            if (user.DisplayTeams != null)
            {
                foreach (var dt in user.DisplayTeams)
                {
                    if (!merged.DisplayTeams.Any(mdt => mdt.NcaaTeamName == dt.NcaaTeamName))
                        merged.DisplayTeams.Add(dt);
                }
            }

            var userSports = ToLastWins(user.Sports, sport => sport.SportShortName);
            var newSports = ToLastWins(@new.Sports, sport => sport.SportShortName);

            foreach (var kvp in newSports)
            {
                var sport = kvp.Value;
                if (userSports.TryGetValue(kvp.Key, out var userSport))
                {
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

            foreach (var kvp in userSports)
            {
                if (!newSports.ContainsKey(kvp.Key))
                    merged.Sports.Add(kvp.Value);
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

        public static async Task<UpdateInstallResult?> DownloadAndInstallUpdateAsync(GitHubRelease release)
        {
            if (InstallOverride != null)
                return await InstallOverride(release).ConfigureAwait(false);

            UpdateInstallResult? result = null;
            var ran = await InstallGate.RunAsync(async () =>
            {
                result = await DownloadAndInstallCoreAsync(release).ConfigureAwait(false);
            }).ConfigureAwait(false);
            return ran ? result : null;
        }

        private static async Task<UpdateInstallResult?> DownloadAndInstallCoreAsync(GitHubRelease release)
        {
            var asset = release.assets?.FirstOrDefault(item => item.name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
            if (string.IsNullOrWhiteSpace(asset?.name) || asset.browser_download_url == null)
                return null;

            var currentAppDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var tempDir = Path.Combine(currentAppDir, "update_temp");
            Directory.CreateDirectory(tempDir);

            var tempPath = Path.Combine(tempDir, asset.name);
            var extractPath = Path.Combine(tempDir, "extract-" + Guid.NewGuid().ToString("N"));

            try
            {
                using var response = await _httpClient.GetAsync(asset.browser_download_url);
                response.EnsureSuccessStatusCode();
                await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    await response.Content.CopyToAsync(fs);

                File.SetAttributes(tempPath, FileAttributes.Normal);

                // Antivirus scanners lock a file that was just downloaded from the internet.
                await Task.Delay(2000);

                // Extract a copy so a lock on the download itself does not fail the unzip.
                var extractZipPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(asset.name) + "_extract.zip");
                File.Copy(tempPath, extractZipPath, true);

                const int maxRetries = 5;
                for (var i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        if (Directory.Exists(extractPath))
                            Directory.Delete(extractPath, true);
                        Directory.CreateDirectory(extractPath);
                        System.IO.Compression.ZipFile.ExtractToDirectory(extractZipPath, extractPath);
                        break;
                    }
                    catch (IOException) when (i < maxRetries - 1)
                    {
                        await Task.Delay((int)Math.Pow(2, i) * 1000);
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
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
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
            var stagingDir = Path.Combine(parentDir, $"NcaaTranslator-{latestVersion}.staging-{Guid.NewGuid():N}");

            try
            {
                CopyDirectory(extractPath, stagingDir);
                if (!TryMergeConfig(currentAppDir, stagingDir))
                {
                    DeleteDirectory(stagingDir);
                    return null;
                }

                var currentWindowPath = Path.Combine(currentAppDir, WindowBounds.FileName);
                if (File.Exists(currentWindowPath))
                    File.Copy(currentWindowPath, Path.Combine(stagingDir, WindowBounds.FileName), true);

                var stagedExe = FindLaunchExe(stagingDir);
                if (stagedExe == null)
                {
                    DeleteDirectory(stagingDir);
                    return null;
                }

                PromoteDirectory(stagingDir, newVersionDir);
                var exeName = Path.GetFileName(stagedExe);
                var finalExe = Path.Combine(newVersionDir, exeName);
                return new UpdateInstallResult
                {
                    Version = latestVersion.ToString(),
                    Directory = newVersionDir,
                    ExePath = File.Exists(finalExe) ? finalExe : null
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update installation failed: {ex.Message}");
                DeleteDirectory(stagingDir);
                return null;
            }
        }

        private static bool TryMergeConfig(string currentAppDir, string newVersionDir)
        {
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

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Config merge failed: {ex.Message}");
                return false;
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(source, file);
                var targetPath = Path.Combine(destination, relativePath);
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir))
                    Directory.CreateDirectory(targetDir);
                File.Copy(file, targetPath, true);
            }
        }

        private static void PromoteDirectory(string stagingDir, string finalDir)
        {
            var backup = finalDir + ".replacing";
            DeleteDirectory(backup);
            if (Directory.Exists(finalDir))
                Directory.Move(finalDir, backup);

            try
            {
                Directory.Move(stagingDir, finalDir);
            }
            catch
            {
                if (!Directory.Exists(finalDir) && Directory.Exists(backup))
                    Directory.Move(backup, finalDir);
                throw;
            }

            DeleteDirectory(backup);
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }
}
