using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Demo;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class Ep0035Us5Tk3Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void BothChoicesShowAuthoritativeFuelRiskAndDeadlineBeforeDeparture(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var routes = new ThreatRoutesSnapshot(99,
            [new("direct:s", "s", false, [new(1, 2)], 3600000, 10, "exposed", 4000, 60000),
             new("detour:s", "s", true, [new(2, 3), new(3, 4), new(1, 2)], 7200000, 25, "lower", 0, -3600000)]);
        var rows = ThreatRoutesPresentation.Rows(routes, 0);
        Assert.Equal(2, rows.Length);
        Assert.NotEqual(rows[0].RouteId, rows[1].RouteId);
        Assert.Contains("10", rows[0].SecondaryText);
        Assert.Contains("25", rows[1].SecondaryText);
        Assert.Contains(Localization.Get("Demo.Route.Exposed"), rows[0].SecondaryText);
        Assert.Contains(Localization.Get("Demo.Route.Lower"), rows[1].SecondaryText);
        Assert.Contains(string.Format(Localization.Get("Demo.Route.Late"), "01:00"), rows[1].ReasonText);
        var blocked = ThreatRoutesPresentation.Rows(routes with
        {
            Options = [routes.Options[0] with
            { FuelRequiredKg = null, UnavailableReason = CommandReasonCodes.InsufficientVoyageFuel }]
        }, 0)[0];
        Assert.False(blocked.IsEnabled);
        Assert.Contains("?", blocked.SecondaryText);
        Assert.Contains(Localization.Get("Demo.Route.Fuel"), blocked.ReasonText);
    }
}
