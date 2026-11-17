using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

[Collection("Demo process debug")]
public sealed class DemoEp0036Us0004Tk0001Tests
{
    [Fact]
    public void AcceptedBounty_LootArrestRoundtrip_PaidOnlyOnce()
    {
        try
        {
            using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
            Assert.True(engine.ExecuteDemoCampaign(new AcceptBountyCommand("track", engine.CaptureSnapshot().DemoCampaign!.Revision, "bounty:pirate:01")).Applied);
            engine.LoadScenario(Ep0035Us3Tk2Tests.CapturedScenario(engine), true);
            var state = engine.CaptureSnapshot().DemoCampaign!;
            Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", state.Revision, state.PendingSensorReport!.BatchId)).Applied);
            state = engine.CaptureSnapshot().DemoCampaign!;
            Assert.True(engine.ExecuteDemoCampaign(new BeginEncounterBattleCommand("begin", state.Revision, state.WorldEncounter!.Binding.EncounterId)).Applied);
            SimulationEngine.SetLootDebug(new("debug", 0, 15, "keep", "success"));
            var battle = engine.TacticalBattleSessionForTests!;
            foreach (var drone in battle.Battle.Drones.Where(d => d.Side != battle.PlayerSide)) drone.Status = TacticalDroneStatus.Disabled;
            battle.Battle.CheckVictory();
            Assert.True(engine.ExitTacticalBattle(new("loot", battle.Id)).Accepted);
            long credits = engine.PlayerCredits;
            var loot = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!;
            var source = loot.Objects.First(s => s.Modules.Length > 0 && s.Consumables.Any(c => c.Remaining > 1));
            var item = source.Consumables.First(c => c.Remaining > 1);
            var module = source.Modules[0];
            DemoCampaignResponse Apply(Func<long, DemoCampaignCommand> create) => engine.ExecuteDemoCampaign(create(engine.CaptureSnapshot().DemoCampaign!.Revision));
            Assert.True(Apply(r => new CollectLootConsumablesCommand("partial", r, loot.BattleId, source.ObjectId, item.ItemTypeId, 1)).Applied);
            Assert.True(Apply(r => new AttemptModuleSalvageCommand("module", r, loot.BattleId, source.ObjectId, module.InstanceId)).Applied);
            Assert.True(Apply(r => new ArrestPilotCommand("arrest", r, loot.BattleId, source.ObjectId)).Applied);
            var saved = engine.CaptureSaveState();
            string ledger = JsonSerializer.Serialize(saved.GameState.DemoCampaign!.LootSession);
            engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true, true), true);
            Assert.False(engine.IsTacticalBattleActive);
            Assert.Equal(ledger, JsonSerializer.Serialize(engine.CaptureSaveState().GameState.DemoCampaign!.LootSession));
            Assert.Single(engine.CaptureSnapshot().DemoCampaign!.Company!.Prisoners!.Prisoners);
            Assert.Equal(BattleLootReasons.AlreadyAttempted, Apply(r => new AttemptModuleSalvageCommand("module-again", r, loot.BattleId, source.ObjectId, module.InstanceId)).Reason);
            Assert.Equal(PrisonerCustodyReasons.AlreadyCaptured, Apply(r => new ArrestPilotCommand("arrest-again", r, loot.BattleId, source.ObjectId)).Reason);
            Assert.True(Apply(r => new CollectLootConsumablesCommand("rest", r, loot.BattleId, source.ObjectId, item.ItemTypeId, item.Remaining - 1)).Applied);
            Assert.Equal(credits, engine.PlayerCredits);
            Assert.True(Apply(r => new FinishBattleLootCommand("finish", r, loot.BattleId, true)).Applied);
            DemoEp0036Us0002Tk0002Tests.Dock(engine);
            Assert.True(DemoEp0036Us0002Tk0002Tests.Claim(engine).Applied);
            Assert.Equal(credits + 1500, engine.PlayerCredits);
            saved = engine.CaptureSaveState();
            engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true, true), true);
            Assert.Equal(TargetBountyReasons.AlreadyPaid, DemoEp0036Us0002Tk0002Tests.Claim(engine, "again").Reason);
            Assert.Equal(credits + 1500, engine.PlayerCredits);
            Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.BountyPayments);
            Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.EncounterResults);
        }
        finally { SimulationEngine.SetLootDebug(new("reset", 0)); }
    }

    [Fact]
    public void OpenPreviousLootWithPendingNextBattleRejectedAtomically()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new FinishBattleLootCommand("close", state.Revision, state.Rewards!.Loot!.BattleId, true)).Applied);
        var saved = engine.CaptureSaveState();
        var campaign = saved.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!;
        var group = world.Groups.First(g => !g.Retired);
        engine.LoadScenario(saved with { GameState = saved.GameState with {
            SpaceObjects = saved.GameState.SpaceObjects.Select(s => s.ObjectId != saved.GameState.PlayerShipObjectId ? s : s with
            { PositionX = group.X, PositionY = group.Y + 1500, SpeedMps = 0, Orbit = null, IsDocked = false, DockedStationObjectId = null }).ToArray(),
            DemoCampaign = campaign with { WorldEncounter = null, PirateWorld = world with {
                Groups = world.Groups.Replace(group, group with { Phase = "captured", CapturedMotionTimeMs = world.LastMotionTimeMs, MotionPlan = null }) } }
        } }, true);
        state = engine.CaptureSnapshot().DemoCampaign!;
        if (state.PendingSensorReport is { } report)
            Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack-next", state.Revision, report.BatchId)).Applied);
        state = engine.CaptureSnapshot().DemoCampaign!;
        var checkpoint = engine.CapturePendingEncounterCheckpoint(new("next", state.Revision, state.WorldEncounter!.Binding.EncounterId));
        using var target = DemoEp0033Us0001Tk0003Tests.Demo();
        var before = ScenarioLoader.Serialize(target.CaptureSaveState());
        var corrupt = checkpoint with { GameState = checkpoint.GameState with { DemoCampaign = checkpoint.GameState.DemoCampaign! with {
            LootSession = checkpoint.GameState.DemoCampaign!.LootSession! with { Phase = LootPhases.Open } } } };
        Assert.Throws<ScenarioException>(() => target.LoadScenario(corrupt, true));
        Assert.Equal(before, ScenarioLoader.Serialize(target.CaptureSaveState()));
        // The unchanged closed ledger remains compatible with a legitimate next-battle checkpoint.
        target.LoadScenario(checkpoint, true);
        Assert.True(target.IsTacticalBattleActive);
    }

    [Fact]
    public void MalformedLedgerRejectedWithoutReplacingWorld()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var saved = engine.CaptureSaveState();
        var campaign = saved.GameState.DemoCampaign!;
        var loot = campaign.LootSession!;
        var source = loot.Sources.First(s => s.Consumables.Length > 0);
        foreach (var corrupt in new[]
        {
            loot with { Phase = "Combat" },
            loot with { Sources = loot.Sources.Add(loot.Sources[0]) },
            loot with { Sources = loot.Sources.Replace(source, source with { Consumables = source.Consumables.SetItem(0, source.Consumables[0] with { Remaining = -1 }) }) }
        })
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(saved with { GameState = saved.GameState with { DemoCampaign = campaign with { LootSession = corrupt } } }, true));
            Assert.Equal(JsonSerializer.Serialize(loot), JsonSerializer.Serialize(engine.CaptureSaveState().GameState.DemoCampaign!.LootSession));
        }
    }
}
