using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatWreckTests
{
    private static AuthoritativeSnapshot DestroyWithPointBlasts(SimulationEngine engine, bool partition = false)
    {
        var scene = At(engine, 0);
        for (int n = 0; n < 12 && scene.Objects.Any(o => o.ObjectId == Target); n++)
        {
            long launch = n * 10000L; scene = At(engine, launch);
            var target = scene.Objects.Single(o => o.ObjectId == Target);
            var owner = scene.Objects.Single(o => o.ObjectId == Player);
            var aim = ModuleCombatEndToEndTests.LinearAim(owner, target, 10);
            engine.ReceiveCommand(LaunchAt("wreck-hit-" + n, aim.X, aim.Y)); scene = At(engine, launch);
            long due = (long)Math.Ceiling(Assert.Single(scene.Objects, o => o.OwnProjectile is not null).OwnProjectile!.DetonationMotionTimeMs);
            if (partition) for (long t = launch + 37; t < due; t += 137) At(engine, t);
            scene = At(engine, due);
        }
        Assert.DoesNotContain(scene.Objects, o => o.ObjectId == Target); return scene;
    }
    [Fact]
    public void Point_destruction_creates_one_stationary_empty_wreck_at_the_physical_ship_pose()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { SpeedMps = 100, DirectionDegrees = 90, MovementType = "Linear" } : o);
        using var whole = Create(scenario); using var split = Create(scenario);
        var a = DestroyWithPointBlasts(whole); var b = DestroyWithPointBlasts(split, true);
        var wreck = Assert.Single(a.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        var other = Assert.Single(b.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        var death = Assert.Single(a.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed);
        Assert.Equal(death.MotionTimeMs / 1000, wreck.X, 6); Assert.Equal(-100, wreck.Y, 6);
        Assert.Equal(wreck, other); Assert.Equal(0, wreck.SpeedKmS); Assert.Equal(0, wreck.Direction);
        var runtime = whole.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == wreck.ObjectId);
        Assert.Equal("Permanent", runtime.PersistenceType); Assert.Empty(runtime.Modules); Assert.True(runtime.Crew.IsDefaultOrEmpty);
        Assert.True(runtime.Inventory.IsDefaultOrEmpty); Assert.True(runtime.Passengers.IsDefaultOrEmpty); Assert.Null(runtime.HullLayout);
    }
    [Fact]
    public void Destruction_clears_selection_and_navigation_and_the_world_keeps_running()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Player ? o with { SpeedMps = 50, MovementType = "Linear" } : o));
        engine.ReceiveCommand(new("approach", 1, Player, "MOD-PLAYER-ENGINE-01", NavigationComputerCommandTypes.Approach, Target)); At(engine, 0);
        engine.SetObjectInteractionState(Target, Target);
        var result = DestroyWithPointBlasts(engine); Assert.Null(result.SelectedObjectId); Assert.Null(result.ActiveObjectId);
        Assert.Null(result.Objects.Single(o => o.ObjectId == Player).NavigationTargetObjectId);
        Assert.DoesNotContain(engine.RuntimeObjects.SelectMany(o => o.Modules), m => m.ActiveCycle?.TargetObjectId == Target);
        var later = At(engine, result.MotionTimeMs + 10000); Assert.True(later.GameTimeMs > result.GameTimeMs);
        Assert.Equal(SessionOutcomeState.Running, later.EffectiveOutcome.State);
        Assert.Single(later.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
    }
    [Fact]
    public void Wreck_survives_real_file_load_without_replaying_point_blast_and_ignores_followup_blast()
    {
        using var engine = Create(); var result = DestroyWithPointBlasts(engine);
        var wreck = Assert.Single(result.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        long time = result.MotionTimeMs + 10000; var save = engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0);
        string path = Path.Combine(Path.GetTempPath(), "dss-wreck-" + Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save)); using var loaded = SimulationEngine.CreateFromSaveFile(Path.Combine(ClientRoot, "Settings.json"), path);
            var before = At(loaded, time); Assert.Empty(before.CombatImpacts); Assert.Contains(before.Objects, o => o.ObjectId == wreck.ObjectId);
            int hits = before.CombatJournal.Count(e => e.Type == CombatEventType.ModuleHit);
            loaded.ReceiveCommand(LaunchAt("wreck-followup", wreck.X, wreck.Y)); var launch = At(loaded, time);
            long due = (long)Math.Ceiling(Assert.Single(launch.Objects, o => o.OwnProjectile is not null).OwnProjectile!.DetonationMotionTimeMs);
            var after = At(loaded, due); Assert.Equal(hits, after.CombatJournal.Count(e => e.Type == CombatEventType.ModuleHit));
            Assert.Equal(wreck, after.Objects.Single(o => o.ObjectId == wreck.ObjectId));
            Assert.Single(after.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed);
        }
        finally { File.Delete(path); }
    }
    [Theory]
    [InlineData("speed")]
    [InlineData("heading")]
    [InlineData("movement")]
    [InlineData("temporary")]
    [InlineData("modules")]
    [InlineData("crew")]
    [InlineData("cargo")]
    [InlineData("missile")]
    public void Loader_rejects_invalid_wreck_and_unbacked_missile(string kind)
    {
        using var engine = Create();
        var result = DestroyWithPointBlasts(engine);
        var save = engine.CaptureSaveStateForTests(result.MotionTimeMs, SimulationSpeed.Speed0, result.MotionTimeMs);
        Assert.NotNull(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, isSave: true));
        var wreck = save.GameState.SpaceObjects.Single(o => o.ObjectType == SpaceObjectType.Wreck);
        var invalid = kind switch
        {
            "speed" => wreck with { SpeedMps = 1 },
            "heading" => wreck with { DirectionDegrees = 1 },
            "movement" => wreck with { MovementType = "Linear" },
            "temporary" => wreck with { PersistenceType = "Temporary" },
            "modules" => wreck with { Modules = save.GameState.SpaceObjects.Single(o => o.ObjectId == Player).Modules },
            "crew" => wreck with { Crew = [new("crew", "Crew")] },
            "cargo" => wreck with { Inventory = [new("item.ice", 1)] },
            _ => wreck with { ObjectType = SpaceObjectType.Missile, RelationToPlayer = null }
        };
        var bad = save with
        {
            GameState = save.GameState with
            { SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == wreck.ObjectId ? invalid : o).ToArray() }
        };
        var error = Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(bad), true));
        Assert.Contains(kind == "missile" ? "missile" : "wreck", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("wreck-1")]
    [InlineData("WRECK-1")]
    public void Wreck_id_skips_existing_objects(string occupiedId)
    {
        using var engine = Create(TorpedoImpactTests.Scenario(null, Obstacle(occupiedId, 1000, 1000)));
        var snapshot = DestroyWithPointBlasts(engine);
        Assert.Equal("wreck-2", Assert.Single(snapshot.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck)).ObjectId);
        Assert.Equal(snapshot.Objects.Length, snapshot.Objects.Select(o => o.ObjectId).Distinct().Count());
    }


    [Fact]
    public void Destruction_does_not_shift_station_market_boundary_commits()
    {
        var market = Obstacle("market", 1000, 1000) with { ObjectType = SpaceObjectType.Station, MassKg = null, CompositionType = null, MarketProfileId = "market.mining" };
        using var battle = Create(TorpedoImpactTests.Scenario(null, market)); using var control = Create(TorpedoImpactTests.Scenario(null, market));
        var dead = DestroyWithPointBlasts(battle); At(control, dead.MotionTimeMs);
        At(battle, dead.MotionTimeMs + 10000, GameCalendar.HourMs); At(control, dead.MotionTimeMs + 10000, GameCalendar.HourMs);
        var a = battle.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "market"); var b = control.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "market");
        Assert.True(a.MarketRevision > 1); Assert.Equal(b.MarketRevision, a.MarketRevision);
        Assert.Equal(b.MarketBudgetCredits, a.MarketBudgetCredits); Assert.Equal(b.Inventory.ToArray(), a.Inventory.ToArray());
    }
}
