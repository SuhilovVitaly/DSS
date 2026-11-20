using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Tests.Demo;

namespace DeepSpaceSaga.Engine.Tests.PostbattleResults;

public sealed class EP0042Us0003Tk0002Tests
{
    [Theory]
    [InlineData(LootRowKind.Consumable, 3000)]
    [InlineData(LootRowKind.Module, 5000)]
    [InlineData(LootRowKind.Scrap, 10000)]
    public void LootOperations_SingleDurationsAndOneRoll(LootRowKind kind, int duration)
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        long now = 10000;
        engine.LootClockMilliseconds = () => now;
        var before = engine.CaptureSnapshot();
        var campaign = before.DemoCampaign!;
        var loot = campaign.Rewards!.Loot!;
        Assert.True(engine.ExecuteDemoCampaign(new OpenBattleResultsCommand("open", campaign.Revision, loot.BattleId)).Applied);
        var target = SelectRow(loot, kind);
        long draws = engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!.DrawCount;
        var start = new StartLootOperationsCommand("start", Revision(), loot.BattleId, target);
        var accepted = engine.ExecuteDemoCampaign(start);
        Assert.True(accepted.Applied);
        Assert.Equal(duration, Assert.Single(accepted.Rewards!.Loot!.Operations).DurationMs);
        Assert.Equal(accepted, engine.ExecuteDemoCampaign(start));
        Assert.Equal(BattleLootReasons.OperationsRunning,
            engine.ExecuteDemoCampaign(start with { CommandId = "duplicate", ExpectedRevision = Revision() }).Reason);
        Assert.Equal(BattleLootReasons.OperationsRunning, engine.ExecuteDemoCampaign(
            new AcknowledgeBattleResultsCommand("exit", Revision(), loot.BattleId, true)).Reason);

        now += duration - 1;
        Assert.Equal(BattleLootReasons.OperationNotDue, engine.ExecuteDemoCampaign(
            new AdvanceLootOperationsCommand("early", Revision(), loot.BattleId)).Reason);
        now++;
        var advance = new AdvanceLootOperationsCommand("due", Revision(), loot.BattleId);
        var completed = engine.ExecuteDemoCampaign(advance);
        Assert.True(completed.Applied);
        Assert.Empty(completed.Rewards!.Loot!.Operations);
        Assert.Equal(completed, engine.ExecuteDemoCampaign(advance));
        Assert.Equal(draws + (kind == LootRowKind.Consumable ? 0 : 1),
            engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!.DrawCount);
        var after = engine.CaptureSnapshot();
        Assert.Equal(before.GameTimeMs, after.GameTimeMs);
        Assert.Equal(before.MotionTimeMs, after.MotionTimeMs);

        long Revision() => engine.CaptureSnapshot().DemoCampaign!.Revision;
    }

    private static LootRowKey SelectRow(LootSessionSnapshot loot, LootRowKind kind)
    {
        foreach (var source in loot.Objects)
        {
            string? id = kind switch
            {
                LootRowKind.Consumable => source.Consumables.FirstOrDefault(item => item.CanCollect)?.ItemTypeId,
                LootRowKind.Module => source.Modules.FirstOrDefault(item => item.CanAttempt)?.InstanceId,
                LootRowKind.Scrap => source.Scrap is { CanAttempt: true } scrap ? scrap.ItemTypeId : null,
                _ => null
            };
            if (id is not null) return new(source.ObjectId, kind, id);
        }
        throw new InvalidOperationException("Test encounter has no recoverable row.");
    }
}
