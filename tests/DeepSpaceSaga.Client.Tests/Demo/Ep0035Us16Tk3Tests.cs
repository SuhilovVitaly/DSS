using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.Demo;

[Collection("InterfaceLog")]
public sealed class Ep0035Us16Tk3Tests
{
    [Fact]
    public async Task RecoveredFlowAdoptsMatchingStateWithoutStartingAnotherBattle()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var state = ((TacticalBattleScreen)fixture.Screens.Current).Flow.State!;
        var flow = new TacticalBattleFlow(fixture.Handle)
        { CampaignEncounter = new(new(state.BattleId, "encounter:g", "g"), WorldEncounterPhases.PendingBattle, false) };
        Assert.False(flow.AdoptRecoveredEncounter(state with { BattleId = "wrong" }));
        Assert.Null(flow.State);
        long sequence = fixture.Engine.CaptureSnapshot().DemoCampaign!.CommitSequence;
        Assert.True(await flow.StartEncounterAsync());
        Assert.Equal(state.BattleId, flow.State!.BattleId);
        Assert.Equal(state.Revision, flow.State.Revision);
        Assert.Equal(sequence, fixture.Engine.CaptureSnapshot().DemoCampaign!.CommitSequence);
        Assert.Null(flow.LastStart);
        Assert.False(await flow.ExitAsync());
    }
}
