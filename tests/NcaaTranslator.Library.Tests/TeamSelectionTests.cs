using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class TeamSelectionTests
{
    private static List<TeamOption> Options()
    {
        return TeamSelection.CreateOptions(new[]
        {
            new Team { name6Char = "NO DAK", nameShort = "North Dakota", customName = "UND" },
            new Team { name6Char = "S DAK", nameShort = "South Dakota", customName = "South Dakota" },
            new Team { name6Char = null, nameShort = "No Code", customName = "Skip Me" },
            new Team { name6Char = "", nameShort = "Empty", customName = "Also Skip" }
        });
    }

    [Fact]
    public void CreateOptions_UsesName6CharAsValue()
    {
        var options = Options();

        Assert.Equal(2, options.Count);
        Assert.All(options, o => Assert.False(string.IsNullOrEmpty(o.Value)));
        Assert.Contains(options, o => o.Value == "NO DAK" && o.Display == "UND" && o.NameShort == "North Dakota");
        Assert.DoesNotContain(options, o => o.Value == "North Dakota" || o.Display == "Skip Me");
    }

    [Fact]
    public void ResolveName6Char_PrefersSelectedValueOverDisplayText()
    {
        var options = Options();

        var result = TeamSelection.ResolveName6Char("NO DAK", "UND", options);

        Assert.Equal("NO DAK", result);
    }

    [Fact]
    public void ResolveName6Char_DisplayText_ResolvesToName6Char()
    {
        var options = Options();

        var result = TeamSelection.ResolveName6Char(null, "UND", options);

        Assert.Equal("NO DAK", result);
    }

    [Fact]
    public void ResolveName6Char_NameShort_ResolvesToName6Char()
    {
        var options = Options();

        var result = TeamSelection.ResolveName6Char(null, "North Dakota", options);

        Assert.Equal("NO DAK", result);
    }

    [Fact]
    public void ResolveName6Char_TypedCode_ResolvesToName6Char()
    {
        var options = Options();

        var result = TeamSelection.ResolveName6Char(null, "S DAK", options);

        Assert.Equal("S DAK", result);
    }

    [Fact]
    public void ResolveName6Char_UnknownDisplayText_DoesNotPersistText()
    {
        var options = Options();

        var result = TeamSelection.ResolveName6Char(null, "UND Fighting Hawks", options);

        Assert.Null(result);
    }
}
