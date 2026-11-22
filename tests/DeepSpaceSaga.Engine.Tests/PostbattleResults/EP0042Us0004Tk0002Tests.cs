using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Tests.Demo;

namespace DeepSpaceSaga.Engine.Tests.PostbattleResults;

[Collection("Demo process debug")]
public sealed class EP0042Us0004Tk0002Tests
{
    [Fact]
    public void BulkLoot_PartialConsumablesWaitingClaimNoReroll()
    {
        SimulationEngine.SetLootDebug(new("force-success", 0, Attempts: "success"));
        try
        {
            using var engine = DemoEp0036Us0007Tk0003Tests.Won();
            long now = 10000;
            engine.LootClockMilliseconds = () => now;
            var loot = Current();
            var holder = loot.Objects.First(source => source.Consumables.Any(item => item.Remaining > 1));
            var stack = holder.Consumables.First(item => item.Remaining > 1);
            var wreck = loot.Objects.First(source => source.Scrap is not null);
            var scrap = wreck.Scrap!;
            EP0042Us0004Tk0001Tests.FillCargoLeaving(engine, stack.UnitMassKg);
            Assert.True(engine.ExecuteDemoCampaign(new OpenBattleResultsCommand("open", Revision(), loot.BattleId)).Applied);
            var itemRow = new LootRowKey(holder.ObjectId, LootRowKind.Consumable, stack.ItemTypeId);
            var scrapRow = new LootRowKey(wreck.ObjectId, LootRowKind.Scrap, scrap.ItemTypeId);
            Assert.True(engine.ExecuteDemoCampaign(new StartLootOperationsCommand("stack", Revision(), loot.BattleId, itemRow)).Applied);
            Assert.True(engine.ExecuteDemoCampaign(new StartLootOperationsCommand("scrap", Revision(), loot.BattleId, scrapRow)).Applied);

            now += 3000;
            Assert.True(engine.ExecuteDemoCampaign(new AdvanceLootOperationsCommand("first", Revision(), loot.BattleId)).Applied);
            Assert.Equal(stack.Remaining - 1, Current().Objects.Single(source => source.ObjectId == holder.ObjectId)
                .Consumables.Single(item => item.ItemTypeId == stack.ItemTypeId).Remaining);
            Assert.Equal(0, Current().FreeCargoKg);
            now += 7000;
            Assert.True(engine.ExecuteDemoCampaign(new AdvanceLootOperationsCommand("second", Revision(), loot.BattleId)).Applied);
            Assert.Equal(LootAttemptStates.ExtractedWaiting, Current().Objects.Single(source => source.ObjectId == wreck.ObjectId).Scrap!.State);
            var saved = engine.CaptureSaveState().GameState.DemoCampaign!;
            long draws = saved.LootSession!.DrawCount;
            Assert.False(engine.ExecuteDemoCampaign(new StartLootOperationsCommand("reroll", Revision(), loot.BattleId, scrapRow)).Applied);
            Assert.Equal(BattleLootReasons.CargoFull, engine.ExecuteDemoCampaign(
                new ClaimExtractedLootCommand("full", Revision(), loot.BattleId, scrapRow)).Reason);

            // Arrange free cargo through the existing world mutation seam. The new results UI
            // deliberately offers no cargo disposal.
            string? reason = null;
            Action? commit = null;
            engine.PrepareBattleLootCommand(new JettisonCargoCommand("arrange-space", Revision(), loot.BattleId, scrap.ItemTypeId, scrap.Quantity),
                ref saved, ref reason, ref commit);
            Assert.Null(reason);
            Assert.NotNull(commit);
            commit();
            var claim = new ClaimExtractedLootCommand("claim", Revision(), loot.BattleId, scrapRow);
            var granted = engine.ExecuteDemoCampaign(claim);
            Assert.True(granted.Applied);
            Assert.Equal(granted, engine.ExecuteDemoCampaign(claim));
            Assert.Equal(LootAttemptStates.Collected, Current().Objects.Single(source => source.ObjectId == wreck.ObjectId).Scrap!.State);
            Assert.Equal(draws, engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!.DrawCount);

            long Revision() => engine.CaptureSnapshot().DemoCampaign!.Revision;
            LootSessionSnapshot Current() => engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!;
        }
        finally { SimulationEngine.SetLootDebug(new("reset", 0)); }
    }
}
