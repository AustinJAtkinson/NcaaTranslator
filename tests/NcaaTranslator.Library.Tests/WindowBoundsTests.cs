using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class WindowBoundsTests : IDisposable
{
    private readonly TempWorkspace _workspace = new(isolateCwd: true);

    public WindowBoundsTests()
    {
        WindowBounds.BaseDirectory = _workspace.DirectoryPath;
    }

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void Load_MissingFile_ReturnsDefaultSize()
    {
        var bounds = WindowBounds.Load();

        Assert.Equal(WindowBounds.DefaultWidth, bounds.Width);
        Assert.Equal(WindowBounds.DefaultHeight, bounds.Height);
        Assert.Null(bounds.Left);
        Assert.Null(bounds.Top);
        Assert.False(bounds.Maximized);
        Assert.Equal(ExpectedPath(), WindowBounds.ResolvePath());
        Assert.False(File.Exists(ExpectedPath()));
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaultSize()
    {
        File.WriteAllText(ExpectedPath(), "{not-json");

        var bounds = WindowBounds.Load();

        Assert.Equal(WindowBounds.DefaultWidth, bounds.Width);
        Assert.Equal(WindowBounds.DefaultHeight, bounds.Height);
        Assert.False(bounds.Maximized);
    }

    [Fact]
    public void Load_ClampsWidthAndHeightBelowMin()
    {
        WriteBounds("""{"Width":800,"Height":400,"Left":40,"Top":50,"Maximized":false}""");

        var bounds = WindowBounds.Load();

        Assert.Equal(WindowBounds.MinWidth, bounds.Width);
        Assert.Equal(WindowBounds.MinHeight, bounds.Height);
        Assert.Equal(40, bounds.Left);
        Assert.Equal(50, bounds.Top);
        Assert.False(bounds.Maximized);
    }

    [Fact]
    public void ApplySizeChange_WhenMaximized_KeepsRestoreSize()
    {
        var bounds = new WindowBounds
        {
            Width = 1200,
            Height = 800,
            Left = 80,
            Top = 60,
            Maximized = false,
        };

        bounds.ApplySizeChange(1920, 1080, 0, 0, maximized: true);

        Assert.True(bounds.Maximized);
        Assert.Equal(1200, bounds.Width);
        Assert.Equal(800, bounds.Height);
        Assert.Equal(80, bounds.Left);
        Assert.Equal(60, bounds.Top);
    }

    [Fact]
    public void ApplyMaximized_DoesNotReplaceRestoreGeometry()
    {
        var bounds = new WindowBounds
        {
            Width = 1300,
            Height = 820,
            Left = 12,
            Top = 24,
        };

        bounds.ApplyMaximized();

        Assert.True(bounds.Maximized);
        Assert.Equal(1300, bounds.Width);
        Assert.Equal(820, bounds.Height);
        Assert.Equal(12, bounds.Left);
        Assert.Equal(24, bounds.Top);
    }

    [Fact]
    public void Load_OffScreenOrigin_CentersWithDefaultSize()
    {
        WriteBounds("""{"Width":1200,"Height":800,"Left":8000,"Top":9000,"Maximized":false}""");
        var displays = new[] { new DisplayRect(0, 0, 1920, 1080) };

        var bounds = WindowBounds.Load(displays);

        Assert.Null(bounds.Left);
        Assert.Null(bounds.Top);
        Assert.Equal(WindowBounds.DefaultWidth, bounds.Width);
        Assert.Equal(WindowBounds.DefaultHeight, bounds.Height);
        Assert.False(bounds.Maximized);
    }

    [Fact]
    public void Load_OffScreenOriginWhileMaximized_KeepsRestoreSize()
    {
        WriteBounds("""{"Width":1200,"Height":800,"Left":8000,"Top":9000,"Maximized":true}""");
        var displays = new[] { new DisplayRect(0, 0, 1920, 1080) };

        var bounds = WindowBounds.Load(displays);

        Assert.Null(bounds.Left);
        Assert.Null(bounds.Top);
        Assert.True(bounds.Maximized);
        Assert.Equal(1200, bounds.Width);
        Assert.Equal(800, bounds.Height);
    }

    [Fact]
    public void Load_OriginOnDisplay_KeepsLocation()
    {
        WriteBounds("""{"Width":1200,"Height":800,"Left":100,"Top":80,"Maximized":false}""");
        var displays = new[] { new DisplayRect(0, 0, 1920, 1080) };

        var bounds = WindowBounds.Load(displays);

        Assert.Equal(100, bounds.Left);
        Assert.Equal(80, bounds.Top);
        Assert.Equal(1200, bounds.Width);
        Assert.Equal(800, bounds.Height);
    }

    [Fact]
    public void ApplyMinimized_DoesNotChangePersistedState()
    {
        var bounds = new WindowBounds
        {
            Width = 1200,
            Height = 800,
            Left = 30,
            Top = 40,
            Maximized = false,
        };

        bounds.ApplyMinimized();
        bounds.Save();

        var json = File.ReadAllText(ExpectedPath());
        Assert.DoesNotContain("Minimized", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1200, bounds.Width);
        Assert.Equal(800, bounds.Height);
        Assert.Equal(30, bounds.Left);
        Assert.Equal(40, bounds.Top);
        Assert.False(bounds.Maximized);
    }

    [Fact]
    public void Save_WritesJsonNextToBaseDirectory()
    {
        var bounds = new WindowBounds
        {
            Width = 1280,
            Height = 720,
            Left = 16,
            Top = 24,
            Maximized = false,
        };

        bounds.Save();

        Assert.True(File.Exists(ExpectedPath()));
        Assert.False(File.Exists(Path.Combine(_workspace.CwdPath, "Window.json")));

        var loaded = WindowBounds.Load();
        Assert.Equal(1280, loaded.Width);
        Assert.Equal(720, loaded.Height);
        Assert.Equal(16, loaded.Left);
        Assert.Equal(24, loaded.Top);
        Assert.False(loaded.Maximized);
    }

    [Fact]
    public void ApplyRestored_ClearsMaximizedAndStoresGeometry()
    {
        var bounds = new WindowBounds
        {
            Width = 1440,
            Height = 900,
            Maximized = true,
        };

        bounds.ApplyRestored(1250, 810, 40, 50);

        Assert.False(bounds.Maximized);
        Assert.Equal(1250, bounds.Width);
        Assert.Equal(810, bounds.Height);
        Assert.Equal(40, bounds.Left);
        Assert.Equal(50, bounds.Top);
    }

    private void WriteBounds(string json) =>
        File.WriteAllText(ExpectedPath(), json);

    private string ExpectedPath() =>
        Path.GetFullPath(Path.Combine(_workspace.DirectoryPath, "Window.json"));
}
