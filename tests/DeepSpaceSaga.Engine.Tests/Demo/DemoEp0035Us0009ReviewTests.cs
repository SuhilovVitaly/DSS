using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0009ReviewTests
{
    [Fact]
    public void LostPilotOperator_ClearsAssignmentAndLoadsAsScript()
    {
        using var engine = DemoEp0032Us0001Tk0002Tests.Tutorial();
        var pilotId = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones[0].CrewId;
        engine.LoadScenario(DemoEp0035Us0009Tk0002Tests.Assign(engine.CaptureSaveState(), pilotId), true);
        var start = DemoEp0032Us0001Tk0002Tests.Start(engine, "operator-loss");
        var session = engine.TacticalBattleSessionForTests!;
        session.Battle.Drones.First(d => d.Side == session.PlayerSide).Status = TacticalDroneStatus.Destroyed;
        engine.ExitTacticalBattle(new("exit", start.State!.BattleId));
        var sensor = engine.CaptureSnapshot().DemoCampaign!.SensorOperator!;
        Assert.Null(sensor.CrewId);
        Assert.Equal(1920, sensor.EffectiveRadiusWorldUnits);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true), true);
        Assert.Equal(sensor, engine.CaptureSnapshot().DemoCampaign!.SensorOperator);
    }

    [Fact]
    public void OverflowingSkill_RejectsLoadWithoutReplacingWorld()
    {
        var source = EngineContentLoader.LoadRegistryFromSettingsFile(DemoEp0033Us0001Tk0003Tests.Root() + "/Settings.json", out _, out _);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories),
            All(source.ModuleTypes).Select(m => m.TypeId == "module.sensor.player-start" ? m with { SensorRadiusWorldUnits = double.MaxValue } : m),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), source.CatalogVersion, source.LegacyCatalogFingerprint,
            All(source.StationMarketProfiles), All(source.ShipClasses), All(source.StationMarketEvents), tacticalBattle: source.TacticalBattle);
        using var original = DemoEp0032Us0001Tk0002Tests.Tutorial();
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(DemoEp0035Us0009Tk0002Tests.Assign(original.CaptureSaveState(), null), true);
        var before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var invalid = DemoEp0035Us0009Tk0002Tests.Assign(engine.CaptureSaveState(), "CHR-PLAYER-DEFENSE");
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(invalid, true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
        Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
}
