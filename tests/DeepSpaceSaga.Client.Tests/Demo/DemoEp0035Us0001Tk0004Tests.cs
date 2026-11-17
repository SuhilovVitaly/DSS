using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0035Us0001Tk0004Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void Squad_IsOneObservedMarkerWithApproximateTotal(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var group = new PirateGroupSnapshot("g", 12, 34, 0, 0, 10, true, PirateGroupStatuses.Observed, 11,
            [new("g", "lair", "Demo.Pirate.Lair", 10), new("g", "lair", "Demo.Pirate.Lair", 10)]);
        var marker = Assert.Single(PirateGroupsPresentation.Markers(new() { Groups = [group] }));
        Assert.Equal((12d, 34d), (marker.X, marker.Y));
        Assert.Contains(string.Format(Localization.Get("Demo.Pirate.ApproximateDrones"), 11), marker.Label);
        Assert.Contains(Localization.Get("Demo.Pirate.Lair"), marker.Label);
        var debug = Assert.Single(PirateGroupsPresentation.Markers(new() { Groups = [group with { IsDetected = false, Status = PirateGroupStatuses.DebugVisible }] }));
        Assert.DoesNotContain("11", debug.Label);
        Assert.DoesNotContain(Localization.Get("Demo.Pirate.Lair"), debug.Label);
        Assert.Empty(PirateGroupsPresentation.Markers(null));
        Assert.Empty(PirateGroupsPresentation.Markers(new()));
    }
}
