using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us15Tk4Tests
{
    [Fact]
    public void FinishedSquadHistoryRoundtripsAndCorruptBindingFailsBeforeWorldReplacement()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var session = engine.TacticalBattleSessionForTests!;
        session.Battle.EndWithTimeout(1);
        Assert.True(engine.ExitTacticalBattle(new("finish", session.Id)).Accepted);
        var save = engine.CaptureSaveState();
        using var loaded = DemoEp0033Us0001Tk0003Tests.Demo();
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        var after = loaded.CaptureSaveState().GameState.DemoCampaign!;
        Assert.Equal(JsonSerializer.Serialize(save.GameState.DemoCampaign!.EncounterResults), JsonSerializer.Serialize(after.EncounterResults));
        Assert.Single(after.PirateWorld!.Groups.Where(g => g.Retired));
        var corrupt = save with { GameState = save.GameState with { DemoCampaign = after with
        { EncounterResults = after.EncounterResults.SetItem(0, after.EncounterResults[0] with { PlayerWon = true }) } } };
        Assert.Throws<ScenarioException>(() => loaded.LoadScenario(corrupt, true));
        Assert.False(loaded.CaptureSaveState().GameState.DemoCampaign!.EncounterResults[0].PlayerWon);
    }
}
