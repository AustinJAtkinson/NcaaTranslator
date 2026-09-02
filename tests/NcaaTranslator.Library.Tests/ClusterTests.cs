using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class ClusterTests
{
    [Fact]
    public void ClusterContests_FcsWeekendSplit_ProducesTwoClusters()
    {
        var contests = new[]
        {
            Dated(1, "08/27/2026"),
            Dated(2, "08/28/2026"),
            Dated(3, "08/29/2026"),
            Dated(4, "09/03/2026"),
            Dated(5, "09/05/2026"),
            Dated(6, "09/06/2026")
        };

        var clusters = ContestClustering.ClusterContests(contests);

        Assert.Equal(2, clusters.Count);
        Assert.Equal(new[] { 1L, 2L, 3L }, clusters[0].Select(c => c.contestId));
        Assert.Equal(new[] { 4L, 5L, 6L }, clusters[1].Select(c => c.contestId));
        Assert.Equal("Aug 27\u201329", ContestClustering.FormatDateRange(clusters[0]));
        Assert.Equal("Sep 3\u20136", ContestClustering.FormatDateRange(clusters[1]));
    }

    [Fact]
    public void ClusterContests_ThursdayThroughMonday_IsOneCluster()
    {
        var contests = new[]
        {
            Dated(1, "09/03/2026"),
            Dated(2, "09/04/2026"),
            Dated(3, "09/05/2026"),
            Dated(4, "09/06/2026"),
            Dated(5, "09/07/2026")
        };

        var clusters = ContestClustering.ClusterContests(contests);

        Assert.Single(clusters);
        Assert.Equal(5, clusters[0].Count);
    }

    [Fact]
    public void ClusterContests_GapOfTwoDays_DoesNotSplit()
    {
        var contests = new[]
        {
            Dated(1, "09/01/2026"),
            Dated(2, "09/03/2026")
        };

        Assert.Single(ContestClustering.ClusterContests(contests));
    }

    [Fact]
    public void ClusterContests_GapOfThreeDays_Splits()
    {
        var contests = new[]
        {
            Dated(1, "09/01/2026"),
            Dated(2, "09/04/2026")
        };

        Assert.Equal(2, ContestClustering.ClusterContests(contests).Count);
    }

    [Fact]
    public void ClusterContests_FallsBackToEpochWhenStartDateMissing()
    {
        var contest = TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc",
            startTimeEpoch: TestHelpers.EpochOn("08/27/2026"), startDate: "09/01/2026");
        contest.startDate = null;

        var date = ContestClustering.GetLocalDate(contest);
        Assert.Equal(new DateTime(2026, 8, 27), date);
    }

    [Fact]
    public void PickCurrentCluster_TodayInsideCluster()
    {
        var clusters = FcsClusters();
        var asOf = new DateTime(2026, 8, 28);

        var current = ContestClustering.PickCurrentCluster(clusters, asOf);

        Assert.Equal(new[] { 1L, 2L, 3L }, current.Select(c => c.contestId));
    }

    [Fact]
    public void PickCurrentCluster_TodayInHole_UsesUpcoming()
    {
        var clusters = FcsClusters();
        var asOf = new DateTime(2026, 9, 1);

        var current = ContestClustering.PickCurrentCluster(clusters, asOf);

        Assert.Equal(new[] { 4L, 5L, 6L }, current.Select(c => c.contestId));
        Assert.Equal(1, ContestClustering.PickCurrentClusterIndex(clusters, asOf));
    }

    [Fact]
    public void PickCurrentCluster_AllPast_UsesLast()
    {
        var clusters = FcsClusters();
        var asOf = new DateTime(2026, 9, 10);

        var current = ContestClustering.PickCurrentCluster(clusters, asOf);

        Assert.Equal(new[] { 4L, 5L, 6L }, current.Select(c => c.contestId));
    }

    [Fact]
    public void LastClusterFullyInPast_RequiresPastDatesAndNoInProgress()
    {
        var clusters = FcsClusters();
        Assert.False(ContestClustering.LastClusterFullyInPast(clusters, new DateTime(2026, 9, 1)));
        Assert.True(ContestClustering.LastClusterFullyInPast(clusters, new DateTime(2026, 9, 7)));

        clusters[1][0].gameState = "I";
        Assert.False(ContestClustering.LastClusterFullyInPast(clusters, new DateTime(2026, 9, 7)));
    }

    [Fact]
    public void ShouldAutoIncrementWeek_BlocksWhenLeftoverClusterIsInProgress()
    {
        var clusters = FcsClusters();
        var asOf = new DateTime(2026, 9, 7);
        Assert.True(ContestClustering.LastClusterFullyInPast(clusters, asOf));

        clusters[0][0].gameState = "I";
        var contests = clusters.SelectMany(c => c).ToList();
        Assert.True(ContestClustering.LastClusterFullyInPast(clusters, asOf));
        Assert.False(ContestClustering.ShouldAutoIncrementWeek(clusters, contests, asOf));
    }

    [Fact]
    public void FormatDateRange_SingleDay()
    {
        Assert.Equal("Sep 1", ContestClustering.FormatDateRange(new[] { Dated(1, "09/01/2026") }));
    }

    private static List<List<Contest>> FcsClusters() =>
        ContestClustering.ClusterContests(new[]
        {
            Dated(1, "08/27/2026"),
            Dated(2, "08/28/2026"),
            Dated(3, "08/29/2026"),
            Dated(4, "09/03/2026"),
            Dated(5, "09/05/2026"),
            Dated(6, "09/06/2026")
        });

    private static Contest Dated(long id, string startDate) =>
        TestHelpers.CreateDatedContest(id, startDate);
}
