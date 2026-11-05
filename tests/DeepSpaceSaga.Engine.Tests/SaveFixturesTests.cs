using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SaveFixturesTests
{
    private static string ClientRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
    private static string Fixtures => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Fixtures/Saves"));
    private static GameDataRegistry Registry() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _);

    [Fact]
    public void RecordPreRemovalFixturesWhenExplicitlyRequested()
    {
        string? directory = Environment.GetEnvironmentVariable("DSS_RECORD_SAVE_FIXTURES");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathFullyQualified(directory)) throw new InvalidOperationException("Fixture output must be absolute.");
        Directory.CreateDirectory(directory);
        var registry = Registry();
        var source = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "Default", "scenario.json"));
        source = source with { GameState = source.GameState with { MasterSeed = 42 } };
        using var idle = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        idle.LoadScenario(source);
        var idleSave = idle.CaptureSaveState();
        Assert.Equal(19, idleSave.SaveFormatVersion);
        Assert.Empty(idleSave.GameState.PointCombatState!.Projectiles);

        var hostileSource = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "CombatMukha", "scenario.json"));
        var player = source.GameState.SpaceObjects.Single(o => o.ObjectId == source.GameState.PlayerShipObjectId);
        var enemy = hostileSource.GameState.SpaceObjects.First(o => o.ObjectType == SpaceObjectType.NpcShip) with
        {
            PositionX = player.PositionX + 400,
            PositionY = player.PositionY - 250
        };
        var combatSource = source with
        {
            GameState = source.GameState with { SpaceObjects = source.GameState.SpaceObjects.Append(enemy).ToArray() }
        };
        using var combat = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        combat.LoadScenario(combatSource);
        combat.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        combat.ReceiveCommand(new("fixture-torpedo", 1, player.ObjectId, "MOD-PLAYER-TORPEDO-01", CombatCommandTypes.Launch,
            TargetWorldX: player.PositionX + 500, TargetWorldY: player.PositionY, ProfileId: "medium"));
        combat.ReceiveCommand(new("fixture-defense", 2, player.ObjectId, "MOD-PLAYER-COUNTERMEASURE-01", DefenseCommandTypes.Launch,
            TargetWorldX: player.PositionX + 150, TargetWorldY: player.PositionY + 30, ProfileId: "wide"));
        var snapshot = combat.CaptureSnapshotForTests(1000, SimulationSpeed.Speed0, 100);
        Assert.Equal(2, snapshot.CommandResults.Length);
        Assert.All(snapshot.CommandResults, command => Assert.Equal(CommandResultStatus.Executed, command.Status));
        var activeSave = combat.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0, 100);
        AssertActive(activeSave);

        Write("Default-Save19.json", idleSave);
        Write("Default-Save19-combat.json", activeSave);
        Write("Default-Save18.json", Project18(idleSave));
        Write("Default-Save18-combat.json", Project18(activeSave));

        void Write(string name, ScenarioFile data) => File.WriteAllText(Path.Combine(directory, name), ScenarioLoader.Serialize(data));
    }

    [Theory]
    [InlineData("Default-Save18.json", 18, false)]
    [InlineData("Default-Save18-combat.json", 18, true)]
    [InlineData("Default-Save19.json", 19, false)]
    [InlineData("Default-Save19-combat.json", 19, true)]
    public void HistoricalWriterFixturesLoadAndContinueBeforeRemoval(string name, int version, bool active)
    {
        string path = Path.Combine(Fixtures, name);
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(version, json.RootElement.GetProperty("saveFormatVersion").GetInt32());
        var saved = ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true);
        Assert.Equal(19, saved.SaveFormatVersion);
        Assert.Equal("quicksave", saved.Metadata.ScenarioId);
        Assert.NotNull(saved.GameState.CatalogCompatibility);
        if (active) AssertActive(saved);
        else Assert.Empty(saved.GameState.PointCombatState!.Projectiles);
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

    private static void AssertActive(ScenarioFile saved)
    {
        Assert.NotNull(saved.GameState.PointCombatState);
        Assert.Contains(saved.GameState.PointCombatState.Projectiles, p => p.Flight.Kind == ProjectileKind.Torpedo);
        Assert.Contains(saved.GameState.PointCombatState.Projectiles, p => p.Flight.Kind == ProjectileKind.Countermissile);
        Assert.NotNull(saved.GameState.DefenseState);
        Assert.NotEmpty(saved.GameState.HostileCombatState!.Actors);
        Assert.NotEmpty(saved.GameState.CombatReportsState!.ReportedRangeTargetIds);
    }

    private static ScenarioFile Project18(ScenarioFile save) => save with
    {
        SaveFormatVersion = 18,
        GameState = save.GameState with
        {
            DebugTuning = null,
            PointCombatState = save.GameState.PointCombatState! with
            {
                Projectiles = save.GameState.PointCombatState.Projectiles.Select(p =>
                    p with { LauncherModuleTypeId = null, OriginalFlight = null }).ToArray()
            }
        }
    };
}
