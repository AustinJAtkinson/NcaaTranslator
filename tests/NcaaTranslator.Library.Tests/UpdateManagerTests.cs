using System.Reflection;
using System.Text.Json;
using NcaaTranslator.Library;
using Xunit;

namespace NcaaTranslator.Library.Tests;

public class UpdateManagerTests
{
    [Fact]
    public void ShouldUpdate_WhenLatestIsNewer_ReturnsTrue()
    {
        Assert.True(UpdateManager.ShouldUpdate(new Version(4, 0, 0), new Version(4, 1, 0)));
    }

    [Fact]
    public void ShouldUpdate_WhenLatestEqualsCurrent_ReturnsFalse()
    {
        Assert.False(UpdateManager.ShouldUpdate(new Version(4, 0, 0), new Version(4, 0, 0)));
    }

    [Fact]
    public void ShouldUpdate_WhenLatestIsOlder_ReturnsFalse()
    {
        Assert.False(UpdateManager.ShouldUpdate(new Version(4, 1, 0), new Version(4, 0, 0)));
    }

    [Fact]
    public void GetInstalledExeFileName_UsesAssemblyName_AddsExeOnWindowsOnly()
    {
        var assemblyName = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name;
        Assert.False(string.IsNullOrWhiteSpace(assemblyName));

        var fileName = UpdateManager.GetInstalledExeFileName();

        Assert.DoesNotContain("Wpf", fileName, StringComparison.OrdinalIgnoreCase);
        if (OperatingSystem.IsWindows())
            Assert.Equal(assemblyName + ".exe", fileName);
        else
            Assert.Equal(assemblyName, fileName);
    }

    [Fact]
    public void MergeSettings_CopiesUserLookBackAndLookForward()
    {
        var user = new Setting
        {
            Timer = 20,
            Sports = new List<Sport>
            {
                new Sport
                {
                    SportName = "Football FCS",
                    SportShortName = "FCS",
                    Week = 3,
                    LookBack = 2,
                    LookForward = 4
                }
            }
        };
        var packaged = new Setting
        {
            Timer = 15,
            Sports = new List<Sport>
            {
                new Sport
                {
                    SportName = "Football FCS",
                    SportShortName = "FCS",
                    Week = 1,
                    LookBack = 0,
                    LookForward = 0
                }
            }
        };

        var merged = UpdateManager.MergeSettings(user, packaged);

        var sport = Assert.Single(merged.Sports!);
        Assert.Equal(3, sport.Week);
        Assert.Equal(2, sport.LookBack);
        Assert.Equal(4, sport.LookForward);
    }

    [Fact]
    public void MergeSettings_PreservesUserZeroLookBackAndLookForward()
    {
        var user = new Setting
        {
            Timer = 20,
            Sports = new List<Sport>
            {
                new Sport
                {
                    SportName = "Volleyball",
                    SportShortName = "WVB",
                    LookBack = 0,
                    LookForward = 0
                }
            }
        };
        var packaged = new Setting
        {
            Timer = 15,
            Sports = new List<Sport>
            {
                new Sport
                {
                    SportName = "Volleyball",
                    SportShortName = "WVB",
                    LookBack = 5,
                    LookForward = 5
                }
            }
        };

        var merged = UpdateManager.MergeSettings(user, packaged);

        var sport = Assert.Single(merged.Sports!);
        Assert.Equal(0, sport.LookBack);
        Assert.Equal(0, sport.LookForward);
    }

    [Fact]
    public void InstallExtractedUpdate_MergesUserSettingsAndNames_CopiesWindow()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncaa-install-" + Guid.NewGuid().ToString("N"));
        var current = Path.Combine(root, "app");
        var extract = Path.Combine(root, "extract");
        Directory.CreateDirectory(current);
        Directory.CreateDirectory(extract);

