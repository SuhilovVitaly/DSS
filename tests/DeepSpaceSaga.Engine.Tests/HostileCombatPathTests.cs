using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class HostileCombatPathTests
{
    [Fact]
    public void Swept_collision_uses_hostile_curve_and_real_bounds()
    {
        var segment = new HostileMotionSegment(1000, 100, 0, 0, 0, 20, 0, 2, 1800);
        var state = new HostileCombatSnapshot(Player, "e", "w", 100, true, HostileCombatPhase.OrbitHold, segment);
        var owner = new SpaceObjectRuntime(new(Target, 0, 0, 20, 0, HostileCombat: state), SpaceObjectType.NpcShip, 1000, []);
        var path = SimulationEngine.CombatPath(owner);
        var middle = HostileOrbitMath.Sample(segment, 50.5);
        var contact = TorpedoCollisionMath.FirstContact(path, new(_ => (middle.X, middle.Y), 0, 0), 1000, 1100, 0.01);
        Assert.NotNull(contact);
        Assert.InRange(contact.Value.MotionTimeMs, 1050, 1050.5);
        Assert.Equal(20.2 / 100, path.MaxSpeed, 12);
        Assert.Equal(2d / 100000 + 20.2 / 100 * 1800 * Math.PI / 180000, path.MaxAcceleration!.Value, 12);
        var endpoint = HostileOrbitMath.Sample(segment, 100);
        var forecast = path.Position(1200);
        Assert.Equal(endpoint.X, forecast.X, 10);
        Assert.Equal(endpoint.Y + endpoint.SpeedKmS, forecast.Y, 10);
    }

    internal static ScenarioFile OrbitScene(bool defense = false) => TorpedoImpactTests.Scenario(o => o.ObjectId != Target ? o : o with
    {
        PositionX = 1000,
        PositionY = 0,
        SpeedMps = 1745.3292519943295,
        DirectionDegrees = 180,
        Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { PowerState = "On" } :
            m.ModuleTypeId == "module.countermeasure.launcher.basic" ? m with { AutoDefenseEnabled = defense } : m).ToArray()
    });

    [Fact]
    public void Guidance_replans_on_real_hostile_maneuver_and_split_intervals_match()
    {
        using var whole = Create(OrbitScene());
        using var split = Create(OrbitScene());
        foreach (var engine in new[] { whole, split }) { At(engine, 100); engine.ReceiveCommand(Fire("attack")); At(engine, 100); }
        var initial = At(whole, 100).Objects.Single(o => o.Torpedo is not null).Torpedo!;
        foreach (long time in new long[] { 113, 170, 200, 345, 599, 1700, 2222 }) At(split, time);
        var expected = At(whole, 3000);
        var actual = At(split, 3000);
        var flight = expected.Objects.Single(o => o.Torpedo is not null).Torpedo!;
        Assert.True(flight.Route.StartMotionTimeMs > initial.Route.StartMotionTimeMs);
        Assert.True(flight.Trail.Length > 1);
        Assert.Equal(HostileCombatPhase.OrbitHold, expected.Objects.Single(o => o.ObjectId == Target).HostileCombat!.Phase);
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
        Assert.Equal(JsonSerializer.Serialize(expected.CombatImpacts), JsonSerializer.Serialize(actual.CombatImpacts));
    }

    [Fact]
    public void Orbiting_defender_launches_and_resolves_real_countermeasure()
    {
        using var engine = new SimulationEngine(AutomaticDefenseTests.WeaponRegistry(100, 0));
        engine.LoadScenario(OrbitScene(true));
        At(engine, 100);
        engine.ReceiveCommand(Fire("attack")); At(engine, 100);
        var end = At(engine, 30000);
        var intercept = Assert.Single(end.CombatImpacts);
        Assert.Equal(TorpedoTerminationKind.Intercept, intercept.TerminationKind);
        Assert.DoesNotContain(end.CombatJournal, e => e.Type == CombatEventType.ModuleHit && e.TargetObjectId == Target);
        Assert.Single(end.CombatJournal.Where(e => e.Roll is not null));
        Assert.Contains(end.CombatJournal, e => e.Type == CombatEventType.Launch && e.Result == "countermeasure");
    }

    [Fact]
    public void Player_acceleration_forces_orbit_to_pursuit_without_resetting_direction()
    {
        using var engine = Create(OrbitScene());
        var orbit = At(engine, 100).Objects.Single(o => o.ObjectId == Target).HostileCombat!;
        Assert.Equal(HostileCombatPhase.OrbitHold, orbit.Phase);
        engine.ReceiveCommand(new("accelerate", 1, Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.Accelerate));
        engine.ReceiveCommand(Fire("attack")); At(engine, 100);
        var snapshot = At(engine, 20100);
        Assert.Equal(6, snapshot.Objects.Single(o => o.ObjectId == Player).SpeedKmS);
        var pursuit = snapshot.Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(HostileCombatPhase.Pursuit, pursuit.HostileCombat!.Phase);
        Assert.Equal(orbit.Clockwise, pursuit.HostileCombat.Clockwise);
        Assert.Equal(6, pursuit.SpeedKmS);
        Assert.True(snapshot.Objects.Single(o => o.Torpedo is not null).Torpedo!.Route.StartMotionTimeMs > 100);
    }

    [Fact]
    public void Three_real_hits_destroy_maneuvering_hostile_and_stop_its_controller()
    {
        using var engine = Create(OrbitScene(), LegacyWeaponRegistry(100, 100));
        long time = 100;
        At(engine, time);
        AuthoritativeSnapshot snapshot = null!;
        for (int hit = 1; hit <= 3; hit++)
        {
            engine.ReceiveCommand(Fire("attack-" + hit));
            Assert.Contains(At(engine, time).Objects, o => o.Torpedo is not null);
            long deadline = time + 180000;
            do { time += 1000; snapshot = At(engine, time); }
            while (snapshot.CombatImpacts.Length < hit && time < deadline);
            Assert.Equal(hit, snapshot.CombatImpacts.Length);
        }
        Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == Target || o.HostileCombat is not null);
        var wreck = Assert.Single(snapshot.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        Assert.Equal(wreck, At(engine, time + 10000).Objects.Single(o => o.ObjectId == wreck.ObjectId));
    }
}
