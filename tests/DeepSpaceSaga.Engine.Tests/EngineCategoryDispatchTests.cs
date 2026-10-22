using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class EngineCategoryDispatchTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void Basic_engine_six_kms()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        var basic = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.engine.basic"));
        Assert.Equal("module.engine", basic.CategoryTypeId);
        Assert.Equal(6000, basic.MaxSpeedMps);
        Assert.Equal(10m, basic.Maneuverability);
        Assert.Equal(200, basic.FuelEfficiencyKmPerKg);
    }

    [Fact]
    public void Strizh_accepts_engine_commands()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        var strizh = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.engine.strizh"));
        Assert.Equal("module.engine", strizh.CategoryTypeId);
        Assert.Equal(8000, strizh.MaxSpeedMps);
        Assert.Equal(1000, strizh.LinearInertiaMps2);
        Assert.Equal(24, strizh.AngularInertiaDegPerSec);
        Assert.Equal(5000, strizh.MassKg);
        Assert.Equal(40m, strizh.Maneuverability);
        Assert.Contains(ShipEngineCommandTypes.Accelerate, strizh.CommandTypeIds);

        var source = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json"));
        string playerId = source.GameState.PlayerShipObjectId!;
        const string engineId = "MOD-PLAYER-ENGINE-01";
        var scenario = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId != playerId ? o : o with
                {
                    Modules = o.Modules!.Select(m => m.ModuleId == engineId ? m with
                    { ModuleTypeId = strizh.TypeId } : m).ToArray()
                }).ToArray()
            }
        };
        Assert.Equal(8, SolarSystemGenerator.OperationalMaxSpeedKmS(
            scenario.GameState.SpaceObjects.Single(o => o.ObjectId == playerId), registry));
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        Assert.Equal(8, engine.CaptureSnapshotForTests(0).Objects.Single(o => o.ObjectId == playerId).MaxSpeedKmS);
        engine.ReceiveCommand(new PlayerCommand("strizh-accelerate", 1, playerId, engineId, ShipEngineCommandTypes.Accelerate));
        var result = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1, 0);
        Assert.Equal(ShipEngineCommandTypes.Accelerate,
            result.InstalledModules.Single(m => m.ModuleId == engineId).ActiveCommandType);
    }
}
