using System.Text.Json;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class VoyageFuelContentTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void Packaged_basic_engine_declares_ten_km_per_kg_efficiency()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Data", "Modules", "Engine", "modules-engine.json")));
        var engine = Assert.Single(json.RootElement.GetProperty("moduleImplementations").EnumerateArray(),
            m => m.GetProperty("typeId").GetString() == "module.engine.basic");
        Assert.Equal(10, Assert.Single(engine.EnumerateObject(), p => p.Name == "fuelEfficiencyKmPerKg").Value.GetInt64());
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    public void Packaged_settings_boots_with_engine_efficiency_content(string scenario)
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios", scenario, "scenario.json")));
        var snapshot = engine.CaptureSnapshotForTests(0, simulationTimeMs: 0);
        Assert.Contains(snapshot.Objects, o => o.ObjectId == snapshot.PlayerShipObjectId);
        Assert.Equal(10, registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.engine.basic")).FuelEfficiencyKmPerKg);
    }

    [Fact]
    public void Fuel_efficiency_content_does_not_change_engine_motion_or_capacity_values()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
        var engine = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.engine.basic"));
        Assert.Equal(1000, engine.FuelCapacityKg);
        Assert.Equal(4000, engine.MaxSpeedMps);
        Assert.Equal(1, engine.TurnStepDegrees);
        Assert.Equal(400, engine.LinearInertiaMps2);
        Assert.Equal(4, engine.AngularInertiaDegPerSec);
        Assert.Equal(1000, engine.BaseCycleTimeMs);
        Assert.Equal(100, engine.BaseSuccessChancePercent);
    }
}
