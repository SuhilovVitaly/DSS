using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class CombatSnapshotTests
{
    [Fact]
    public void Weapon_parameters_roundtrip()
    {
        var launcher = new LauncherCombatSnapshot("torpedo", 3, 90, 150,
            RangeKm: 200.25, Maneuverability: 5.125m);
        var flight = new TorpedoSnapshot("owner", "launcher", "target", 100,
            3, 90, 150, 0, new(100, 1, TorpedoRoutePhase.Straight, false),
            CapturedManeuverability: 5.125m, OwnerPlayerRelationAtLaunch: PlayerRelation.Enemy);
        var restoredLauncher = JsonSerializer.Deserialize<LauncherCombatSnapshot>(JsonSerializer.Serialize(launcher));
        var restoredFlight = JsonSerializer.Deserialize<TorpedoSnapshot>(JsonSerializer.Serialize(flight))!;
        Assert.Equal(launcher, restoredLauncher);
        Assert.Equal(5.125m, restoredFlight.CapturedManeuverability);
        Assert.Equal(PlayerRelation.Enemy, restoredFlight.OwnerPlayerRelationAtLaunch);
        Assert.Equal(90, restoredFlight.TurnRateDegPerSec);
        Assert.Equal(100, restoredFlight.HitChancePercent);

        // Journal facts retain the removed launcher's identity and distinguish expiry from a miss.
        var entry = new CombatJournalEntry(1, 60100, CombatEventType.Expired,
            "owner", "target", "countermeasure", 10, 20, ChanceTenths: 499,
            LauncherModuleId: "defense-2", LaunchMode: LaunchMode.Manual,
            Accuracy: 55m, TargetManeuverability: 5.125m,
            TerminationKind: CountermeasureTerminationKind.LifetimeExpired);
        var restoredEntry = JsonSerializer.Deserialize<CombatJournalEntry>(JsonSerializer.Serialize(entry))!;
        Assert.Equal(entry, restoredEntry);
        Assert.Null(restoredEntry.Roll);
        Assert.Null(restoredEntry.Damage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("torpedo-1")]
    public void CombatSnapshot_roundtrips_route_trail_and_launcher(string? activeTorpedoObjectId)
    {
        var turn = new TorpedoRouteSegment(-10, 20, 0, 3, -90, 500.25);
        var straight = new TorpedoRouteSegment(-20, 10, 314.9775, 3, 0, 1500.75);
        var route = new TorpedoRoute(1200, 2, TorpedoRoutePhase.Straight, true,
            ImmutableArray.Create(turn, straight), ElapsedMs: 600.5);
        var trail = ImmutableArray.Create(
            new TrailSegment(1000, new TorpedoRouteSegment(0, 20, 270, 3, 0, 200), 1),
            new TrailSegment(1200, turn, 2),
            new TrailSegment(1700.25, straight with { DurationMs = 100.25 }, 2));
        var torpedo = new TorpedoSnapshot("ship-1", "launcher-1", "ship-2", 1000,
            3, 90, 150, 24.015, route, trail, PredictedImpactMotionTimeMs: 3201);
        var hull = new HullCombatSnapshot("ship.tetrarch", 300, 450);
        var launcher = new LauncherCombatSnapshot(activeTorpedoObjectId, 3, 90, 150);
        var snapshot = new AuthoritativeSnapshot(7, 987654321, SimulationSpeed.Speed0,
            ImmutableArray.Create(
                new ObjectMotionSnapshot("ship-1", 0, 0, 0, 0, HullCombat: hull),
                new ObjectMotionSnapshot("torpedo-1", -22, 8, 3, 314.9775,
                    ObjectType: SpaceObjectType.Missile, Torpedo: torpedo),
                new ObjectMotionSnapshot("wreck-1", 100, 200, 0, 0, ObjectType: SpaceObjectType.Wreck)),
            InstalledModules: ImmutableArray.Create(new InstalledModuleSnapshot(
                "launcher-1", "module.torpedo.launcher.basic", "Launcher", 10,
                ImmutableArray.Create(CombatCommandTypes.Fire), LauncherCombat: launcher)),
            SimulationTimeMs: 1800);

        var json = JsonSerializer.Serialize(snapshot);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(restored);
        Assert.Equal(1800, restored.MotionTimeMs);
        Assert.Equal(987654321, restored.GameTimeMs);
        Assert.Equal(hull, restored.Objects[0].HullCombat);
        Assert.Equal("Wreck", restored.Objects[2].ObjectType);
        Assert.Equal("Missile", restored.Objects[1].ObjectType);
        var restoredTorpedo = Assert.IsType<TorpedoSnapshot>(restored.Objects[1].Torpedo);
        Assert.Equal("ship-1", restoredTorpedo.OwnerObjectId);
        Assert.Equal("launcher-1", restoredTorpedo.LauncherModuleId);
        Assert.Equal("ship-2", restoredTorpedo.TargetObjectId);
        Assert.Equal(1000, restoredTorpedo.LaunchMotionTimeMs);
        Assert.Equal(3201, restoredTorpedo.PredictedImpactMotionTimeMs);
        Assert.Equal(24.015, restoredTorpedo.DistanceTravelledWorldUnits);
        Assert.Equal(100, restoredTorpedo.HitChancePercent);
        Assert.Equal(1200, restoredTorpedo.Route.StartMotionTimeMs);
        Assert.Equal(2, restoredTorpedo.Route.PlannerVersion);
        Assert.Equal(TorpedoRoutePhase.Straight, restoredTorpedo.Route.Phase);
        Assert.True(restoredTorpedo.Route.HasIntercept);
        Assert.Equal(600.5, restoredTorpedo.Route.ElapsedMs);
        Assert.Equal(route.Segments.ToArray(), restoredTorpedo.Route.Segments.ToArray());
        Assert.Equal(trail.ToArray(), restoredTorpedo.Trail.ToArray());
        Assert.Equal(launcher, restored.InstalledModules[0].LauncherCombat);
        Assert.Equal("torpedo.fire", Assert.Single(restored.InstalledModules[0].CommandTypeIds));
        Assert.Equal(json, JsonSerializer.Serialize(restored));
    }

    [Fact]
    public void Legacy_snapshot_without_combat_fields_stays_valid()
    {
        const string legacyJson = """
            {
              "SnapshotSequence": 1, "GameTimeMs": 1234, "CurrentSpeed": 0,
              "Objects": [{"ObjectId":"ship-1","X":10,"Y":20,"SpeedKmS":0.7,"Direction":90}],
              "InstalledModules": [{"ModuleId":"engine-1","ModuleTypeId":"module.engine.basic",
                "DisplayName":"Engine","Position":0,"CommandTypeIds":["engine.accelerate"]}]
            }
            """;
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(legacyJson);

        Assert.NotNull(restored);
        var motion = Assert.Single(restored.Objects);
        Assert.Equal(new ObjectMotionSnapshot("ship-1", 10, 20, 0.7, 90), motion);
        Assert.Null(motion.HullCombat);
        Assert.Null(motion.Torpedo);
        var module = Assert.Single(restored.InstalledModules);
        Assert.Null(module.LauncherCombat);
        Assert.Equal("engine.accelerate", Assert.Single(module.CommandTypeIds));
        Assert.Null(new InstalledModuleSnapshot("engine-1", "module.engine.basic", "Engine", 0,
            ImmutableArray<string>.Empty).LauncherCombat);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(restored));
        var motionJson = document.RootElement.GetProperty("Objects")[0];
        Assert.Equal(JsonValueKind.Null, motionJson.GetProperty("HullCombat").ValueKind);
        Assert.Equal(JsonValueKind.Null, motionJson.GetProperty("Torpedo").ValueKind);
        Assert.Equal(JsonValueKind.Null,
            document.RootElement.GetProperty("InstalledModules")[0].GetProperty("LauncherCombat").ValueKind);
    }

    [Theory]
    [InlineData(TorpedoRoutePhase.Turning, 90)]
    [InlineData(TorpedoRoutePhase.Straight, 0)]
    public void No_intercept_has_null_eta_not_zero(TorpedoRoutePhase phase, double angularVelocity)
    {
        // A pursuit or lost-target continuation has geometry, but no confirmed encounter.
        var route = new TorpedoRoute(1000, 1, phase, false,
            ImmutableArray.Create(new TorpedoRouteSegment(10, 20, 90, 3, angularVelocity, 1000)));
        var torpedo = new TorpedoSnapshot("ship-1", "launcher-1", "lost-target", 500,
            3, 90, 150, 15, route);
        var json = JsonSerializer.Serialize(torpedo);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("PredictedImpactMotionTimeMs").ValueKind);

        var restored = JsonSerializer.Deserialize<TorpedoSnapshot>(json);
        Assert.NotNull(restored);
        Assert.Null(restored.PredictedImpactMotionTimeMs);
        Assert.False(restored.Route.HasIntercept);
        Assert.Equal(phase, restored.Route.Phase);
        Assert.Single(restored.Route.Segments);
        Assert.Equal("lost-target", restored.TargetObjectId);
        Assert.Equal(100, restored.HitChancePercent);
    }

    [Fact]
    public void Default_combat_collections_serialize_as_empty_arrays()
    {
        var torpedo = new TorpedoSnapshot("ship-1", "launcher-1", "ship-2", 0,
            3, 90, 150, 0, new TorpedoRoute(0, 1, TorpedoRoutePhase.Turning, false));
        var json = JsonSerializer.Serialize(torpedo);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(0, document.RootElement.GetProperty("Trail").GetArrayLength());
        Assert.Equal(0, document.RootElement.GetProperty("Route").GetProperty("Segments").GetArrayLength());

        var restored = JsonSerializer.Deserialize<TorpedoSnapshot>(json);
        Assert.NotNull(restored);
        Assert.False(restored.Trail.IsDefault);
        Assert.Empty(restored.Trail);
        Assert.False(restored.Route.Segments.IsDefault);
        Assert.Empty(restored.Route.Segments);
    }

    [Theory]
    [InlineData("")]
    [InlineData(",\"Trail\":null")]
    [InlineData(",\"Trail\":[]")]
    public void Missing_optional_combat_values_keep_safe_defaults(string trailJson)
    {
        var json = """
            {"OwnerObjectId":"ship-1","LauncherModuleId":"launcher-1","TargetObjectId":"ship-2",
             "LaunchMotionTimeMs":0,"SpeedKmS":3,"TurnRateDegPerSec":90,"Damage":150,
             "DistanceTravelledWorldUnits":0,
             "Route":{"StartMotionTimeMs":0,"PlannerVersion":1,"Phase":0,"HasIntercept":false}
            """ + trailJson + "}";
        var restored = JsonSerializer.Deserialize<TorpedoSnapshot>(json);

        Assert.NotNull(restored);
        Assert.Equal(100, restored.HitChancePercent);
        Assert.Null(restored.PredictedImpactMotionTimeMs);
        Assert.True(restored.Trail.IsDefaultOrEmpty);
        Assert.True(restored.Route.Segments.IsDefaultOrEmpty);
        var roundTripped = JsonSerializer.Deserialize<TorpedoSnapshot>(JsonSerializer.Serialize(restored));
        Assert.NotNull(roundTripped);
        Assert.False(roundTripped.Trail.IsDefault);
        Assert.False(roundTripped.Route.Segments.IsDefault);
    }
}
