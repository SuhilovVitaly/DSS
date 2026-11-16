using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0032Us0002Tk0003Tests
{
    [Fact]
    public void MemorialDisplaysCapturedPilotAndReopenDoesNotMutate()
    {
        using var locale = Localization.UseLocaleForTests("English");
        var pilot = new CrewMemberSnapshot("crew", "Anna", CrewGender.Female, Accuracy: 7);
        var entries = Enumerable.Range(0, 8).Select(i => new PilotMemorialEntry(pilot with { Id = "crew" + i }, "battle", 100)).ToImmutableArray();
        var state = new DemoCampaignSnapshot(1, 1, new(PilotLoss: new(entries, ["captain"])));
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null, campaign: () => state);
        screen.OnActivated();
        screen.OnMouseWheel(50, 600, -1);
        Assert.Equal(1, screen.MemorialScroll);
        Assert.Contains("Anna", PilotLossPresentation.Entry(screen.Memorial[0]));
        Assert.Contains("7", PilotLossPresentation.Entry(screen.Memorial[0]));
        screen.OnDeactivated();
        screen.OnActivated();
        Assert.Equal(8, screen.Memorial.Length);
        Assert.Equal("captain", Assert.Single(state.Company!.PilotLoss!.LiveCrewIds));
    }
}
