using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0007Tk0003Tests
{
    internal static SimulationEngine Won()
    {
        var engine = Ep0035Us15Tk3Tests.Begin();
        var session = engine.TacticalBattleSessionForTests!;
        var enemies = session.Battle.Drones.Where(d => d.Side != session.PlayerSide).ToArray();
        foreach (var drone in enemies.Skip(1)) drone.Status = TacticalDroneStatus.Disabled;
        Assert.True(engine.DebugDestroyTacticalUnit(new("last", session.Id, session.Revision, enemies[0].Id)).Accepted);
        return engine;
    }

    [Fact]
    public void VictoryCommitsBeforeFirstLoot()
    {
        using var engine = Won();
        Assert.False(engine.IsTacticalBattleActive);
        Assert.NotNull(engine.GetTacticalBattleState());
        var save = engine.CaptureSaveState();
        Assert.True(Assert.Single(save.GameState.DemoCampaign!.EncounterResults).PlayerWon);
        Assert.Equal(LootPhases.Open, save.GameState.DemoCampaign.LootSession!.Phase);
        Assert.Single(save.GameState.DemoCampaign.PirateWorld!.Groups.Where(g => g.Retired));
    }

    [Fact]
    public void ExitWarningThenDiscard_DoesNotSettleTwice()
    {
        using var engine = Won();
        var snapshot = engine.CaptureSnapshot().DemoCampaign!;
        var battle = snapshot.Rewards!.Loot!.BattleId;
        var finish = new FinishBattleLootCommand("warn", snapshot.Revision, battle);
        Assert.Equal(BattleLootReasons.ConfirmDiscardRequired, engine.ExecuteDemoCampaign(finish).Reason);
        Assert.Equal(TacticalBattleRejectionCode.WrongPhase, engine.ExitTacticalBattle(new("exit", battle)).Code);
        var confirmed = finish with { CommandId = "confirm", ConfirmDiscard = true };
        var response = engine.ExecuteDemoCampaign(confirmed);
        Assert.True(response.Applied);
        Assert.Equal(LootPhases.Closed, response.Rewards!.Loot!.Phase);
        Assert.Equal(response, engine.ExecuteDemoCampaign(confirmed));
        Assert.Null(engine.GetTacticalBattleState());
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.EncounterResults);
        Assert.Equal(BattleLootReasons.LootClosed, engine.ExecuteDemoCampaign(confirmed with
        { CommandId = "again", ExpectedRevision = response.Revision }).Reason);
    }

    [Fact]
    public void LoadRestoresFrozenSourcesNotCombat()
    {
        using var engine = Won();
        var saved = engine.CaptureSaveState();
        string before = JsonSerializer.Serialize(saved.GameState.DemoCampaign!.LootSession);
        var loaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true, true);
        engine.LoadScenario(loaded, true);
        Assert.False(engine.IsTacticalBattleActive);
        Assert.NotNull(engine.GetTacticalBattleState());
        Assert.Equal(before, JsonSerializer.Serialize(engine.CaptureSaveState().GameState.DemoCampaign!.LootSession));
        Assert.Equal(SimulationEngine.TacticalBattleActiveReason, engine.TrySetSpeed(SimulationSpeed.Speed1));
    }

    [Fact]
    public void WrongBattleRejected_AndWorldClockFrozenDuringLootOpen()
    {
        using var engine = Won();
        var before = engine.CaptureSnapshot();
        Assert.Equal(BattleLootReasons.WrongBattle, engine.ExecuteDemoCampaign(new FinishBattleLootCommand("wrong", before.DemoCampaign!.Revision, "wrong", true)).Reason);
        Assert.Equal(DemoCampaignReasons.BattleActive, engine.ExecuteDemoCampaign(new CancelBountyCommand("blocked", before.DemoCampaign.Revision, "bounty:pirate:01")).Reason);
        Assert.Equal(SimulationEngine.TacticalBattleActiveReason, engine.TrySetSpeed(SimulationSpeed.Speed1));
        Assert.Equal(before.GameTimeMs, engine.CaptureSnapshot().GameTimeMs);
        Assert.Equal(before.MotionTimeMs, engine.CaptureSnapshot().MotionTimeMs);
    }

    [Fact]
    public void ForgedScrapOrModuleMapping_RejectsWithoutReplacingWorld()
    {
        using var engine = Won();
        var saved = engine.CaptureSaveState();
        var campaign = saved.GameState.DemoCampaign!;
        var loot = campaign.LootSession!;
        var wreck = loot.Sources.First(s => s.Scrap is not null);
        foreach (var scrap in new[] { wreck.Scrap! with { ItemTypeId = "item.electronics" }, wreck.Scrap! with { Quantity = wreck.Scrap.Quantity + 1 } })
        {
            var corrupt = saved with
            {
                GameState = saved.GameState with
                {
                    DemoCampaign = campaign with
                    { LootSession = loot with { Sources = loot.Sources.Replace(wreck, wreck with { Scrap = scrap }) } }
                }
            };
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(corrupt, true));
            Assert.Equal(JsonSerializer.Serialize(loot), JsonSerializer.Serialize(engine.CaptureSaveState().GameState.DemoCampaign!.LootSession));
        }
        var drone = loot.Sources.First(s => !s.Modules.IsEmpty);
        var module = drone.Modules[0];
        var bad = saved with
        {
            GameState = saved.GameState with
            {
                DemoCampaign = campaign with
                {
                    LootSession = loot with
                    { Sources = loot.Sources.Replace(drone, drone with { Modules = drone.Modules.SetItem(0, module with { ItemTypeId = "item.scrap" }) }) }
                }
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, true));
    }

    [Fact]
    public void DefeatHasNoLoot()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        DemoEp0036Us0001Tk0002Tests.Finish(engine, false);
        Assert.Null(engine.CaptureSaveState().GameState.DemoCampaign!.LootSession);
    }
}
