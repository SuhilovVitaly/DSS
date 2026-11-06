using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DebugTuningSaveTests
{

    [Theory]
    [InlineData("unknown", 2)]
    [InlineData("module.engine.tetrarch", -1)]
    public void Invalid_saved_overlay_does_not_change_current_world_registry_or_rng(string type, double value)
    {
        using var engine = Create();
        engine.ReceiveCommand(new("valid", 1, "", "", DebugTuningCommandTypes.Apply,
            DebugTuning: new([new("module.engine.tetrarch", "maxSpeedKmS", 2)])));
        PointCombatUS0018TK0002Tests.At(engine);
        var before = engine.CaptureSaveState();
        var corrupt = before with { GameState = before.GameState with { DebugTuning = [new(type, "maxSpeedKmS", value)] } };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(corrupt, true));
        Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void New_scenario_on_reused_engine_restores_original_catalog_defaults()
    {
        using var engine = DebugTuningShipTests.Create(3000);
        DebugTuningShipTests.Apply(engine, "maxSpeedKmS", 2);
        PointCombatUS0018TK0002Tests.At(engine);
        engine.LoadScenario(ScenarioLoader.LoadFromFile(Path.Combine(TorpedoImpactTests.ClientRoot,
            "Scenarios/PlayerShipOnly/scenario.json")));
        var state = engine.CaptureSnapshotForTests().DebugTuning!;
        Assert.Equal(0, state.Revision);
        Assert.Equal(state.Defaults.ToArray(), state.Values.ToArray());
    }

    [Fact]
    public void Failure_after_overlay_staging_restores_the_previous_effective_registry()
    {
        using var engine = DebugTuningShipTests.Create(3000);
        var before = engine.CaptureSaveState();
        var corrupt = before with
        {
            GameState = before.GameState with
            { DebugTuning = [new("module.engine.tetrarch", "maxSpeedKmS", 2)], SpaceObjects = [] }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(corrupt, true));
        var state = engine.CaptureSnapshotForTests();
        Assert.Equal(5, state.Objects.Single(o => o.ObjectId == state.PlayerShipObjectId).MaxSpeedKmS);
        Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }
}
