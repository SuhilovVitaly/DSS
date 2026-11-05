using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SaveFixturesTests
{
    internal static string ClientRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
    internal static string Fixtures => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Fixtures/Saves"));
    internal static GameDataRegistry Registry() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _);

    [Theory]
    [InlineData("Default-Save18.json", 18, false)]
    [InlineData("Default-Save18-combat.json", 18, true)]
    [InlineData("Default-Save19.json", 19, false)]
    [InlineData("Default-Save19-combat.json", 19, true)]
    public void HistoricalWriterFixturesMigrateAndContinueDeterministically(string name, int version, bool active)
    {
        string path = Path.Combine(Fixtures, name);
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(version, json.RootElement.GetProperty("saveFormatVersion").GetInt32());
        Assert.Equal(active ? 3 : 0, json.RootElement.GetProperty("gameState").GetProperty("pointCombatState").GetProperty("Projectiles").GetArrayLength());
        var saved = ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true);
        Assert.Equal(SaveFormat.CurrentSaveFormatVersion, saved.SaveFormatVersion);
        Assert.Equal("quicksave", saved.Metadata.ScenarioId);
        Assert.NotNull(saved.GameState.CatalogCompatibility);
        Assert.Null(saved.GameState.PointCombatState);
        Assert.Null(saved.GameState.DefenseState);
        Assert.Null(saved.GameState.HostileCombatState);
        Assert.Null(saved.GameState.CombatReportsState);
        Assert.DoesNotContain(saved.GameState.SpaceObjects, o => o.ObjectId == "SPC-MUKHA-01");
        var registry = Registry();
        using var a = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        using var b = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        a.LoadScenario(saved, isSave: true);
        b.LoadScenario(saved, isSave: true);
        long world = saved.GameState.GameTimeMs + 1000, motion = saved.GameState.MotionTimeMs + 100;
        a.CaptureSnapshotForTests(world, SimulationSpeed.Speed0, motion);
        b.CaptureSnapshotForTests(world, SimulationSpeed.Speed0, motion);
        Assert.Equal(ScenarioLoader.Serialize(a.CaptureSaveStateForTests(world, SimulationSpeed.Speed0, motion)),
            ScenarioLoader.Serialize(b.CaptureSaveStateForTests(world, SimulationSpeed.Speed0, motion)));
    }
}
