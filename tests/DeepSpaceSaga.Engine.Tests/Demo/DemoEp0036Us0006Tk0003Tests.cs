using System.Text.Json;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0006Tk0003Tests
{
    [Theory]
    [InlineData(2500, "weapon", 2375)]
    [InlineData(2500, "engine", 2625)]
    [InlineData(3500, "weapon", 3325)]
    [InlineData(4000, "weapon", 3800)]
    [InlineData(3500, "engine", 3675)]
    [InlineData(4000, "engine", 4200)]
    [InlineData(3700, "armor", 3700)]
    public void ChanceFormula_ExactBasisPoints(int basis, string category, int expected) =>
        Assert.Equal(expected, SimulationEngine.ModuleSalvageChance(basis, category));

    [Fact]
    public void TwinEnginesIndependent()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var loot = DemoEp0036Us0003Tk0003Tests.Loot(engine);
        foreach (var source in loot.Sources.Where(s => s.Kind == "DisabledDrone"))
        {
            var engines = source.Modules.Where(m => m.Category == "engine").ToArray();
            Assert.Equal(2, engines.Length);
            Assert.NotEqual(engines[0].InstanceId, engines[1].InstanceId);
            Assert.Equal(engines[0].ItemTypeId, engines[1].ItemTypeId);
        }
        BattleLootData.Validate(loot);
    }

    [Fact]
    public void DamagedSourceSuccessProducesPlainIntactItem()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var drone = engine.TacticalBattleSessionForTests!.Battle.Drones.Last(d => d.Side == "B");
        drone.Engines[0].Factor = 0.5;
        var loot = DemoEp0036Us0003Tk0003Tests.Loot(engine);
        var source = loot.Sources.Single(s => s.ObjectId == drone.Id);
        var module = source.Modules.First(m => m.Category == "engine");
        source = source with { Modules = source.Modules.Replace(module, module with { Destroyed = false, ChanceBasisPoints = 10000 }) };
        var campaign = new DemoCampaignData { LootSession = loot with { Sources = loot.Sources.Select(s => s.ObjectId == source.ObjectId ? source : s).ToImmutableArray() } };
        string? reason = null; Action? commit = null;
        long before = engine.CaptureSnapshot().InstalledModules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == module.ItemTypeId).Sum(c => c.Quantity);
        var command = new AttemptModuleSalvageCommand("salvage", 0, loot.BattleId, source.ObjectId, module.InstanceId);
        engine.PrepareBattleLootCommand(command, ref campaign, ref reason, ref commit);
        Assert.Null(reason); Assert.NotNull(commit); commit();
        Assert.Equal(before + 1, engine.CaptureSnapshot().InstalledModules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == module.ItemTypeId).Sum(c => c.Quantity));
        Assert.Equal(LootAttemptStates.Collected, campaign.LootSession!.Sources.Single(s => s.ObjectId == source.ObjectId).Modules.Single(m => m.InstanceId == module.InstanceId).State);
        long draws = campaign.LootSession.DrawCount;
        engine.PrepareBattleLootCommand(command with { CommandId = "again" }, ref campaign, ref reason, ref commit);
        Assert.Equal(BattleLootReasons.AlreadyAttempted, reason);
        Assert.Equal(draws, campaign.LootSession.DrawCount);
        BattleLootData.Validate(campaign.LootSession);
    }

    [Fact]
    public void CargoFull_NoRngNoAttempt()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var loot = DemoEp0036Us0003Tk0003Tests.Loot(engine);
        var wreck = loot.Sources.First(s => s.Scrap is not null);
        long free = engine.CaptureSnapshot().InstalledModules.Sum(m => m.AvailableCapacityKg ?? 0);
        var campaign = new DemoCampaignData
        {
            LootSession = loot with
            {
                Sources = loot.Sources.Replace(wreck,
            wreck with { Scrap = wreck.Scrap! with { Quantity = free, ChanceBasisPoints = 10000 } })
            }
        };
        string? reason = null; Action? commit = null;
        engine.PrepareBattleLootCommand(new AttemptScrapCommand("fill", 0, loot.BattleId, wreck.ObjectId), ref campaign, ref reason, ref commit);
        Assert.Null(reason); Assert.NotNull(commit); commit();
        var source = campaign.LootSession!.Sources.First(s => s.Modules.Any(m => !m.Destroyed));
        var module = source.Modules.First(m => !m.Destroyed);
        var before = campaign;
        reason = null; commit = null;
        engine.PrepareBattleLootCommand(new AttemptModuleSalvageCommand("full", 0, loot.BattleId, source.ObjectId, module.InstanceId), ref campaign, ref reason, ref commit);
        Assert.Equal(BattleLootReasons.CargoFull, reason);
        Assert.Null(commit); Assert.Equal(before, campaign);
    }

    [Fact]
    public void SaveLoad_PreservesResults()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var loot = DemoEp0036Us0003Tk0003Tests.Loot(engine);
        string json = JsonSerializer.Serialize(loot);
        var restored = JsonSerializer.Deserialize<BattleLootData>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(restored));
        BattleLootData.Validate(restored);
    }
}
