using System.Reflection;
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
}
