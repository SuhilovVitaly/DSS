using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Tests.Demo;

namespace DeepSpaceSaga.Engine.Tests.PostbattleResults;

[Collection("Demo process debug")]
public sealed class EP0042Us0004Tk0001Tests
{
    [Fact]
    public void BulkLoot_StableTopDownTiesAndNoCargoPrefilter()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        FillCargoLeaving(engine, 0);
        long now = 100;
        engine.LootClockMilliseconds = () => now;
        var campaign = engine.CaptureSnapshot().DemoCampaign!;
        var loot = campaign.Rewards!.Loot!;
        Assert.Equal(0, loot.FreeCargoKg);
        Assert.True(engine.ExecuteDemoCampaign(new OpenBattleResultsCommand("open", campaign.Revision, loot.BattleId)).Applied);
        var bulk = new StartLootOperationsCommand("all", Revision(), loot.BattleId);
        var response = engine.ExecuteDemoCampaign(bulk);
        Assert.True(response.Applied);
        var operations = response.Rewards!.Loot!.Operations;
        Assert.NotEmpty(operations);
        Assert.Equal(operations, Assert.IsType<LootSessionSnapshot>(engine.ExecuteDemoCampaign(bulk).Rewards!.Loot).Operations);
        Assert.False(engine.ExecuteDemoCampaign(bulk with { CommandId = "second", ExpectedRevision = Revision() }).Applied);
        var moduleOrder = loot.Objects.SelectMany(source => source.Modules.Select(module =>
            new LootRowKey(source.ObjectId, LootRowKind.Module, module.InstanceId))).ToArray();
        Assert.Equal(moduleOrder, operations.Where(op => op.Row.Kind == LootRowKind.Module).Select(op => op.Row));
        Assert.All(operations, operation => Assert.Equal(operation.DurationMs, operation.RemainingMs));

        now += 10000;
        var advance = engine.ExecuteDemoCampaign(new AdvanceLootOperationsCommand("first-due", Revision(), loot.BattleId));
        Assert.True(advance.Applied);
        Assert.Equal(operations.Skip(1).Select(op => op.Row), advance.Rewards!.Loot!.Operations.Select(op => op.Row));
        Assert.DoesNotContain(advance.Rewards.Loot.Objects, source => source.Pilot?.Captured == true);

        long Revision() => engine.CaptureSnapshot().DemoCampaign!.Revision;
    }

    internal static void FillCargoLeaving(SimulationEngine engine, long leaveKg)
    {
        var loot = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!;
        var data = engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!;
        var wreck = data.Sources.First(source => source.Scrap is not null);
        var arranged = new DemoCampaignData
        {
            LootSession = data with
            {
                Sources = data.Sources.Replace(wreck, wreck with
                { Scrap = wreck.Scrap! with { Quantity = loot.FreeCargoKg - leaveKg, ChanceBasisPoints = 10000 } })
            }
        };
        string? reason = null;
        Action? fill = null;
        engine.PrepareBattleLootCommand(new AttemptScrapCommand("fill", 0, data.BattleId, wreck.ObjectId), ref arranged, ref reason, ref fill);
        Assert.Null(reason);
        Assert.NotNull(fill);
        fill();
    }
}
