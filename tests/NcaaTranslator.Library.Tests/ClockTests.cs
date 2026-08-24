using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class ClockTests
{
    [Fact]
    public void DisplayClock_Pregame_UsesLocalStartTime()
    {
        const long epoch = 1700000000;
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
    public void DisplayClock_Final_UsesFinalMessage()
    {
        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "FINAL"
        };

        Assert.Equal("FINAL", contest.displayClock);
        Assert.Equal("FINAL", contest.displayClockDefault);
    }

    [Fact]
    public void DisplayClock_Final_Replaces2OTInDisplayClock()
    {
        var contest = new Contest
        {
            gameState = "F",
            finalMessage = "2OT"
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
            finalMessage = null
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
