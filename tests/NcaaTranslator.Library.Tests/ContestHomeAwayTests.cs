using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class ContestHomeAwayTests
{
    [Fact]
    public void HomeAndAway_UseIsHome_NotListOrder()
    {
        var contest = new Contest
        {
            teams = new List<ContestTeam>
            {
                new ContestTeam { isHome = false, customName = "Away First", score = 10 },
                new ContestTeam { isHome = true, customName = "Home Second", score = 21 }
            }
        };

        Assert.Equal("Home Second", contest.HomeCustomName);
        Assert.Equal("Away First", contest.AwayCustomName);
        Assert.Equal(21, contest.HomeScore);
        Assert.Equal(10, contest.AwayScore);
        Assert.Same(contest.teams[1], contest.HomeTeam);
        Assert.Same(contest.teams[0], contest.AwayTeam);
    }

    [Fact]
    public void HomeAndAway_HomeFirst_StillResolvesByIsHome()
    {
        var contest = TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc",
            homeScore: 17, awayScore: 14);
        contest.teams[0].customName = "UND";
        contest.teams[1].customName = "South Dakota";

        Assert.Equal("UND", contest.HomeCustomName);
        Assert.Equal("South Dakota", contest.AwayCustomName);
        Assert.Equal(17, contest.HomeScore);
        Assert.Equal(14, contest.AwayScore);
    }

    [Fact]
    public void HomeAndAway_MissingTeam_ReturnsNull()
    {
        var contest = new Contest
        {
            teams = new List<ContestTeam>
            {
                new ContestTeam { isHome = true, customName = "Only Home", score = 3 }
            }
        };

        Assert.Equal("Only Home", contest.HomeCustomName);
        Assert.Equal(3, contest.HomeScore);
        Assert.Null(contest.AwayTeam);
        Assert.Null(contest.AwayCustomName);
        Assert.Null(contest.AwayScore);
    }

    [Fact]
    public void HomeAndAway_EmptyTeams_ReturnsNull()
    {
        var contest = new Contest();

        Assert.Null(contest.HomeTeam);
        Assert.Null(contest.AwayTeam);
        Assert.Null(contest.HomeCustomName);
        Assert.Null(contest.AwayCustomName);
        Assert.Null(contest.HomeScore);
        Assert.Null(contest.AwayScore);
    }
}
