using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class ConfigPathTests : IDisposable
{
    private readonly TempWorkspace _workspace = new(isolateCwd: true);

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void Settings_LoadAndSave_UsesBaseDirectory()
    {
        WriteSettings("""{"Timer":20,"HomeTeam":"NO DAK","Sports":[],"DisplayTeams":[]}""");

        Settings.Load();

        Assert.Equal(20, Settings.SettingsList!.Timer);
        Assert.Equal("NO DAK", Settings.homeTeam);

        Settings.SettingsList.Timer = 42;
        Settings.SettingsList.HomeTeam = "NDSU";
        Settings.Save();

        Settings.SettingsList = null;
        Settings.Load();

        Assert.Equal(42, Settings.SettingsList!.Timer);
        Assert.Equal("NDSU", Settings.homeTeam);
        Assert.Equal(ExpectedPath("Settings.json"), Settings.ResolvePath());
        AssertConfigOnlyInBaseDirectory("Settings.json");
    }

    [Fact]
    public void Settings_Load_MissingFile_ThrowsFileNotFoundWithResolvedPath()
    {
        var expected = ExpectedPath("Settings.json");
        var ex = Assert.Throws<FileNotFoundException>(() => Settings.Load());

        Assert.Contains("Settings.json", ex.Message);
        Assert.Contains(expected, ex.Message);
        Assert.DoesNotContain(Path.GetFullPath(_workspace.CwdPath), ex.Message);
        Assert.Equal(expected, ex.FileName);
        Assert.Null(Settings.SettingsList);
    }

    [Fact]
    public void Settings_Load_NullJson_ThrowsInvalidDataException()
    {
        WriteSettings("null");

        var ex = Assert.Throws<InvalidDataException>(() => Settings.Load());

        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Settings.json", ex.Message);
        Assert.Null(Settings.SettingsList);
    }

    [Fact]
    public void Settings_Load_ExplicitPath_IgnoresBaseDirectoryOnSave()
    {
        var otherDir = TestHelpers.CreateTempDir();
        try
        {
            var path = Path.Combine(otherDir, "custom-settings.json");
            File.WriteAllText(path, """{"Timer":7,"HomeTeam":"UVA"}""");

            Settings.Load(path);
            Assert.Equal(7, Settings.SettingsList!.Timer);

            Settings.SettingsList.Timer = 9;
            Settings.Save();

            Assert.Equal(Path.GetFullPath(path), Settings.ResolvePath());
            Assert.Contains("\"Timer\":9", File.ReadAllText(path));
            Assert.False(File.Exists(Path.Combine(_workspace.DirectoryPath, "Settings.json")));
            Assert.False(File.Exists(Path.Combine(_workspace.CwdPath, "custom-settings.json")));
            Assert.False(File.Exists(Path.Combine(_workspace.CwdPath, "Settings.json")));
        }
        finally
        {
            try { Directory.Delete(otherDir, true); } catch { }
        }
    }

    [Fact]
    public void NameConverters_LoadAndReload_UsesBaseDirectory()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);

        NameConverters.FilePath = "NcaaNameConverter.json";
        NameConverters.Load();

        Assert.Equal("UND", NameConverters.LookupTeam(new Names { name6Char = "NO DAK" }));

        NameConverters.NameList!.teams.Add(new Team
        {
            name6Char = "ZZZ",
            nameShort = "Zed",
            customName = "Zed"
        });
        NameConverters.Reload();

        Assert.Equal(ExpectedPath("NcaaNameConverter.json"), NameConverters.ResolvePath());
        Assert.Contains(NameConverters.GetTeams(), t => t.name6Char == "ZZZ");
        Assert.Contains("ZZZ", File.ReadAllText(NameConverters.ResolvePath()));
        AssertConfigOnlyInBaseDirectory("NcaaNameConverter.json");
    }

    [Fact]
    public void NameConverters_Load_MissingFile_ThrowsFileNotFoundWithResolvedPath()
    {
        var expected = ExpectedPath("NcaaNameConverter.json");
        var ex = Assert.Throws<FileNotFoundException>(() => NameConverters.Load());

        Assert.Contains("NcaaNameConverter.json", ex.Message);
        Assert.Contains(expected, ex.Message);
        Assert.DoesNotContain(Path.GetFullPath(_workspace.CwdPath), ex.Message);
        Assert.Equal(expected, ex.FileName);
        Assert.Null(NameConverters.NameList);
    }

    [Fact]
    public void NameConverters_Load_NullJson_ThrowsInvalidDataException()
    {
        File.WriteAllText(Path.Combine(_workspace.DirectoryPath, "NcaaNameConverter.json"), "null");

        var ex = Assert.Throws<InvalidDataException>(() => NameConverters.Load());

        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NcaaNameConverter.json", ex.Message);
        Assert.Null(NameConverters.NameList);
    }

    [Fact]
    public void WpfCsproj_ConfigCopy_UsesPreserveNewest()
    {
        var csproj = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "NcaaTranslator.Wpf", "NcaaTranslator.Wpf.csproj"));

        Assert.Contains("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>", csproj);
        Assert.DoesNotContain("<CopyToOutputDirectory>Always</CopyToOutputDirectory>", csproj);
        Assert.Contains("Settings.json", csproj);
        Assert.Contains("NcaaNameConverter.json", csproj);
    }

    private void WriteSettings(string json)
    {
        File.WriteAllText(Path.Combine(_workspace.DirectoryPath, "Settings.json"), json);
    }

    private void AssertConfigOnlyInBaseDirectory(string fileName)
    {
        Assert.True(File.Exists(ExpectedPath(fileName)));
        Assert.False(File.Exists(Path.Combine(_workspace.CwdPath, fileName)));
        Assert.NotEqual(
            Path.GetFullPath(_workspace.CwdPath),
            Path.GetFullPath(_workspace.DirectoryPath));
    }

    private string ExpectedPath(string fileName) =>
        Path.GetFullPath(Path.Combine(_workspace.DirectoryPath, fileName));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NcaaTranslator.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repo root containing NcaaTranslator.sln");
    }
}
