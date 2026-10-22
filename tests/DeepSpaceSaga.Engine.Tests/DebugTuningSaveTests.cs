using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DebugTuningSaveTests
{
    [Fact]
    public void Retimed_flight_roundtrips_through_a_real_save_file_and_detonates_at_the_same_time()
    {
        using var engine = Create(o => o with { PositionX = o.ObjectId == Player ? 0 : 10000, PositionY = 0 }, scatter: 0);
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "medium")); PointCombatUS0018TK0002Tests.At(engine);
        PointCombatUS0018TK0002Tests.At(engine, 2000);
        engine.ReceiveCommand(new("tune", 2, "", "", DebugTuningCommandTypes.Apply,
            DebugTuning: new([new("module.torpedo.launcher.basic", "speedKmS", 20, "medium")])));
        PointCombatUS0018TK0002Tests.At(engine, 2000);
        var save = engine.CaptureSaveStateForTests(600000, SimulationSpeed.Speed0, 2000);
        Assert.Equal(19, save.SaveFormatVersion);
        Assert.Single(save.GameState.DebugTuning!);
        Assert.NotNull(Assert.Single(save.GameState.PointCombatState!.Projectiles).OriginalFlight);
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep0013/save-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "midflight.json");
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            using var restored = new SimulationEngine(Registry(0));
            restored.LoadScenario(ScenarioLoader.LoadFromFile(path, true, true), true);
            var flight = Flight(PointCombatUS0018TK0002Tests.At(restored, 2000));
            Assert.Equal(3000, flight.OwnProjectile!.DetonationMotionTimeMs);
            Assert.Equal(200, flight.X, 8);
            Assert.Equal(20, PointCombatUS0018TK0002Tests.At(restored, 2000).DebugTuning!.Values.Single(v =>
                v.ModuleTypeId == "module.torpedo.launcher.basic" && v.Field == "speedKmS" && v.ProfileId == "medium").Value);
            var row = Assert.Single(save.GameState.PointCombatState.Projectiles);
            var corrupt = save with
            {
                GameState = save.GameState with
                {
                    PointCombatState = save.GameState.PointCombatState with
                    { Projectiles = [row with { OriginalFlight = row.OriginalFlight! with { DetonationX = 401 } }] }
                }
            };
            string beforeFailure = ScenarioLoader.Serialize(restored.CaptureSaveStateForTests(600000, SimulationSpeed.Speed0, 2000));
            Assert.Throws<ScenarioException>(() => restored.LoadScenario(corrupt, true));
            Assert.Equal(beforeFailure, ScenarioLoader.Serialize(restored.CaptureSaveStateForTests(600000, SimulationSpeed.Speed0, 2000)));
            var first = PointCombatUS0018TK0002Tests.At(engine, 3000);
            var second = PointCombatUS0018TK0002Tests.At(restored, 3000);
            Assert.Equal(JsonSerializer.Serialize(first.CombatJournal), JsonSerializer.Serialize(second.CombatJournal));
            Assert.Equal(engine.CaptureLaunchScatterState(), restored.CaptureLaunchScatterState());
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Save18_without_tuning_migrates_to_catalog_and_default_overlay_is_omitted(bool inFlight)
    {
        using var engine = Create();
        if (inFlight) { engine.ReceiveCommand(Launch()); PointCombatUS0018TK0002Tests.At(engine); }
        var save = engine.CaptureSaveState();
        string json = ScenarioLoader.Serialize(save);
        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.GetProperty("gameState").TryGetProperty("debugTuning", out _));
        var old = save with
        {
            SaveFormatVersion = 18,
            GameState = save.GameState with
            {
                PointCombatState = save.GameState.PointCombatState! with
                { Projectiles = save.GameState.PointCombatState.Projectiles.Select(p => p with { LauncherModuleTypeId = null, OriginalFlight = null }).ToArray() }
            }
        };
        var migrated = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(old), true, true);
        Assert.Equal(19, migrated.SaveFormatVersion);
        using var restored = new SimulationEngine(Registry()); restored.LoadScenario(migrated, true);
        var state = restored.CaptureSnapshotForTests().DebugTuning!;
        Assert.Equal(state.Defaults.ToArray(), state.Values.ToArray());
        Assert.Null(restored.CaptureSaveState().GameState.DebugTuning);
        if (inFlight) Assert.Equal(Flight(PointCombatUS0018TK0002Tests.At(engine)).OwnProjectile,
            Flight(PointCombatUS0018TK0002Tests.At(restored)).OwnProjectile);
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson("{\"saveFormatVersion\":17,\"gameState\":\"old\"}", true, true));
    }

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
