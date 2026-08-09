using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers 4.4: bootstrapping a brand new engine from a save file (mirrors New Game
/// bootstrap from settings, but the scenario source is a save with gameTimeMs &gt; 0
/// and an in-progress module ActiveCycle).
/// </summary>
public class EngineContentLoaderSaveFileTests
{
    [Fact]
    public void CreateEngineFromSaveFile_builds_engine_and_continues_active_turn_cycle()
    {
        string settingsPath = ResolveRealSettingsPath();

        // Hand-written save: gameTimeMs > 0 (rejected for New Game, allowed here), and
        // the engine module's turn cycle is already 500 ms into its 1000 ms window.
        string saveJson = """
        {
          "scenarioMetadata": { "scenarioId": "quicksave", "name": "Quicksave" },
          "saveFormatVersion": 1,
          "gameState": {
            "gameTimeMs": 5000,
            "currentSpeed": "Speed0",
            "playerShipObjectId": "SPC-0001",
            "spaceObjects": [
              {
                "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "name": "Player Ship",
                "positionX": 10000.0, "positionY": 10000.0,
                "speedMps": 0, "directionDegrees": 90, "movementType": "Stationary",
                "modules": [
                  {
                    "moduleId": "MOD-PLAYER-ENGINE-01",
                    "moduleTypeId": "module.engine.basic",
                    "platformIndex": 0,
                    "occupiedCells": [0],
                    "structurePoints": 100,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": {
                      "cycleId": "CYC-ENGINE-000001",
                      "startedGameTimeMs": 4500,
                      "durationMs": 1000,
                      "commandType": "engine.turn-right-until-cancel",
                      "isAutoRepeat": true
                    },
                    "cargo": []
                  }
                ]
              }
            ]
          }
        }
        """;

        string savePath = Path.Combine(Path.GetTempPath(), $"dss-save-{Guid.NewGuid():N}.json");
        File.WriteAllText(savePath, saveJson);
        try
        {
            var engine = EngineContentLoader.CreateEngineFromSaveFile(settingsPath, savePath);

            Assert.Equal("SPC-0001", engine.PlayerShipObjectId);
            Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);

            // The cycle was 500 ms into its 1000 ms turn-step window when saved.
            // The next tick at gameTimeMs = 5500 completes it — direction advances by
            // exactly one turn step, and the auto-repeat cycle carries on.
            var next = engine.CaptureSnapshotForTests(5500, SimulationSpeed.Speed1);
            var ship = next.Objects.Single(o => o.ObjectId == "SPC-0001");

            Assert.Equal(91, ship.Direction);
            Assert.Equal(ShipEngineCommandTypes.TurnRightUntilCancel, ship.ActiveEngineCommandType);
        }
        finally
        {
            File.Delete(savePath);
        }
    }

    [Fact]
    public void CreateEngineFromSaveFile_rejects_missing_save_file()
    {
        string settingsPath = ResolveRealSettingsPath();
        string missingPath = Path.Combine(Path.GetTempPath(), $"dss-missing-{Guid.NewGuid():N}.json");

        Assert.Throws<Scenario.ScenarioException>(
            () => EngineContentLoader.CreateEngineFromSaveFile(settingsPath, missingPath));
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
