using System.Xml.Serialization;
using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class OosTests : IDisposable
{
    private readonly TempWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private static OosUpdater CreateUpdater(string directory, int outScores = 1, int teamsPer = 2)
    {
        return new OosUpdater
        {
            Enabled = true,
            OosFilePath = directory,
            OosFileName = "OUT_Score_",
            NumberOfOutScores = outScores,
            NumberOfTeamsPer = teamsPer
        };
    }

    private static void WriteTemplate(string path, bool includeAllElements = true)
    {
        var elements = new List<ClsGFXElement>
        {
            new() { GraphicObjName = "G1 - V Team", GraphicObjText = "" },
            new() { GraphicObjName = "G1 - V Score", GraphicObjText = "" },
            new() { GraphicObjName = "G1 - H Team", GraphicObjText = "" },
            new() { GraphicObjName = "G1 - H Score", GraphicObjText = "" },
            new() { GraphicObjName = "G1 - Quarter", GraphicObjText = "" },
            new() { GraphicObjName = "G2 - V Team", GraphicObjText = "" },
            new() { GraphicObjName = "G2 - V Score", GraphicObjText = "" },
            new() { GraphicObjName = "G2 - H Team", GraphicObjText = "" },
            new() { GraphicObjName = "G2 - H Score", GraphicObjText = "" },
            new() { GraphicObjName = "G2 - Time", GraphicObjText = "" },
            new() { GraphicObjName = "G2 - Quarter", GraphicObjText = "" }
        };

        if (includeAllElements)
        {
            elements.Insert(4, new ClsGFXElement { GraphicObjName = "G1 - Time", GraphicObjText = "" });
        }

        var template = new ClsGFXTemplate
        {
            GfxElements = new GfxElements { ClsGFXElement = elements }
        };

        var serializer = new XmlSerializer(typeof(ClsGFXTemplate));
        using var writer = new StreamWriter(path);
        serializer.Serialize(writer, template);
    }

    [Fact]
    public void UpdateOos_NullDisplayGames_DoesNotThrow()
    {
        var updater = CreateUpdater(_workspace.DirectoryPath);
        var scoreboard = new NcaaScoreboard { data = new Data { displayGames = null } };

        var ex = Record.Exception(() => NcaaProcessor.UpdateOos(scoreboard, updater));
        Assert.Null(ex);
    }

    [Fact]
    public void UpdateOos_EmptyDisplayGames_DoesNotThrow()
    {
        var updater = CreateUpdater(_workspace.DirectoryPath);
        var scoreboard = new NcaaScoreboard { data = new Data { displayGames = new List<Contest>() } };

        var ex = Record.Exception(() => NcaaProcessor.UpdateOos(scoreboard, updater));
        Assert.Null(ex);
    }

    [Fact]
    public void UpdateOos_NullData_DoesNotThrow()
    {
        var updater = CreateUpdater(_workspace.DirectoryPath);
        var ex = Record.Exception(() => NcaaProcessor.UpdateOos(new NcaaScoreboard(), updater));
        Assert.Null(ex);
    }

    [Fact]
    public void UpdateOos_FillsAllSlotsThenSerializes()
    {
        var templatePath = Path.Combine(_workspace.DirectoryPath, "OUT_Score_1.tmp");
        WriteTemplate(templatePath);

        var homeGame = TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc", gameState: "I", homeScore: 21, awayScore: 14);
        homeGame.currentPeriod = "Q2";
        homeGame.contestClock = "05:00";
        homeGame.teams[0].customName = "UND";
        homeGame.teams[1].customName = "South Dakota";

        var confGame = TestHelpers.CreateContest(2, "NDSU", "North Dakota St.", "mvc", "SDSU", "South Dakota St.", "mvc", gameState: "F", homeScore: 35, awayScore: 10);
        confGame.finalMessage = "FINAL";
        confGame.teams[0].customName = "NDSU";
        confGame.teams[1].customName = "SDSU";

        var scoreboard = new NcaaScoreboard
        {
            data = new Data
            {
                displayGames = new List<Contest> { homeGame, confGame }
            }
        };

        NcaaProcessor.UpdateOos(scoreboard, CreateUpdater(_workspace.DirectoryPath));

        var serializer = new XmlSerializer(typeof(ClsGFXTemplate));
        using var reader = File.OpenRead(templatePath);
        var result = (ClsGFXTemplate)serializer.Deserialize(reader)!;
        var texts = result.GfxElements!.ClsGFXElement!.ToDictionary(e => e.GraphicObjName!, e => e.GraphicObjText);

        Assert.Equal("South Dakota", texts["G1 - V Team"]);
        Assert.Equal("14", texts["G1 - V Score"]);
        Assert.Equal("UND", texts["G1 - H Team"]);
        Assert.Equal("21", texts["G1 - H Score"]);
        Assert.Equal("05:00", texts["G1 - Time"]);
        Assert.Equal("Q2", texts["G1 - Quarter"]);

        Assert.Equal("SDSU", texts["G2 - V Team"]);
        Assert.Equal("10", texts["G2 - V Score"]);
        Assert.Equal("NDSU", texts["G2 - H Team"]);
        Assert.Equal("35", texts["G2 - H Score"]);
        Assert.Equal("", texts["G2 - Time"]);
        Assert.Equal("FINAL", texts["G2 - Quarter"]);
    }

    [Fact]
    public void UpdateOos_MissingGfxElement_DoesNotThrow()
    {
        var templatePath = Path.Combine(_workspace.DirectoryPath, "OUT_Score_1.tmp");
        WriteTemplate(templatePath, includeAllElements: false);

        var game = TestHelpers.CreateContest(1, "NO DAK", "North Dakota", "mvc", "S DAK", "South Dakota", "mvc", gameState: "I");
        game.currentPeriod = "Q1";
        game.contestClock = "12:00";
        game.teams[0].customName = "UND";
        game.teams[1].customName = "South Dakota";

        var scoreboard = new NcaaScoreboard
        {
            data = new Data { displayGames = new List<Contest> { game } }
        };

        var ex = Record.Exception(() => NcaaProcessor.UpdateOos(scoreboard, CreateUpdater(_workspace.DirectoryPath, teamsPer: 1)));
        Assert.Null(ex);
    }
}
