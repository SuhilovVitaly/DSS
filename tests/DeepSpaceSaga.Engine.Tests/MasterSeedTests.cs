using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// ТЗ-02A: masterSeed for New Game and save/load continuation (requirements §15).
/// Covers all 5 acceptance criteria from implementation_tasks_diffs_03_12.md.
/// </summary>
public class MasterSeedTests
{
    private const string PlayerShipId = "SHIP";

    // Minimal New Game scenario (no masterSeed field — matches DefaultScenario's shape).
    private const string NewGameJson = """
    {
      "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed1",
        "playerShipObjectId": "SHIP",
        "spaceObjects": [
          { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" }
        ]
      }
    }
    """;

    // Acceptance criterion 1: two independent New Game sessions get different masterSeed.
    [Fact]
    public void New_game_generates_a_random_masterSeed()
    {
        var engine1 = new SimulationEngine();
        engine1.LoadScenario(ScenarioLoader.LoadFromJson(NewGameJson));

        var engine2 = new SimulationEngine();
        engine2.LoadScenario(ScenarioLoader.LoadFromJson(NewGameJson));

        Assert.NotEqual(0UL, engine1.MasterSeed);
        Assert.NotEqual(0UL, engine2.MasterSeed);
        Assert.NotEqual(engine1.MasterSeed, engine2.MasterSeed);

        // Expected/normal case, not a "legacy save" warning case.
        Assert.True(engine1.MasterSeedWasMissingOnLoad);
        Assert.True(engine2.MasterSeedWasMissingOnLoad);
    }

    [Fact]
    public void MasterSeed_does_not_change_across_speed_or_pause_changes()
    {
        var engine = new SimulationEngine();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(NewGameJson));
        ulong original = engine.MasterSeed;

        engine.SetSpeed(SimulationSpeed.Speed0);
        engine.SetSpeed(SimulationSpeed.Speed2);
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed2);
        engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed0);

        Assert.Equal(original, engine.MasterSeed);
    }

    // Acceptance criterion 2: save followed by load preserves the exact same masterSeed.
    [Fact]
    public void Save_then_load_preserves_the_same_masterSeed()
    {
        var engine = new SimulationEngine();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(NewGameJson));
        ulong original = engine.MasterSeed;

        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed1);
        Assert.Equal(original, save.GameState.MasterSeed);

        var loadedEngine = new SimulationEngine();
        loadedEngine.LoadScenario(save);

        Assert.Equal(original, loadedEngine.MasterSeed);
        // Reused as-is, not (re)generated.
        Assert.False(loadedEngine.MasterSeedWasMissingOnLoad);
    }

    // Acceptance criterion 3: deterministic RNG stream seed derivation is stable after save/load.
    [Fact]
    public void RngStreamSeedDerivation_is_pure_and_deterministic()
    {
        ulong a = RngStreamSeedDerivation.DeriveStreamSeed(12345UL, "asteroid-belt");
        ulong b = RngStreamSeedDerivation.DeriveStreamSeed(12345UL, "asteroid-belt");

        Assert.Equal(a, b);
    }

    [Fact]
    public void RngStreamSeedDerivation_differs_by_masterSeed_and_by_streamName()
    {
        ulong baseline = RngStreamSeedDerivation.DeriveStreamSeed(1UL, "stream-a");

        Assert.NotEqual(baseline, RngStreamSeedDerivation.DeriveStreamSeed(2UL, "stream-a"));
        Assert.NotEqual(baseline, RngStreamSeedDerivation.DeriveStreamSeed(1UL, "stream-b"));
    }

    [Fact]
    public void RngStreamSeedDerivation_is_stable_after_save_load_round_trip()
    {
        var engine = new SimulationEngine();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(NewGameJson));

        ulong seedBeforeSave = RngStreamSeedDerivation.DeriveStreamSeed(engine.MasterSeed, "asteroid-belt");

        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed1);
        var loadedEngine = new SimulationEngine();
        loadedEngine.LoadScenario(save);

        ulong seedAfterLoad = RngStreamSeedDerivation.DeriveStreamSeed(loadedEngine.MasterSeed, "asteroid-belt");

        Assert.Equal(seedBeforeSave, seedAfterLoad);
    }

    // Acceptance criterion 4: existing fixed DefaultScenario loading still works without
    // procedural asteroid generation (regression — the scenario file itself is untouched).
    [Fact]
    public void Real_default_scenario_still_loads_and_now_also_gets_a_masterSeed()
    {
        string settingsPath = ResolveRealSettingsPath();

        var engine = EngineContentLoader.CreateEngineFromSettingsFile(settingsPath);

        Assert.Equal("SPC-0001", engine.PlayerShipObjectId);
        Assert.Equal(4, engine.RuntimeObjects.Length);
        // Still the 2 fixed temporary asteroids — no procedural generation introduced.
        Assert.Equal(2, engine.RuntimeObjects.Count(o => o.PersistenceType == "Temporary"));

        Assert.NotEqual(0UL, engine.MasterSeed);
        Assert.True(engine.MasterSeedWasMissingOnLoad); // DefaultScenario has no masterSeed field
    }

    // Acceptance criterion 5: loading a legacy save without masterSeed auto-generates one
    // (and MasterSeedWasMissingOnLoad signals callers to warn — see LocalGameSessionConnection).
    [Fact]
    public void Legacy_save_without_masterSeed_autogenerates_one_and_flags_it()
    {
        const string legacySaveJson = """
        {
          "scenarioMetadata": { "scenarioId": "quicksave", "name": "Quicksave" },
          "saveFormatVersion": 1,
          "gameState": {
            "gameTimeMs": 5000, "currentSpeed": "Speed0",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;

        var legacySave = ScenarioLoader.LoadFromJson(legacySaveJson, allowNonZeroGameTime: true);
        Assert.Null(legacySave.GameState.MasterSeed);

        var engine = new SimulationEngine();
        engine.LoadScenario(legacySave);

        Assert.NotEqual(0UL, engine.MasterSeed);
        Assert.True(engine.MasterSeedWasMissingOnLoad);

        // Re-saving persists the now-generated value going forward.
        var resaved = engine.CaptureSaveStateForTests(5000, SimulationSpeed.Speed0);
        Assert.Equal(engine.MasterSeed, resaved.GameState.MasterSeed);
    }

    [Fact]
    public void Save_with_masterSeed_is_not_flagged_as_missing_on_reload()
    {
        const string saveWithSeedJson = """
        {
          "scenarioMetadata": { "scenarioId": "quicksave", "name": "Quicksave" },
          "saveFormatVersion": 1,
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "masterSeed": 9999999999,
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;

        var save = ScenarioLoader.LoadFromJson(saveWithSeedJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(save);

        Assert.Equal(9999999999UL, engine.MasterSeed);
        Assert.False(engine.MasterSeedWasMissingOnLoad);
    }

    private static string ResolveRealSettingsPath()
    {
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Settings.json"));
        }

        return settingsPath;
    }
}
