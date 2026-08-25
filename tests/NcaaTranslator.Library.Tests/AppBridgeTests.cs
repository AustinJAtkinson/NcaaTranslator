using System.Text.Json;
using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class AppBridgeTests
{
    [Fact]
    public void Handle_Ping_ReturnsOk()
    {
        using var workspace = new TempWorkspace();

        using var doc = Handle("""{"id":"1","method":"ping"}""");

        Assert.Equal("1", Id(doc));
        Assert.True(Result(doc).GetProperty("ok").GetBoolean());
        Assert.False(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void Handle_GetSettings_UsesAlreadyLoadedSettings()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.UseSettings("UVA");
        Settings.SettingsList!.Timer = 42;

        using var doc = Handle("""{"id":"2","method":"getSettings"}""");

        Assert.Equal("2", Id(doc));
        var result = Result(doc);
        Assert.Equal(42, result.GetProperty("timer").GetInt32());
        Assert.Equal("UVA", result.GetProperty("homeTeam").GetString());
        Assert.False(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void Handle_GetSettings_LoadsFromBaseDirectoryWhenUnloaded()
    {
        using var workspace = new TempWorkspace(isolateCwd: true);
        File.WriteAllText(
            Path.Combine(workspace.DirectoryPath, "Settings.json"),
            """{"Timer":15,"HomeTeam":"NDSU"}""");
        Assert.Null(Settings.SettingsList);

        using var doc = Handle("""{"id":"s","method":"getSettings"}""");

        Assert.Equal("s", Id(doc));
        var result = Result(doc);
        Assert.Equal(15, result.GetProperty("timer").GetInt32());
        Assert.Equal("NDSU", result.GetProperty("homeTeam").GetString());
        Assert.Equal(15, Settings.SettingsList!.Timer);
        Assert.False(File.Exists(Path.Combine(workspace.CwdPath, "Settings.json")));
    }

    [Fact]
    public void Handle_GetSettings_MissingFile_ReturnsErrorWithoutThrowing()
    {
        using var workspace = new TempWorkspace(isolateCwd: true);
        Assert.Null(Settings.SettingsList);

        using var doc = Handle("""{"id":"s","method":"getSettings"}""");

        Assert.Equal("s", Id(doc));
        Assert.False(doc.RootElement.TryGetProperty("result", out _));
        var error = Error(doc);
        Assert.Contains("Settings.json", error);
        Assert.Contains(Path.GetFullPath(workspace.DirectoryPath), error);
        Assert.DoesNotContain(Path.GetFullPath(workspace.CwdPath), error);
        Assert.Null(Settings.SettingsList);
    }

    [Fact]
    public void Handle_GetSettings_InvalidJson_ReturnsError()
    {
        using var workspace = new TempWorkspace(isolateCwd: true);
        File.WriteAllText(Path.Combine(workspace.DirectoryPath, "Settings.json"), "null");

        using var doc = Handle("""{"id":"bad","method":"getSettings"}""");

        Assert.Equal("bad", Id(doc));
        Assert.False(doc.RootElement.TryGetProperty("result", out _));
        Assert.Contains("invalid", Error(doc), StringComparison.OrdinalIgnoreCase);
        Assert.Null(Settings.SettingsList);
    }

    [Fact]
    public void Handle_GetSettings_IncludesSportsList()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.UseSettings();
        Settings.SettingsList!.Sports!.Add(TestHelpers.CreateSport());

        using var doc = Handle("""{"id":"s","method":"getSettings"}""");

        Assert.Equal("s", Id(doc));
        var result = Result(doc);
        var sports = result.GetProperty("sports");
        Assert.Equal(1, sports.GetArrayLength());
        var sport = sports[0];
        Assert.Equal("Football FCS", sport.GetProperty("name").GetString());
        Assert.Equal("FCS", sport.GetProperty("short").GetString());
        Assert.Equal("MFB", sport.GetProperty("code").GetString());
        Assert.True(sport.GetProperty("enabled").GetBoolean());
        Assert.Equal("MVFC", sport.GetProperty("conferenceName").GetString());
        Assert.Equal(12, sport.GetProperty("division").GetInt32());
        Assert.Equal(2, sport.GetProperty("week").GetInt32());
        Assert.Equal(2025, sport.GetProperty("seasonYear").GetInt32());
        Assert.Equal("Live", sport.GetProperty("gameDisplayMode").GetString());
        var lists = sport.GetProperty("listsNeeded");
        Assert.True(lists.GetProperty("conferenceGames").GetBoolean());
        Assert.True(lists.GetProperty("nonConferenceGames").GetBoolean());
        Assert.True(lists.GetProperty("top25Games").GetBoolean());
        Assert.False(sport.GetProperty("oosUpdater").GetProperty("enabled").GetBoolean());
        Assert.True(result.TryGetProperty("displayTeams", out _));
        Assert.True(result.TryGetProperty("xmlToJson", out _));
    }

    [Fact]
    public void Handle_SaveSettings_PersistsHomeTeamAsName6Char()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);
        TestHelpers.UseSettings("NDSU");
        Settings.SettingsList!.Timer = 20;

        using var doc = Handle(
            """{"id":"sv","method":"saveSettings","params":{"timer":15,"homeTeam":"UND"}}""");

        Assert.Equal("sv", Id(doc));
        Assert.False(doc.RootElement.TryGetProperty("error", out _));
        Assert.Equal("NO DAK", Settings.SettingsList!.HomeTeam);
        Assert.Equal(15, Settings.SettingsList.Timer);
        Assert.Equal("NO DAK", Result(doc).GetProperty("homeTeam").GetString());
        Assert.Equal(15, Result(doc).GetProperty("timer").GetInt32());

        var saved = File.ReadAllText(Path.Combine(workspace.DirectoryPath, "Settings.json"));
        Assert.Contains("\"HomeTeam\":\"NO DAK\"", saved);
        Assert.DoesNotContain("\"HomeTeam\":\"UND\"", saved);

        Settings.SettingsList = null;
        Settings.Load();
        Assert.Equal("NO DAK", Settings.SettingsList!.HomeTeam);
        Assert.Equal(15, Settings.SettingsList.Timer);
    }

    [Fact]
    public void Handle_SaveTeamCustomName_RoundTrips()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);

        using var save = Handle(
            """{"id":"1","method":"saveTeamCustomName","params":{"name6Char":"NO DAK","customName":"Hawks"}}""");

        Assert.Equal("1", Id(save));
        Assert.False(save.RootElement.TryGetProperty("error", out _));
        Assert.Equal("Hawks", Result(save).GetProperty("customName").GetString());
        Assert.Equal("NO DAK", Result(save).GetProperty("name6Char").GetString());

        using var get = Handle("""{"id":"2","method":"getTeams"}""");
        var teams = Result(get);
        var team = teams.EnumerateArray().First(t => t.GetProperty("name6Char").GetString() == "NO DAK");
        Assert.Equal("Hawks", team.GetProperty("customName").GetString());
        Assert.Equal("Hawks", NameConverters.LookupTeam(new Names { name6Char = "NO DAK" }));

        NameConverters.NameList = null;
        NameConverters.TeamDict = new Dictionary<string, Team>();
        NameConverters.Load();
        Assert.Equal("Hawks", NameConverters.LookupTeam(new Names { name6Char = "NO DAK" }));
    }

    [Fact]
    public void Handle_SaveConferenceCustomName_RoundTrips()
    {
        using var workspace = new TempWorkspace();
        TestHelpers.WriteDefaultNames(workspace.DirectoryPath);

        using var save = Handle(
            """{"id":"1","method":"saveConferenceCustomName","params":{"conferenceSeo":"mvc","customConferenceName":"Missouri Valley"}}""");

        Assert.Equal("1", Id(save));
        Assert.False(save.RootElement.TryGetProperty("error", out _));
        Assert.Equal("Missouri Valley", Result(save).GetProperty("customConferenceName").GetString());
        Assert.Equal("mvc", Result(save).GetProperty("conferenceSeo").GetString());

        using var get = Handle("""{"id":"2","method":"getConferences"}""");
        var conferences = Result(get);
        var conference = conferences.EnumerateArray().First(c => c.GetProperty("conferenceSeo").GetString() == "mvc");
        Assert.Equal("Missouri Valley", conference.GetProperty("customConferenceName").GetString());
        Assert.Equal("Missouri Valley", NameConverters.LookupConf(new Conference { conferenceSeo = "mvc" }));

        NameConverters.NameList = null;
        NameConverters.ConfDict = new Dictionary<string, Conferences>();
        NameConverters.Load();
        Assert.Equal("Missouri Valley", NameConverters.LookupConf(new Conference { conferenceSeo = "mvc" }));
    }

    [Fact]
    public void Handle_UnknownMethod_ReturnsError()
    {
        using var workspace = new TempWorkspace();

        using var doc = Handle("""{"id":"x","method":"nope"}""");

        Assert.Equal("x", Id(doc));
        Assert.False(doc.RootElement.TryGetProperty("result", out _));
        Assert.Contains("Unknown method", Error(doc));
        Assert.Contains("nope", Error(doc));
    }

    [Fact]
    public void Handle_InvalidJson_ReturnsError()
    {
        using var workspace = new TempWorkspace();

        using var doc = Handle("{not-json");

        Assert.False(doc.RootElement.TryGetProperty("result", out _));
        Assert.Contains("Invalid JSON", Error(doc));
    }

    [Fact]
    public void Handle_EmptyRequest_ReturnsError()
    {
        using var workspace = new TempWorkspace();

        using var doc = Handle("   ");

        Assert.Contains("Empty request", Error(doc));
    }

    private static JsonDocument Handle(string json) => JsonDocument.Parse(AppBridge.Handle(json));

    private static string? Id(JsonDocument doc) =>
        doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;

    private static JsonElement Result(JsonDocument doc) => doc.RootElement.GetProperty("result");

    private static string Error(JsonDocument doc) =>
        doc.RootElement.GetProperty("error").GetString() ?? "";
}
