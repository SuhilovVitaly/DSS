using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class ScenarioEngineTests
{
    private const string DefaultScenarioJson = """
    {
      "scenarioMetadata": { "scenarioId": "default", "name": "Default Scenario" },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0",
        "playerShipObjectId": "SPC-0001",
        "focus": { "mode": "Attached", "objectId": "SPC-0001" },
        "spaceObjects": [
          { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 10000, "positionY": 10000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" },
          { "objectId": "SPC-0002", "objectType": "Station", "persistenceType": "Permanent",
            "positionX": 11000, "positionY": 11000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" },
          { "objectId": "SPC-0003", "objectType": "Asteroid", "persistenceType": "Temporary",
            "positionX": 10400, "positionY": 10000, "speedMps": 100, "directionDegrees": 270,
            "movementType": "Linear", "massKg": 1000000, "compositionType": "Silicate" },
          { "objectId": "SPC-0004", "objectType": "Asteroid", "persistenceType": "Temporary",
            "positionX": 10000, "positionY": 10450, "speedMps": 200, "directionDegrees": 0,
            "movementType": "Linear", "massKg": 1000000, "compositionType": "Ice" }
        ]
      }
    }
    """;

    [Fact]
    public void Engine_loads_scenario_with_4_objects()
    {
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        Assert.Equal("SPC-0001", engine.PlayerShipObjectId);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
    }

    [Fact]
    public async Task First_snapshot_has_scenario_objects()
    {
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        AuthoritativeSnapshot? first = null;

        await foreach (var snapshot in engine.RunAsync(cts.Token))
        {
            first = snapshot;
            break;
        }

        Assert.NotNull(first);
        Assert.Equal(4, first.Objects.Length);
        Assert.Equal("SPC-0001", first.PlayerShipObjectId);

        // Objects at expected positions (gameTime=0, Speed0, so no movement)
        Assert.Contains(first.Objects, o => o.ObjectId == "SPC-0001" && o.X == 10000 && o.Y == 10000);
        Assert.Contains(first.Objects, o => o.ObjectId == "SPC-0002" && o.X == 11000 && o.Y == 11000);
        Assert.Contains(first.Objects, o => o.ObjectId == "SPC-0003" && o.X == 10400 && o.Y == 10000);
        Assert.Contains(first.Objects, o => o.ObjectId == "SPC-0004" && o.X == 10000 && o.Y == 10450);
    }

    [Fact]
    public async Task First_snapshot_has_GameTime_0_and_Speed0()
    {
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        AuthoritativeSnapshot? first = null;

        await foreach (var snapshot in engine.RunAsync(cts.Token))
        {
            first = snapshot;
            break;
        }

        Assert.NotNull(first);
        Assert.Equal(0, first.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, first.CurrentSpeed);
    }

    [Fact]
    public async Task Probe_1_is_not_in_first_snapshot()
    {
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        AuthoritativeSnapshot? first = null;

        await foreach (var snapshot in engine.RunAsync(cts.Token))
        {
            first = snapshot;
            break;
        }

        Assert.NotNull(first);
        Assert.DoesNotContain(first.Objects, o => o.ObjectId == "probe-1");
    }

    [Fact]
    public async Task First_snapshot_is_yielded_immediately()
    {
        // The initial snapshot must arrive without the 1-second delay.
        // We verify by timing: if it takes < 500ms, it's immediate.
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        await foreach (var snapshot in engine.RunAsync(cts.Token))
        {
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds < 500,
                $"First snapshot took {sw.ElapsedMilliseconds}ms, expected < 500ms (immediate)");
            break;
        }
    }

    [Fact]
    public void Can_load_real_scenario_file()
    {
        // Walk up from the test output directory to find the repo root,
        // then into the Client project where the scenario lives.
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Scenarios", "Default", "scenario.json"));

        // Fallback for environments where the output structure differs (CI / dotnet test from sln dir)
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json"));
        }

        Assert.True(File.Exists(path),
            $"Real scenario file not found at '{path}'. " +
            "Ensure it is copied to the output directory or the test is running from the repo root.");

        var scenario = ScenarioLoader.LoadFromFile(path);
        Assert.Equal("default", scenario.Metadata.ScenarioId);
        Assert.Equal("SPC-0001", scenario.GameState.PlayerShipObjectId);
        Assert.Equal(4, scenario.GameState.SpaceObjects.Count);

        var playerShip = scenario.GameState.SpaceObjects
            .Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        var cargoModule = Assert.Single(playerShip.Modules ?? []);
        Assert.Equal("Container", cargoModule.ModuleType);
        Assert.Equal(100_000, cargoModule.CapacityKg);

        var energyCells = Assert.Single(cargoModule.Cargo ?? []);
        Assert.Equal("Energy Cells", energyCells.ResourceType);
        Assert.Equal(1_000, energyCells.Quantity);
        Assert.Equal(10, energyCells.UnitMassKg);
    }

    [Fact]
    public void LoadScenario_sets_speed_from_scenario()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed2",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;

        var scenario = ScenarioLoader.LoadFromJson(json);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        Assert.Equal(SimulationSpeed.Speed2, engine.CurrentSpeed);
    }
}
