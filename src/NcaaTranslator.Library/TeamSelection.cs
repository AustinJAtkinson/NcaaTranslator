namespace NcaaTranslator.Library
{
    public sealed class TeamOption
    {
        public string Display { get; init; } = "";
        public string Value { get; init; } = "";
        public string? NameShort { get; init; }
    }

    public static class TeamSelection
    {
        public static List<TeamOption> CreateOptions(IEnumerable<Team> teams)
        {
            return teams
                .Where(t => !string.IsNullOrEmpty(t.name6Char))
                .Select(t => new TeamOption
                {
                    Display = t.customName ?? t.nameShort ?? t.name6Char!,
                    Value = t.name6Char!,
                    NameShort = t.nameShort
                })
                .OrderBy(t => t.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string? ResolveName6Char(string? selectedValue, string? text, IEnumerable<TeamOption> options)
        {
            var list = options as IReadOnlyList<TeamOption> ?? options.ToList();

            if (TryMatchValue(selectedValue, list, out var fromSelected))
                return fromSelected;

            if (TryMatchAny(text, list, out var fromText))
                return fromText;

            return string.IsNullOrWhiteSpace(selectedValue) ? null : selectedValue.Trim();
        }

        private static bool TryMatchValue(string? value, IReadOnlyList<TeamOption> options, out string? name6Char)
        {
            name6Char = null;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            foreach (var option in options)
            {
                if (string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    name6Char = option.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool TryMatchAny(string? text, IReadOnlyList<TeamOption> options, out string? name6Char)
        {
            name6Char = null;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            foreach (var option in options)
            {
                if (string.Equals(option.Value, text, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(option.Display, text, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(option.NameShort, text, StringComparison.OrdinalIgnoreCase))
                {
                    name6Char = option.Value;
                    return true;
                }
            }

            return false;
        }
    }
}
