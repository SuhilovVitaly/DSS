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
        PositionX = 350,
        PositionY = 0,
        SpeedMps = 610.8652381980153,
        DirectionDegrees = 180,
        Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { PowerState = "On" } :
            m.ModuleTypeId == "module.countermeasure.launcher.basic" ? m with { AutoDefenseEnabled = defense } : m).ToArray()
    });

    [Fact]
    public void Fixed_point_flight_does_not_replan_on_a_hostile_maneuver_and_partitions_match()
    {
        using var whole = Create(OrbitScene()); using var split = Create(OrbitScene());
        foreach (var engine in new[] { whole, split }) { At(engine, 100); engine.ReceiveCommand(LaunchAt("point", 500, 0, "slow")); At(engine, 100); }
        var initial = Assert.Single(At(whole, 100).Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        foreach (long t in new long[] { 113, 170, 200, 345, 599, 1700, 2222 }) At(split, t);
        var expected = At(whole, 3000); var actual = At(split, 3000);
        var flight = Assert.Single(expected.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal(initial.DetonationX, flight.DetonationX); Assert.Equal(initial.DetonationY, flight.DetonationY);
        Assert.Equal(initial.DetonationMotionTimeMs, flight.DetonationMotionTimeMs); Assert.Equal(174, flight.X, 8);
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
        Assert.Equal(JsonSerializer.Serialize(expected.CombatJournal), JsonSerializer.Serialize(actual.CombatJournal));
        Assert.DoesNotContain(expected.Objects, o => o.Countermeasure is not null);
    }

    [Fact]
    public void Player_acceleration_forces_orbit_to_pursuit_without_resetting_direction()
    {
        using var engine = Create(OrbitScene());
        var orbit = At(engine, 100).Objects.Single(o => o.ObjectId == Target).HostileCombat!;
        Assert.Equal(HostileCombatPhase.OrbitHold, orbit.Phase);
        engine.ReceiveCommand(new("accelerate", 1, Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.Accelerate));
        At(engine, 100);
        var snapshot = At(engine, 20100);
        Assert.Equal(6, snapshot.Objects.Single(o => o.ObjectId == Player).SpeedKmS);
        var pursuit = snapshot.Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(HostileCombatPhase.Pursuit, pursuit.HostileCombat!.Phase);
        Assert.Equal(orbit.Clockwise, pursuit.HostileCombat.Clockwise);
        Assert.Equal(6, pursuit.SpeedKmS);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null || o.Countermeasure is not null);
    }

    [Fact]
    public void Actual_point_hits_destroy_a_maneuvering_hostile_and_remove_its_controller()
    {
        using var engine = Create(OrbitScene()); AuthoritativeSnapshot snapshot = At(engine, 100);
        for (int shot = 0; shot < 8 && snapshot.Objects.Any(o => o.ObjectId == Target); shot++)
        {
            long time = 100 + shot * 12500; snapshot = At(engine, time);
            var target = snapshot.Objects.Single(o => o.ObjectId == Target);
            engine.ReceiveCommand(LaunchAt("point-" + shot, target.X, target.Y, "slow")); At(engine, time);
            snapshot = At(engine, time + 10000);
        }
        Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == Target || o.HostileCombat is not null);
        var wreck = Assert.Single(snapshot.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        Assert.Equal(wreck, At(engine, snapshot.MotionTimeMs + 10000).Objects.Single(o => o.ObjectId == wreck.ObjectId));
        Assert.All(snapshot.CombatJournal, e => Assert.Null(e.Roll));
    }
}
