using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatWreckTests
{
    private static AuthoritativeSnapshot ThreeHits(SimulationEngine engine, bool partition = false)
    {
        AuthoritativeSnapshot snapshot = At(engine, 0);
        for (int n = 0; n < 3; n++)
        {
            engine.ReceiveCommand(Fire("hit-" + n));
            At(engine, n * 5000);
            if (partition)
                for (long t = n * 5000 + 37; t < (n + 1) * 5000; t += 137) At(engine, t);
            snapshot = At(engine, (n + 1) * 5000);
        }
        return snapshot;
    }

    [Fact]
    public void Third_hit_creates_one_new_stationary_wreck()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { SpeedMps = 100, DirectionDegrees = 90, MovementType = "Linear" } : o);
        using var whole = Create(scenario);
        using var split = Create(scenario);
        var result = ThreeHits(whole);
        var partitioned = ThreeHits(split, true);
        var wreck = Assert.Single(result.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        var other = Assert.Single(partitioned.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        var hit = result.CombatImpacts[^1];
        Assert.Equal(Target, hit.DestroyedObjectId);
        Assert.Equal(wreck.ObjectId, hit.WreckObjectId);
        Assert.NotEqual(Target, wreck.ObjectId);
        Assert.Equal(other.ObjectId, wreck.ObjectId);
        Assert.InRange(Math.Abs(other.X - wreck.X) + Math.Abs(other.Y - wreck.Y), 0, 3e-7);
        Assert.Equal(hit.MotionTimeMs / 1000, wreck.X, 6);
        Assert.Equal(-100, wreck.Y, 6);
        Assert.Equal(0, wreck.SpeedKmS);
        Assert.Equal(0, wreck.Direction);
        Assert.Null(wreck.HullCombat);
        Assert.DoesNotContain(result.Objects, o => o.ObjectId == Target || o.Torpedo is not null);
        var runtime = whole.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == wreck.ObjectId);
        Assert.Equal("Permanent", runtime.PersistenceType);
        Assert.Empty(runtime.Modules);
        Assert.True(runtime.Crew.IsDefaultOrEmpty);
        Assert.True(runtime.Inventory.IsDefaultOrEmpty);
        Assert.True(runtime.Passengers.IsDefaultOrEmpty);
        Assert.Null(runtime.HullLayout);
    }

    [Fact]
    public void Destroyed_target_references_are_cleared()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Player ? o with { SpeedMps = 50, MovementType = "Linear" } : o));
        engine.ReceiveCommand(new("approach", 1, Player, "MOD-PLAYER-ENGINE-01", NavigationComputerCommandTypes.Approach, Target));
        var navigating = At(engine, 0);
        Assert.Equal(Target, navigating.Objects.Single(o => o.ObjectId == Player).NavigationTargetObjectId);
        engine.SetObjectInteractionState(Target, Target);
        var result = ThreeHits(engine);
        Assert.Null(result.SelectedObjectId);
        Assert.Null(result.ActiveObjectId);
        Assert.Null(result.Objects.Single(o => o.ObjectId == Player).NavigationTargetObjectId);
        Assert.DoesNotContain(engine.RuntimeObjects.SelectMany(o => o.Modules), m => m.ActiveCycle?.TargetObjectId == Target);
        var wreck = Assert.Single(result.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        engine.SetObjectInteractionState(wreck.ObjectId, wreck.ObjectId);
        Assert.Equal(wreck.ObjectId, At(engine, 15000).SelectedObjectId);
    }

    [Fact]
    public void Wreck_is_invulnerable_to_followup_torpedo()
    {
        using var engine = Create();
        var third = ThreeHits(engine);
        var wreck = Assert.Single(third.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        engine.ReceiveCommand(Fire("wreck-shot", wreck.ObjectId));
        Assert.Single(At(engine, 15000).Objects.Where(o => o.Torpedo is not null));
        var after = At(engine, 19000);
        Assert.Equal(4, after.CombatImpacts.Length);
        var hit = after.CombatImpacts[^1];
        Assert.Equal(wreck.ObjectId, hit.HitObjectId);
        Assert.Equal(0, hit.DamageApplied);
        Assert.Null(hit.DestroyedObjectId);
        Assert.Null(hit.WreckObjectId);
        Assert.Equal(wreck, Assert.Single(after.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck)));
        Assert.Null(after.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);
    }

    [Fact]
    public void World_continues_after_pirate_destruction()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Player ? o with { SpeedMps = 50, MovementType = "Linear" } : o));
        var dead = ThreeHits(engine);
        var later = At(engine, 30000);
        Assert.True(later.GameTimeMs > dead.GameTimeMs);
        Assert.NotEqual(dead.Objects.Single(o => o.ObjectId == Player).Y, later.Objects.Single(o => o.ObjectId == Player).Y);
        Assert.Equal(3, later.CombatImpacts.Length);
        Assert.Single(later.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        Assert.Equal(450, later.Objects.Single(o => o.ObjectId == Player).HullCombat!.CurrentHp);
    }

    [Fact]
    public void Wreck_survives_file_load_without_replaying_impacts()
    {
        using var engine = Create();
        var result = ThreeHits(engine);
        string wreckId = result.CombatImpacts[^1].WreckObjectId!;
        var save = engine.CaptureSaveStateForTests(15000, SimulationSpeed.Speed0);
        var wreck = save.GameState.SpaceObjects.Single(o => o.ObjectId == wreckId);
        Assert.Equal("Stationary", wreck.MovementType);
        string path = Path.Combine(Path.GetTempPath(), "dss-wreck-" + Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            using var loaded = SimulationEngine.CreateFromSaveFile(Path.Combine(ClientRoot, "Settings.json"), path);
            var snapshot = At(loaded, 15000);
            Assert.Empty(snapshot.CombatImpacts);
            Assert.Single(snapshot.Objects.Where(o => o.ObjectId == wreckId));
            Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == Target);
            loaded.ReceiveCommand(Fire("after-load", wreckId));
            At(loaded, 15000);
            var hit = Assert.Single(At(loaded, 19000).CombatImpacts);
            Assert.Equal(wreckId, hit.HitObjectId);
            Assert.Equal(0, hit.DamageApplied);
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
        ThreeHits(engine);
        var save = engine.CaptureSaveStateForTests(15000, SimulationSpeed.Speed0);
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
            _ => wreck with { ObjectType = SpaceObjectType.Missile }
        };
        var bad = save with
        {
            GameState = save.GameState with
            { SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == wreck.ObjectId ? invalid : o).ToArray() }
        };
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(bad), true));
    }

    [Theory]
    [InlineData("wreck-1")]
    [InlineData("WRECK-1")]
    public void Wreck_id_skips_existing_objects(string occupiedId)
    {
        using var engine = Create(TorpedoImpactTests.Scenario(null, Obstacle(occupiedId, 1000, 1000)));
        var snapshot = ThreeHits(engine);
        Assert.Equal("wreck-2", Assert.Single(snapshot.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck)).ObjectId);
        Assert.Equal(snapshot.Objects.Length, snapshot.Objects.Select(o => o.ObjectId).Distinct().Count());
    }

    [Fact]
    public void Destruction_does_not_shift_station_market_boundary_commits()
    {
        var market = Obstacle("market", 1000, 1000) with
        {
            ObjectType = SpaceObjectType.Station,
            MassKg = null,
            CompositionType = null,
            MarketProfileId = "market.mining"
        };
        using var battle = Create(TorpedoImpactTests.Scenario(null, market));
        using var control = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { ShipClassId = null } : o, market));
        foreach (var engine in new[] { battle, control })
        {
            for (int n = 0; n < 3; n++)
            {
                engine.ReceiveCommand(Fire("shot-" + n));
                At(engine, n * 5000);
                At(engine, (n + 1) * 5000, n == 2 ? GameCalendar.HourMs : (n + 1) * 5000);
            }
        }
        var expected = control.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "market");
        var actual = battle.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "market");
        Assert.True(expected.MarketRevision > 1);
        Assert.Equal(expected.MarketRevision, actual.MarketRevision);
        Assert.Equal(expected.MarketBudgetCredits, actual.MarketBudgetCredits);
        Assert.Equal(expected.Inventory.ToArray(), actual.Inventory.ToArray());
        Assert.Single(battle.RuntimeObjects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
    }

    [Fact]
    public void Original_scenario_three_real_launches_destroy_moving_pirate()
    {
        var scenario = WithoutDefense(ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json")));
        using var engine = Create(scenario);
        long time = 0;
        for (int n = 0; n < 3; n++)
        {
            engine.ReceiveCommand(Fire("real-" + n));
            var launch = At(engine, time);
            var torpedo = Assert.Single(launch.Objects.Where(o => o.Torpedo is not null));
            time = torpedo.Torpedo!.PredictedImpactMotionTimeMs!.Value + 1000;
            var result = At(engine, time);
            Assert.Equal(n + 1, result.CombatImpacts.Length);
            Assert.Equal(150, result.CombatImpacts[^1].DamageApplied);
        }
        var final = At(engine, time);
        Assert.DoesNotContain(final.Objects, o => o.ObjectId == Target);
        Assert.Single(final.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        Assert.Equal(3, final.CombatImpacts.Length);
    }
}
