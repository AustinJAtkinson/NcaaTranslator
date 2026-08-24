using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class NameConverterTests : IDisposable
{
    private readonly TempWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void LookupTeam_ExistingTeam_ReturnsCustomName()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);

        var result = NameConverters.LookupTeam(new Names
        {
            name6Char = "NO DAK",
            nameShort = "North Dakota",
            seoname = "north-dakota"
        });

        Assert.Equal("UND", result);
    }

    [Fact]
    public void LookupTeam_NewlyAddedTeam_ReturnsAssignedCustomName()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);

        var result = NameConverters.LookupTeam(new Names
        {
            name6Char = "NEWTM",
            nameShort = "New Team",
            seoname = "new-team"
        });

        Assert.Equal("New Team", result);
        Assert.Equal("New Team", NameConverters.LookupTeam(new Names { name6Char = "NEWTM" }));
        Assert.Contains(NameConverters.GetTeams(), t => t.name6Char == "NEWTM" && t.customName == "New Team");
    }

    [Fact]
    public void LookupConf_ExistingConference_ReturnsCustomName()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);

        var result = NameConverters.LookupConf(new Conference { conferenceSeo = "mvc" });

        Assert.Equal("MVFC", result);
    }

    [Fact]
    public void LookupConf_NewlyAddedConference_ReturnsAssignedCustomName()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);

        var result = NameConverters.LookupConf(new Conference { conferenceSeo = "big10" });

        Assert.Equal("big10", result);
        Assert.Equal("big10", NameConverters.LookupConf(new Conference { conferenceSeo = "big10" }));
        Assert.Contains(NameConverters.GetConferences(), c => c.conferenceSeo == "big10" && c.customConferenceName == "big10");
    }

    [Fact]
    public void Load_DuplicateKeys_LastWinsWithoutThrowing()
    {
        var path = Path.Combine(_workspace.DirectoryPath, "dup.json");
        File.WriteAllText(path, """
        {
          "teams": [
            { "seoname": "first", "nameShort": "First", "name6Char": "DUP", "customName": "First" },
            { "seoname": "second", "nameShort": "Second", "name6Char": "DUP", "customName": "Second" }
          ],
          "conferences": [
            { "customConferenceName": "Old", "conferenceSeo": "dupc" },
            { "customConferenceName": "New", "conferenceSeo": "dupc" }
          ]
        }
        """);

        NameConverters.Load(path);

        Assert.Equal("Second", NameConverters.LookupTeam(new Names { name6Char = "DUP" }));
        Assert.Equal("New", NameConverters.LookupConf(new Conference { conferenceSeo = "dupc" }));
    }

    [Fact]
    public void Reload_PersistsSortedTeams()
    {
        TestHelpers.WriteDefaultNames(_workspace.DirectoryPath);
        NameConverters.LookupTeam(new Names { name6Char = "AAA", nameShort = "Aaa", seoname = "aaa" });

        var teams = NameConverters.GetTeams();
        Assert.Equal("AAA", teams.First().name6Char);
    }
}
