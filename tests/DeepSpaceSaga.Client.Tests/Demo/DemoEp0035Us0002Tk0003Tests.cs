using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Demo;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0035Us0002Tk0003Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void MapMarkerShowsObservedTelemetryOnly(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var pursuit = new PursuitSnapshot("g", "g:1", PursuitStatuses.Escaped, 1200, -20, 123, "start_safe");
        var group = new PirateGroupSnapshot("g", 1, 2, 0, 0, 123, true, PirateGroupStatuses.Observed, Pursuit: pursuit);
        var marker = Assert.Single(PirateGroupsPresentation.Markers(new() { Groups = [group] }));
        Assert.Contains("120", marker.Detail);
        Assert.Contains(Localization.Get("Demo.Pursuit.Safe"), marker.Detail);
        Assert.Contains(Localization.Get("Demo.Pursuit.Escaped"), marker.Detail);
        var debug = Assert.Single(PirateGroupsPresentation.Markers(new() { Groups = [group with { IsDetected = false }] }));
        Assert.Empty(debug.Detail);
        Assert.Empty(MapEvasionPresentation.Describe(null));
    }
}
