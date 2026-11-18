using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0003Tk0003Tests
{
    [Fact]
    public void EncountersWithoutLootRejectBeforeCampaignPublication()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var definitions = DeepSpaceSaga.Engine.Content.DemoCampaignDefinitions.Load(DemoEp0033Us0001Tk0003Tests.Root());
        Assert.Throws<ScenarioException>(() => engine.ConfigureDemoCampaign(definitions with { Loot = null }));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    internal static BattleLootData Loot(SimulationEngine engine)
    {
        var session = engine.TacticalBattleSessionForTests!;
        var enemies = session.Battle.Drones.Where(d => d.Side != session.PlayerSide).ToArray();
        foreach (var drone in enemies) drone.Status = TacticalDroneStatus.Disabled;
        enemies[0].Status = TacticalDroneStatus.Destroyed;
        foreach (var carrier in session.Battle.Carriers.Where(c => c.Side != session.PlayerSide)) carrier.Hull = 0;
        return engine.InitializeBattleLoot(session);
    }

    [Fact]
    public void ScrapAttempt_IsOncePerWreck()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var loot = Loot(engine);
        var source = loot.Sources.First(s => s.Scrap is not null);
        var campaign = new DemoCampaignData { LootSession = loot };
        string? reason = null; Action? commit = null;
        engine.PrepareBattleLootCommand(new AttemptScrapCommand("one", 0, loot.BattleId, source.ObjectId), ref campaign, ref reason, ref commit);
        Assert.Null(reason);
        commit?.Invoke();
        Assert.Equal(loot.DrawCount + 1, campaign.LootSession!.DrawCount);
        var after = campaign;
        engine.PrepareBattleLootCommand(new AttemptScrapCommand("two", 0, loot.BattleId, source.ObjectId), ref campaign, ref reason, ref commit);
        Assert.Equal(BattleLootReasons.AlreadyAttempted, reason);
        Assert.Equal(after, campaign);
    }

    [Fact]
    public void CarrierOnlyScrap()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var loot = Loot(engine);
        Assert.All(loot.Sources.Where(s => s.Kind == "Carrier"), s =>
        { Assert.NotNull(s.Scrap); Assert.Empty(s.Consumables); Assert.Empty(s.Modules); Assert.Null(s.Pilot); });
        BattleLootData.Validate(loot);
    }

    [Fact]
    public void ConsumableDestruction_IsWholeStack()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var loot = Loot(engine);
        Assert.NotEmpty(loot.Sources.SelectMany(s => s.Consumables));
        Assert.All(loot.Sources.SelectMany(s => s.Consumables), c => Assert.Equal(c.Destroyed ? 0 : c.OriginalQuantity, c.Remaining));
        Assert.All(loot.Sources.Where(s => s.Kind == "DisabledDrone"), s => Assert.True(s.Pilot!.Alive));
    }

    [Fact]
    public void CargoFull_DoesNotConsumeAttempt()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var loot = Loot(engine);
        var source = loot.Sources.First(s => s.Scrap is not null);
        loot = loot with { Sources = loot.Sources.Replace(source, source with { Scrap = source.Scrap! with { Quantity = long.MaxValue } }) };
        var campaign = new DemoCampaignData { LootSession = loot };
        string? reason = null; Action? commit = null;
        engine.PrepareBattleLootCommand(new AttemptScrapCommand("full", 0, loot.BattleId, source.ObjectId), ref campaign, ref reason, ref commit);
        Assert.Equal(BattleLootReasons.CargoFull, reason);
        Assert.Null(commit);
        Assert.Equal(loot, campaign.LootSession);
    }

    [Fact]
    public void SaveLoad_DoesNotReroll()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        var loot = Loot(engine);
        var json = JsonSerializer.Serialize(loot);
        var restored = JsonSerializer.Deserialize<BattleLootData>(json)!;
        BattleLootData.Validate(restored);
        Assert.Equal(json, JsonSerializer.Serialize(restored));
        var malformed = restored with { Sources = restored.Sources.Add(restored.Sources[0]) };
        Assert.Throws<ScenarioException>(() => BattleLootData.Validate(malformed));
    }

    [Fact]
    public void UnknownBasis_RemainsUnknown()
    {
        var stack = SimulationEngine.AddCargoCost(new CargoStackRuntime(0, 4, null, [CargoAcquisitionSources.LegacyUnknown]), 0, 2, 0, "salvaged");
        Assert.Equal(6, stack.Quantity);
        Assert.Null(stack.CostBasisCredits);
        Assert.Equal(CargoAcquisitionSources.LegacyUnknown, Assert.Single(stack.AcquisitionSources));
    }
}
