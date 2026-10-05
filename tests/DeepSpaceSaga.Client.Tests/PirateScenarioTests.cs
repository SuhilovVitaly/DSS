using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PirateScenarioTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static readonly string SettingsPath = Path.Combine(ClientRoot, "Settings.json");
    private static readonly string ScenarioPath = Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json");
    private static SimulationEngine Create() => EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, ScenarioPath);
    private static ObjectMotionSnapshot Pirate(AuthoritativeSnapshot snapshot) => Assert.Single(snapshot.Objects, o => o.ObjectType == SpaceObjectType.NpcShip);

    [Fact]
    public void Two_distinct_operators_fit_two_cabins()
    {
        using var engine = Create();
        var saved = engine.CaptureSaveState();
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
        foreach (var ship in saved.GameState.SpaceObjects.Where(o => o.ObjectType is "PlayerShip" or "NpcShip"))
        {
            Assert.Equal(2, ship.Crew!.Count);
            int cabins = ship.Modules!.Sum(m => registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(m.ModuleTypeId)).CabinesCount ?? 0);
            Assert.Equal(2, cabins);
            var assignments = ship.Modules!.Where(m => m.OperatorCrewId is not null).Select(m => m.OperatorCrewId).ToArray();
            Assert.Equal(2, assignments.Distinct().Count());
            Assert.All(assignments, id => Assert.Contains(ship.Crew, c => c.CrewId == id));
        }
    }

    [Fact]
    public void Both_ships_have_defense_in_new_room()
    {
        var scenario = ScenarioLoader.LoadFromFile(ScenarioPath);
        foreach (var ship in scenario.GameState.SpaceObjects)
        {
            var module = Assert.Single(ship.Modules!, m => m.ModuleTypeId == "module.countermeasure.launcher.basic");
            Assert.Equal(new HullCellCoordinate(5, 2), Assert.Single(module.OccupiedCells));
            Assert.True(module.AutoDefenseEnabled);
        }
    }
    [Fact]
    public void Pirate_motion_and_identity_unchanged()
    {
        Assert.Contains(ScenarioRepository.ListScenarios(Path.Combine(ClientRoot, "Scenarios")), s => s.ScenarioPath == ScenarioPath);
        using var engine = Create();
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Equal(2, snapshot.Objects.Count(o => o.ObjectType is "PlayerShip" or "NpcShip"));
        Assert.All(snapshot.Objects.Where(o => o.ObjectType is "PlayerShip" or "NpcShip"), o => Assert.Equal(new HullCombatSnapshot("ship.tetrarch", 450, 450), o.HullCombat));
        Assert.Equal(new LauncherCombatSnapshot(null, 3, 90, 150),
            Assert.Single(snapshot.InstalledModules, m => m.LauncherCombat is not null).LauncherCombat! with { Operator = null });
        var pirate = Pirate(snapshot);
        var playerPose = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
        Assert.Equal(-10000, pirate.X - playerPose.X, 6);
        Assert.Equal(-5000, pirate.Y - playerPose.Y, 6);
        Assert.Equal((0.4d, 120d), (pirate.SpeedKmS, pirate.Direction));
        Assert.Equal(PlayerRelation.Enemy, pirate.RelationToPlayer);
        Assert.Equal(SpaceObjectType.NpcShip, pirate.RenderObjectType);
        Assert.Equal(new SKColor(139, 0, 0), SpaceMapColorResolver.GetColor(pirate.RenderObjectType, pirate.RelationToPlayer));
        Assert.False(string.IsNullOrWhiteSpace(pirate.DisplayName));
        Assert.False(string.IsNullOrWhiteSpace(pirate.CaptainDisplayName));
        Assert.False(string.IsNullOrWhiteSpace(pirate.CaptainPortraitImage));
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0).GameState;
        var player = save.SpaceObjects.Single(o => o.ObjectId == save.PlayerShipObjectId);
        var npc = save.SpaceObjects.Single(o => o.ObjectId == pirate.ObjectId);
        Assert.Equal(JsonSerializer.Serialize(player.HullLayout), JsonSerializer.Serialize(npc.HullLayout));
        Assert.Equal(player.Image, npc.Image);
        Assert.Equal(player.Modules!.Select(m => m.ModuleTypeId), npc.Modules!.Select(m => m.ModuleTypeId));
        Assert.Equal(JsonSerializer.Serialize(player.Modules!.Select(m => m with { ModuleId = "test", OperatorCrewId = m.OperatorCrewId is null ? null : m.ModuleTypeId })),
            JsonSerializer.Serialize(npc.Modules!.Select(m => m with { ModuleId = "test", OperatorCrewId = m.OperatorCrewId is null ? null : m.ModuleTypeId })));
        var moved = Pirate(engine.CaptureSnapshotForTests(1000));
        Assert.Equal(3.4641016151377544, moved.X - pirate.X, 6);
        Assert.Equal(2d, moved.Y - pirate.Y, 6);
        Assert.Equal((0.4d, 120d), (moved.SpeedKmS, moved.Direction));
        Assert.Null(moved.ActiveEngineCommandType);
    }

    [Fact]
    public void New_games_get_fresh_seeds_and_seeded_names_are_repeatable_and_varied()
    {
        using var engine = Create();
        using var another = Create();
        Assert.NotEqual(engine.CaptureSaveState().GameState.MasterSeed, another.CaptureSaveState().GameState.MasterSeed);
        var source = ScenarioLoader.LoadFromFile(ScenarioPath);
        var names = new HashSet<string>();
        var captains = new HashSet<string>();
        for (ulong seed = 0; seed < 16; seed++)
        {
            var seeded = source with { GameState = source.GameState with { MasterSeed = seed } };
            engine.LoadScenario(seeded);
            var first = Pirate(engine.CaptureSnapshotForTests());
            engine.LoadScenario(seeded);
            var second = Pirate(engine.CaptureSnapshotForTests());
            Assert.Equal(first.DisplayName, second.DisplayName);
            Assert.Equal(first.CaptainDisplayName, second.CaptainDisplayName);
            names.Add(first.DisplayName!);
            captains.Add(first.CaptainDisplayName!);
        }
        Assert.True(names.Count > 1);
        Assert.True(captains.Count > 1);
    }

    [Fact]
    public void Save_load_preserves_identity_relation_modules_and_motion()
    {
        using var engine = Create();
        var pirate = Pirate(engine.CaptureSnapshotForTests());
        engine.SetObjectInteractionState(null, pirate.ObjectId);
        var save = engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0);
        var path = Path.Combine(Path.GetTempPath(), $"pirate-save-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(save));
            using var restored = EngineContentLoader.CreateEngineFromSaveFile(SettingsPath, path);
            var actual = Pirate(restored.CaptureSnapshotForTests(2000));
            var expected = Pirate(engine.CaptureSnapshotForTests(2000));
            Assert.Equal(expected, actual);
            restored.SetObjectInteractionState(null, pirate.ObjectId);
            Assert.Equal(pirate.ObjectId, restored.CaptureSnapshotForTests(2000).SelectedObjectId);
            Assert.Equal(JsonSerializer.Serialize(save.GameState.SpaceObjects.Single(o => o.ObjectId == pirate.ObjectId).Modules),
                JsonSerializer.Serialize(restored.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0).GameState.SpaceObjects.Single(o => o.ObjectId == pirate.ObjectId).Modules));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Pirate_can_be_clicked_and_information_includes_generated_captain()
    {
        using var engine = Create();
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var target = save.GameState.SpaceObjects.Single(o => o.ObjectType == SpaceObjectType.NpcShip);
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectType == SpaceObjectType.PlayerShip
                    ? o with { PositionX = target.PositionX, PositionY = target.PositionY - 60, SpeedMps = 0, MovementType = "Stationary" } : o).ToArray()
            }
        }, isSave: true);
        var snapshot = engine.CaptureSnapshotForTests();
        var buffer = new SnapshotBuffer();
        buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        screen.OnMouseDown(640, 420);
        var pirate = Pirate(snapshot);
        Assert.Equal(pirate.ObjectId, screen.SelectedObjectId);
        Assert.Equal(PlayerRelation.Enemy, screen.SelectedOrActiveObjectInfo?.RelationToPlayer);
        var lines = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
        Assert.Contains(("Name", pirate.DisplayName!), lines);
        Assert.Contains(("Captain", pirate.CaptainDisplayName!), lines);
        Assert.Contains(("Direction", "120°"), lines);
    }

    [Fact]
    public void Unknown_npc_masks_identity_and_invalid_relations_are_rejected()
    {
        using var engine = Create();
        var source = ScenarioLoader.LoadFromFile(ScenarioPath);
        var hidden = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectType == SpaceObjectType.NpcShip ? o with { IsKnown = false } : o).ToArray()
            }
        };
        engine.LoadScenario(hidden);
        var pirate = engine.CaptureSnapshotForTests().Objects.Single(o => o.ObjectId == "SPC-0002");
        Assert.Null(pirate.CaptainDisplayName);
        Assert.Null(pirate.CaptainPortraitImage);
        Assert.Null(pirate.DisplayName);
        Assert.Null(pirate.RelationToPlayer);
        Assert.Null(pirate.HullCombat);
        Assert.Equal(SpaceObjectType.UnknownSpaceObject, pirate.RenderObjectType);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with { RelationToPlayer = PlayerRelation.Self }).ToArray()
            }
        }));
    }
}
