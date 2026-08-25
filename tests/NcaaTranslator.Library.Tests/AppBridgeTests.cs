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
    public void Handle_UnknownMethod_ReturnsError()
    {
        using var workspace = new TempWorkspace();

        using var doc = Handle("""{"id":"x","method":"start"}""");

        Assert.Equal("x", Id(doc));
        Assert.False(doc.RootElement.TryGetProperty("result", out _));
        Assert.Contains("Unknown method", Error(doc));
        Assert.Contains("start", Error(doc));
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
