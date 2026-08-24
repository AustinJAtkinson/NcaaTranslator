using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class XmlToJsonTests : IDisposable
{
    private readonly TempWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void ConvertXmlToJson_WritesJsonExtension_NotXmlJson()
    {
        var xmlPath = Path.Combine(_workspace.DirectoryPath, "1.xml");
        File.WriteAllText(xmlPath, "<root><a>1</a></root>");

        NcaaProcessor.ConvertXmlToJson(new XmlToJson
        {
            Enabled = true,
            FilePaths = new List<FilePath> { new() { Path = xmlPath } }
        });

        var jsonPath = Path.Combine(_workspace.DirectoryPath, "1.json");
        Assert.True(File.Exists(jsonPath));
        Assert.False(File.Exists(xmlPath + ".json"));
        Assert.Contains("\"a\"", File.ReadAllText(jsonPath));
    }

    [Fact]
    public void ConvertXmlToJson_Disabled_DoesNotWrite()
    {
        var xmlPath = Path.Combine(_workspace.DirectoryPath, "1.xml");
        File.WriteAllText(xmlPath, "<root><a>1</a></root>");

        NcaaProcessor.ConvertXmlToJson(new XmlToJson
        {
            Enabled = false,
            FilePaths = new List<FilePath> { new() { Path = xmlPath } }
        });

        Assert.False(File.Exists(Path.Combine(_workspace.DirectoryPath, "1.json")));
    }

    [Fact]
    public void ConvertXmlToJson_MultipleFilePaths_WritesEachJson()
    {
        var xmlPath1 = Path.Combine(_workspace.DirectoryPath, "alpha.xml");
        var xmlPath2 = Path.Combine(_workspace.DirectoryPath, "beta.xml");
        File.WriteAllText(xmlPath1, "<root><a>1</a></root>");
        File.WriteAllText(xmlPath2, "<root><b>2</b></root>");

        NcaaProcessor.ConvertXmlToJson(new XmlToJson
        {
            Enabled = true,
            FilePaths = new List<FilePath>
            {
                new() { Path = xmlPath1 },
                new() { Path = xmlPath2 }
            }
        });

        var jsonPath1 = Path.Combine(_workspace.DirectoryPath, "alpha.json");
        var jsonPath2 = Path.Combine(_workspace.DirectoryPath, "beta.json");
        Assert.True(File.Exists(jsonPath1));
        Assert.True(File.Exists(jsonPath2));
        Assert.False(File.Exists(xmlPath1 + ".json"));
        Assert.False(File.Exists(xmlPath2 + ".json"));
        Assert.Contains("\"a\"", File.ReadAllText(jsonPath1));
        Assert.Contains("\"b\"", File.ReadAllText(jsonPath2));
    }
}
