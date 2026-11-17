using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Demo;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class Ep0035Us4Tk2Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void LastKnownMarkerLabelsAgeAndFrozenPursuitWithoutClaimingCurrentDetection(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var group = new PirateGroupSnapshot("g", 100, 200, 99, 88, 123, false, PirateGroupStatuses.LastKnown, 6,
            [new("g", "lair", "Demo.Pirate.Lair", 123)], [new("carrier:g", "lair")],
            new("g", "g:1", PursuitStatuses.Approaching, 2000, 20, 123), 5000);
        var marker = Assert.Single(PirateGroupsPresentation.Markers(new() { Groups = [group] }));
        Assert.False(marker.Confirmed);
        Assert.Equal(100, marker.X);
        Assert.Equal(200, marker.Y);
        Assert.Contains(Localization.Get("Demo.Pirate.LastKnown"), marker.Label);
        Assert.Contains(Localization.Get("Demo.Pirate.Lair"), marker.Label);
        Assert.Contains("20", marker.Detail);
        Assert.Contains(Localization.Get("Demo.Pursuit.Approaching"), marker.Detail);
        var debug = Assert.Single(PirateGroupsPresentation.Markers(new() { Groups = [group with { Status = PirateGroupStatuses.DebugVisible }] }));
        Assert.Empty(debug.Detail);
        Assert.Contains(Localization.Get("Demo.Pirate.Unknown"), debug.Label);
    }
}
