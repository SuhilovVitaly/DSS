using System.Text.Json;
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

        // Produce a real current-format save halfway through an actual engine turn cycle.
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        using var source = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        source.LoadScenario(TorpedoImpactTests.Scenario(o => o.ObjectId == TorpedoImpactTests.Player
            ? o with { DirectionDegrees = 90 } : o));
        source.ReceiveCommand(new PlayerCommand("turn", 1, TorpedoImpactTests.Player, "MOD-PLAYER-ENGINE-01",
            ShipEngineCommandTypes.TurnRightUntilCancel));
        source.CaptureSnapshotForTests(0);
        string saveJson = Scenario.ScenarioLoader.Serialize(source.CaptureSaveStateForTests(500, SimulationSpeed.Speed0));

        string savePath = Path.Combine(Path.GetTempPath(), $"dss-save-{Guid.NewGuid():N}.json");
        File.WriteAllText(savePath, saveJson);
        try
        {
            using var engine = EngineContentLoader.CreateEngineFromSaveFile(settingsPath, savePath);

            Assert.Equal("SPC-0001", engine.PlayerShipObjectId);
            Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);

            // The cycle was 500 ms into its 1000 ms turn-step window when saved.
            // The next tick at motionTimeMs = 1000 completes it — direction advances by
            // exactly one turn step, and the auto-repeat cycle carries on.
            var next = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);
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
