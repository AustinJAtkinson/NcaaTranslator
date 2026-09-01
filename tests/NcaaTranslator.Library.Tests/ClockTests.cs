using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class ClockTests
{
    private static long TodayEpoch(double hours = 19)
    {
        var now = DateTimeOffset.Now;
        return now.Subtract(now.TimeOfDay).AddHours(hours).ToUnixTimeSeconds();
    }

    private static long DaysFromTodayEpoch(int days, double hours = 17)
    {
        var now = DateTimeOffset.Now;
        return now.Subtract(now.TimeOfDay).AddDays(days).AddHours(hours).ToUnixTimeSeconds();
    }

    private static string AbbrevDay(long epoch)
    {
        return DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime().ToString("ddd").TrimEnd('.');
    }

    private static string FullDay(long epoch)
    {
        return DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime().ToString("dddd");
    }

    private static IDisposable UseClockFormats(ClockFormats formats)
    {
        var previous = Settings.SettingsList;
        Settings.SettingsList = new Setting { ClockFormats = formats };
        return new RestoreSettings(previous);
    }

    private sealed class RestoreSettings : IDisposable
    {
        private readonly Setting? _previous;

        public RestoreSettings(Setting? previous) => _previous = previous;

        public void Dispose() => Settings.SettingsList = _previous;
    }

    [Fact]
    public void DisplayClock_Pregame_UsesLocalStartTime()
    {
        var now = DateTimeOffset.Now;
        var start = now.Subtract(now.TimeOfDay).AddHours(19);
        var epoch = start.ToUnixTimeSeconds();
        var contest = new Contest
        {
            gameState = "P",
            startTimeEpoch = epoch,
            startTime = "7:00 PM ET",
            tba = false
        };

        var expected = DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime().ToString("h:mm tt");
        Assert.Equal(expected, contest.displayClock);
        Assert.Equal(expected, contest.displayClockDefault);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime().ToString("HH:mm"), contest.ctStateTime24h);
    }

    [Fact]
    public void DisplayClock_Pregame_IncludesWeekdayWhenNotToday()
    {
        var now = DateTimeOffset.Now;
        var start = now.Subtract(now.TimeOfDay).AddDays(2).AddHours(17);
        var epoch = start.ToUnixTimeSeconds();
        var contest = new Contest
        {
            gameState = "P",
            startTimeEpoch = epoch,
            startTime = "5:00 PM ET",
            tba = false
        };

        var local = DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime();
        var day = local.ToString("ddd").TrimEnd('.');
        var expected = $"{day}. {local.ToString("h:mm tt")}";
        Assert.Equal(expected, contest.displayClock);
        Assert.Equal(expected, contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_Final_UsesFinalMessage()
    {
        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "FINAL",
            startTimeEpoch = TodayEpoch()
        };

        Assert.Equal("FINAL", contest.displayClock);
        Assert.Equal("FINAL", contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_Final_IncludesWeekdayWhenNotToday()
    {
        var epoch = DaysFromTodayEpoch(-1);
        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "FINAL",
            startTimeEpoch = epoch
        };

        var expected = $"FINAL - {AbbrevDay(epoch)}";
        Assert.Equal(expected, contest.displayClock);
        Assert.Equal(expected, contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_Final_Replaces2OTThenAppendsWeekdayWhenNotToday()
    {
        var epoch = DaysFromTodayEpoch(-2);
        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "2OT",
            startTimeEpoch = epoch
        };

        var day = AbbrevDay(epoch);
        Assert.Equal($"SO - {day}", contest.displayClock);
        Assert.Equal($"2OT - {day}", contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_Final_OmitsWeekdayWhenIncludeIsOff()
    {
        using var workspace = new TempWorkspace();
        using var _ = UseClockFormats(new ClockFormats
        {
            Final = new ClockFormat { IncludeWeekday = false }
        });

        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "FINAL",
            startTimeEpoch = DaysFromTodayEpoch(-1)
        };

        Assert.Equal("FINAL", contest.displayClock);
        Assert.Equal("FINAL", contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_Final_UsesFullWeekdayAndCustomPattern()
    {
        using var workspace = new TempWorkspace();
        using var _ = UseClockFormats(new ClockFormats
        {
            Final = new ClockFormat
            {
                IncludeWeekday = true,
                FullWeekday = true,
                Separator = " | ",
                Pattern = "{dayofweek}{separator}{text}"
            }
        });

        var epoch = DaysFromTodayEpoch(-1);
        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "FINAL",
            startTimeEpoch = epoch
        };

        var expected = $"{FullDay(epoch)} | FINAL";
        Assert.Equal(expected, contest.displayClock);
        Assert.Equal(expected, contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_Final_EmptyPattern_ReturnsTextOnly()
    {
        using var workspace = new TempWorkspace();
        using var _ = UseClockFormats(new ClockFormats
        {
            Final = new ClockFormat
            {
                IncludeWeekday = true,
                Pattern = ""
            }
        });

        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "FINAL",
            startTimeEpoch = DaysFromTodayEpoch(-1)
        };

        Assert.Equal("FINAL", contest.displayClock);
    }

    [Fact]
    public void DisplayClock_Final_Replaces2OTInDisplayClock()
    {
        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "2OT",
            startTimeEpoch = TodayEpoch()
        };

        Assert.Equal("SO", contest.displayClock);
        Assert.Equal("2OT", contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_NullFinalMessage_DoesNotThrow()
    {
        var contest = new Contest
        {
            gameState = "F",
            finalMessage = null,
            startTimeEpoch = DaysFromTodayEpoch(-1)
        };

        var clock = Record.Exception(() => _ = contest.displayClock);
        var clockDefault = Record.Exception(() => _ = contest.displayClockDefault);

        Assert.Null(clock);
        Assert.Null(clockDefault);
        Assert.Equal("", contest.displayClock);
        Assert.Equal("", contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_InProgress_UsesPeriodAndClock()
    {
        var contest = new Contest
        {
            gameState = "I",
            currentPeriod = "Q2",
            contestClock = "05:00"
        };

        Assert.Equal("Q2     05:00", contest.displayClock);
        Assert.Equal("Q2     05:00", contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_InProgress_NullPeriod_DoesNotThrow()
    {
        var contest = new Contest
        {
            gameState = "I",
            currentPeriod = null,
            contestClock = null
        };

        var ex = Record.Exception(() => _ = contest.displayClock);
        Assert.Null(ex);
        Assert.Equal("     ", contest.displayClock);
    }

    [Fact]
    public void CtStateTime_Tba_ReturnsStartTime()
    {
        var contest = new Contest
        {
            gameState = "P",
            tba = true,
            startTime = "TBA"
        };

        Assert.Equal("TBA", contest.ctStateTime);
        Assert.Equal("TBA", contest.displayClock);
    }
}
