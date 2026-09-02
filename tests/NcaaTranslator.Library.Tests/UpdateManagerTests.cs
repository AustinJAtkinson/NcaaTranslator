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
}
