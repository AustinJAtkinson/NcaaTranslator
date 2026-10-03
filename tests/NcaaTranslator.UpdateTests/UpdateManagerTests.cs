using NcaaTranslator.Library;
using Xunit;

namespace NcaaTranslator.UpdateTests;

public class UpdateManagerTests
{
    [Fact]
    public void ShouldUpdate_WhenLatestIsNewer_ReturnsTrue()
    {
        Assert.True(UpdateManager.ShouldUpdate(new Version(0, 1, 6), new Version(0, 1, 7)));
    }

    [Fact]
    public void ShouldUpdate_WhenLatestEqualsCurrent_ReturnsFalse()
    {
        Assert.False(UpdateManager.ShouldUpdate(new Version(0, 1, 7), new Version(0, 1, 7)));
    }

    [Fact]
    public void ShouldUpdate_WhenLatestIsOlder_ReturnsFalse()
    {
        Assert.False(UpdateManager.ShouldUpdate(new Version(0, 1, 7), new Version(0, 1, 6)));
    }

    [Fact]
    public void ResolveInstalledExe_PrefersDesktopWhenWpfIsMissing()
    {
        var dir = CreateDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "NcaaTranslator.Desktop.exe"), "desktop");

            Assert.Equal(
                Path.Combine(dir, "NcaaTranslator.Desktop.exe"),
                UpdateManager.ResolveInstalledExe(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ResolveInstalledExe_UsesWpfWhenDesktopIsMissing()
    {
        var dir = CreateDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "NcaaTranslator.Wpf.exe"), "wpf");

            Assert.Equal(
                Path.Combine(dir, "NcaaTranslator.Wpf.exe"),
                UpdateManager.ResolveInstalledExe(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ResolveInstalledExe_PrefersDesktopWhenBothExist()
    {
        var dir = CreateDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "NcaaTranslator.Desktop.exe"), "desktop");
            File.WriteAllText(Path.Combine(dir, "NcaaTranslator.Wpf.exe"), "wpf");

            Assert.Equal(
                Path.Combine(dir, "NcaaTranslator.Desktop.exe"),
                UpdateManager.ResolveInstalledExe(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string CreateDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ncaa-wpf-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
