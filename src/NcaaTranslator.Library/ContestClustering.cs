using System.Globalization;

namespace NcaaTranslator.Library
{
    internal static class ContestClustering
    {
        internal const int ClusterGapDays = 3;

        internal static DateTime GetLocalDate(Contest contest)
        {
            if (!string.IsNullOrWhiteSpace(contest.startDate) &&
                DateTime.TryParseExact(
                    contest.startDate.Trim(),
                    "MM/dd/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                return parsed.Date;
            }

            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(contest.startTimeEpoch).ToLocalTime().Date;
            }
            catch
            {
                return DateTime.MinValue.Date;
            }
        }

        internal static List<List<Contest>> ClusterContests(IEnumerable<Contest>? contests)
        {
            var list = contests?.ToList() ?? new List<Contest>();
            if (list.Count == 0)
                return new List<List<Contest>>();

            var byDate = list
                .GroupBy(GetLocalDate)
                .OrderBy(g => g.Key)
                .ToList();

            var clusters = new List<List<Contest>>();
            List<Contest>? current = null;
            DateTime previousDate = default;

            foreach (var group in byDate)
            {
                var date = group.Key;
                if (current == null || (date - previousDate).Days >= ClusterGapDays)
                {
                    current = new List<Contest>();
                    clusters.Add(current);
                }

                current.AddRange(group);
                previousDate = date;
            }

            foreach (var cluster in clusters)
                cluster.Sort((a, b) => a.startTimeEpoch.CompareTo(b.startTimeEpoch));

            return clusters;
        }

        internal static int PickCurrentClusterIndex(IReadOnlyList<IReadOnlyList<Contest>> clusters, DateTime asOf)
        {
            if (clusters.Count == 0)
                return -1;

            var today = asOf.Date;
            for (var i = 0; i < clusters.Count; i++)
            {
                if (clusters[i].Select(GetLocalDate).Any(d => d == today))
                    return i;
            }

            for (var i = 0; i < clusters.Count; i++)
            {
                if (clusters[i].Count == 0)
                    continue;
                var first = clusters[i].Min(GetLocalDate);
                if (first > today)
                    return i;
            }

            return clusters.Count - 1;
        }

        internal static List<Contest> PickCurrentCluster(IReadOnlyList<IReadOnlyList<Contest>> clusters, DateTime asOf)
        {
            var index = PickCurrentClusterIndex(clusters, asOf);
            if (index < 0)
                return new List<Contest>();
            return clusters[index].ToList();
        }

        internal static bool LastClusterFullyInPast(IReadOnlyList<IReadOnlyList<Contest>> clusters, DateTime asOf)
        {
            if (clusters.Count == 0)
                return false;

            var last = clusters[clusters.Count - 1];
            if (last.Count == 0)
                return false;

            var today = asOf.Date;
            if (last.Max(GetLocalDate) >= today)
                return false;

            return last.All(c => c.gameState != "I");
        }

        internal static bool ShouldAutoIncrementWeek(
            IReadOnlyList<IReadOnlyList<Contest>> clusters,
            IEnumerable<Contest>? contests,
            DateTime asOf)
        {
            if (!LastClusterFullyInPast(clusters, asOf))
                return false;

            return contests == null || contests.All(c => c.gameState != "I");
        }

        internal static string? FormatDateRange(IEnumerable<Contest>? contests)
        {
            var dates = (contests ?? Enumerable.Empty<Contest>())
                .Select(GetLocalDate)
                .Where(d => d > DateTime.MinValue)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            return FormatDateRange(dates);
        }

        internal static string? FormatDateRange(IReadOnlyList<DateTime> dates)
        {
            if (dates == null || dates.Count == 0)
                return null;

            var inv = CultureInfo.InvariantCulture;
            var first = dates[0].Date;
            var last = dates[dates.Count - 1].Date;
            if (first == last)
                return first.ToString("MMM d", inv);

            if (first.Year == last.Year && first.Month == last.Month)
                return first.ToString("MMM d", inv) + "\u2013" + last.ToString("%d", inv);

            return first.ToString("MMM d", inv) + "\u2013" + last.ToString("MMM d", inv);
        }

        internal static string FormatSingleDate(DateTime date) =>
            date.ToString("MMM d", CultureInfo.InvariantCulture);
    }
}
