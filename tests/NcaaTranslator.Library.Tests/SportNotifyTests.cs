using NcaaTranslator.Library;

namespace NcaaTranslator.Library.Tests;

public class SportNotifyTests
{
    [Fact]
    public void GridEditedProperties_RaisePropertyChanged()
    {
        var sport = TestHelpers.CreateSport();
        var names = new List<string>();
        sport.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                names.Add(e.PropertyName);
        };

        sport.SportName = "Football FBS";
        sport.SportShortName = "FBS";
        sport.SportCode = "MBB";
        sport.ConferenceName = "ACC";
        sport.Division = 11;
        sport.Week = 3;
        sport.SeasonYear = 2026;

        Assert.Equal(new[]
        {
            nameof(Sport.SportName),
            nameof(Sport.SportShortName),
            nameof(Sport.SportCode),
            nameof(Sport.ConferenceName),
            nameof(Sport.Division),
            nameof(Sport.Week),
            nameof(Sport.SeasonYear)
        }, names);
    }

    [Fact]
    public void SameValue_DoesNotRaisePropertyChanged()
    {
        var sport = TestHelpers.CreateSport();
        sport.Division = 12;
        var raised = false;
        sport.PropertyChanged += (_, _) => raised = true;

        sport.Division = 12;
        sport.Week = 2;
        sport.ConferenceName = "MVFC";

        Assert.False(raised);
    }
}
