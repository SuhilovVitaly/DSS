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
    public void Crew_fit_existing_cabins_after_weapon_assignments_are_removed()
    {
        using var engine = Create();
        var saved = ScenarioLoader.LoadFromFile(ScenarioPath);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
        foreach (var ship in saved.GameState.SpaceObjects.Where(o => o.ObjectType is "PlayerShip" or "NpcShip"))
        {
            int expected = ship.ObjectType == "PlayerShip" ? 3 : 2;
            Assert.Equal(expected, ship.Crew!.Count);
            int cabins = ship.Modules!.Sum(m => registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(m.ModuleTypeId)).CabinesCount ?? 0);
            Assert.Equal(expected, cabins);
            Assert.All(ship.Modules!, module => Assert.Null(module.OperatorCrewId));
        }
    }

    [Fact]
    public void Three_crew_consume_three_rations_at_existing_meal_boundary()
    {
        using var engine = Create();
        var snapshot = engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs, simulationTimeMs: 0);
        Assert.Equal(0, snapshot.MissingRations);
        var save = engine.CaptureSaveStateForTests(12 * GameCalendar.HourMs, SimulationSpeed.Speed0, simulationTimeMs: 0);
        var player = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId);
        Assert.Equal(197, player.Modules!.SelectMany(m => m.Cargo ?? []).Single(c => c.ItemTypeId == "item.food-rations").Quantity);
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
    public void Neutral_contact_can_be_clicked_and_information_includes_generated_captain()
    {
        using var engine = Create();
        var save = ScenarioLoader.LoadFromFile(ScenarioPath);
        var target = save.GameState.SpaceObjects.Single(o => o.ObjectType == SpaceObjectType.NpcShip);
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectType == SpaceObjectType.PlayerShip
                    ? o with { PositionX = target.PositionX, PositionY = target.PositionY - 60, SpeedMps = 0, MovementType = "Stationary" } : o).ToArray()
            }
        });
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
        Assert.Equal(PlayerRelation.Neutral, screen.SelectedOrActiveObjectInfo?.RelationToPlayer);
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