        try
        {
            File.WriteAllText(Path.Combine(current, "Settings.json"), """
                {"Timer":20,"HomeTeam":"NO DAK","Sports":[{"SportName":"Hockey","SportShortName":"HKY","LookBack":2,"LookForward":3}]}
                """);
            File.WriteAllText(Path.Combine(current, "NcaaNameConverter.json"), """
                {"teams":[{"name6Char":"NO DAK","customName":"Mine"}],"conferences":[{"conferenceSeo":"summit-league","customConferenceName":"Summit"}]}
                """);
            File.WriteAllText(Path.Combine(current, "Window.json"), """{"Width":1400,"Height":900}""");

            File.WriteAllText(Path.Combine(extract, "Settings.json"), """
                {"Timer":15,"HomeTeam":"OTHER","Sports":[{"SportName":"Hockey","SportShortName":"HKY","LookBack":0,"LookForward":0}]}
                """);
            File.WriteAllText(Path.Combine(extract, "NcaaNameConverter.json"), """
                {"teams":[{"name6Char":"NO DAK","customName":"Theirs"},{"name6Char":"UVA","customName":"Virginia"}],"conferences":[{"conferenceSeo":"summit-league","customConferenceName":"The Summit"}]}
                """);
            var exeName = UpdateManager.GetInstalledExeFileName();
            File.WriteAllText(Path.Combine(extract, exeName), "exe");

            var result = UpdateManager.InstallExtractedUpdate(
                extract,
                new GitHubRelease { tag_name = "v0.2.0" },
                current);

            Assert.NotNull(result);
            var dest = Path.Combine(root, "NcaaTranslator-0.2.0");
            Assert.Equal(dest, result.Directory);
            Assert.Equal("0.2.0", result.Version);
            Assert.Equal(Path.Combine(dest, exeName), result.ExePath);

            var settings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(Path.Combine(dest, "Settings.json")));
            Assert.NotNull(settings);
            Assert.Equal(20, settings.Timer);
            Assert.Equal("NO DAK", settings.HomeTeam);
            var sport = Assert.Single(settings.Sports!);
            Assert.Equal(2, sport.LookBack);
            Assert.Equal(3, sport.LookForward);

            var names = JsonSerializer.Deserialize<NameConverter>(File.ReadAllText(Path.Combine(dest, "NcaaNameConverter.json")));
            Assert.NotNull(names);
            Assert.Equal("Mine", Assert.Single(names.teams, team => team.name6Char == "NO DAK").customName);
            Assert.Contains(names.teams, team => team.name6Char == "UVA");
            Assert.Equal("Summit", Assert.Single(names.conferences, conf => conf.conferenceSeo == "summit-league").customConferenceName);
            Assert.Equal("""{"Width":1400,"Height":900}""", File.ReadAllText(Path.Combine(dest, "Window.json")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void InstallExtractedUpdate_MissingExe_StillReturnsDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncaa-install-" + Guid.NewGuid().ToString("N"));
        var current = Path.Combine(root, "app");
        var extract = Path.Combine(root, "extract");
        Directory.CreateDirectory(current);
        Directory.CreateDirectory(extract);

        try
        {
            File.WriteAllText(Path.Combine(extract, "readme.txt"), "release");

            var result = UpdateManager.InstallExtractedUpdate(
                extract,
                new GitHubRelease { tag_name = "v0.3.0" },
                current);

            Assert.NotNull(result);
            Assert.Equal(Path.Combine(root, "NcaaTranslator-0.3.0"), result.Directory);
            Assert.Null(result.ExePath);
            Assert.True(File.Exists(Path.Combine(result.Directory, "readme.txt")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void PackagedConfig_HasUniqueUpdaterMergeKeys()
    {
        var root = RepoRoot();
        var settings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(Path.Combine(root, "config", "Settings.json")));
        var names = JsonSerializer.Deserialize<NameConverter>(File.ReadAllText(Path.Combine(root, "config", "NcaaNameConverter.json")));

        Assert.NotNull(settings?.Sports);
        Assert.NotNull(names);

        AssertUnique(settings.Sports.Select(sport => sport.SportShortName));
        AssertUnique(names.teams.Select(team => team.name6Char));
        AssertUnique(names.conferences.Select(conference => conference.conferenceSeo));
    }

    private static void AssertUnique(IEnumerable<string?> keys)
    {
        var list = keys.ToList();
        Assert.NotEmpty(list);
        Assert.All(list, key => Assert.False(string.IsNullOrWhiteSpace(key)));
        Assert.Equal(list.Count, list.Distinct(StringComparer.Ordinal).Count());
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NcaaTranslator.sln"))
                && File.Exists(Path.Combine(dir.FullName, "config", "Settings.json")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find the repo config directory.");
    }
}
