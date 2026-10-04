using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class BasicCombatEndToEndTests
{
    [Fact]
    public void Legacy_three_hit_contract_stays_valid()
    {
        using var run = new CombatRun();
        var initial = run.Snapshot();
        var randomState = run.Engine.CaptureSaveState().GameState.DialogueState;
        Assert.Equal(450, initial.Objects.Single(o => o.ObjectId == Target).HullCombat!.CurrentHp);
        Assert.Equal(2, initial.Objects.Length);
        for (int shot = 1; shot <= 3; shot++)
        {
            var launch = run.Launch(shot);
            var projectile = Flight(launch);
            var owner = launch.Objects.Single(o => o.ObjectId == Player);
            Assert.Equal(owner.X, projectile.X);
            Assert.Equal(owner.Y, projectile.Y);
            Assert.Equal(owner.Direction, projectile.Direction);
            Assert.Equal(3, projectile.SpeedKmS);
            Assert.Equal(90, projectile.Torpedo!.TurnRateDegPerSec);
            Assert.Equal(150, projectile.Torpedo.Damage);
            Assert.Equal(Player, projectile.Torpedo.OwnerObjectId);
            Assert.Equal(Target, projectile.Torpedo.TargetObjectId);
            Assert.Equal(Launcher, projectile.Torpedo.LauncherModuleId);
            Assert.Equal(projectile.ObjectId, launch.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);

            // Duplicate identity and a new busy request must not create another projectile.
            run.Engine.ReceiveCommand(Fire("proof-" + shot));
            run.Engine.ReceiveCommand(Fire("busy-" + shot));
            var busy = run.Snapshot();
            Assert.Equal(projectile.ObjectId, Flight(busy).ObjectId);
            Assert.Contains(busy.CommandResults, r => r.CommandId == "busy-" + shot && r.ReasonCode == CommandReasonCodes.Busy);
            var end = run.AdvanceTo(EndTime(launch));
            AssertHit(end, shot);
            Assert.Equal(shot, end.CombatImpacts.Length);
            var hit = end.CombatImpacts[^1];
            Assert.Equal(projectile.ObjectId, hit.TorpedoObjectId);
            var source = run.Source.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
            double x = source.PositionX + source.SpeedMps / 100000 * hit.MotionTimeMs * Math.Sin(source.DirectionDegrees * Math.PI / 180);
            double y = source.PositionY - source.SpeedMps / 100000 * hit.MotionTimeMs * Math.Cos(source.DirectionDegrees * Math.PI / 180);
            Assert.InRange(Math.Abs(Math.Sqrt(Math.Pow(hit.X - x, 2) + Math.Pow(hit.Y - y, 2)) - 5), 0, 1e-6);
            if (shot == 3)
            {
                var wreck = Assert.Single(end.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
                Assert.Equal(x, wreck.X, 6);
                Assert.Equal(y, wreck.Y, 6);
                Assert.NotEqual(Target, wreck.ObjectId);
                Assert.Equal(hit.WreckObjectId, wreck.ObjectId);
                Assert.Equal(Target, hit.DestroyedObjectId);
                Assert.Equal(0, wreck.SpeedKmS);
                Assert.Equal(0, wreck.Direction);
                Assert.Null(wreck.HullCombat);
            }
        }
        var final = run.Snapshot();
        var later = run.AdvanceTo(final.MotionTimeMs + 10000);
        Assert.Equal(3, later.CombatImpacts.Length);
        Assert.Equal(final.Objects.Single(o => o.ObjectType == SpaceObjectType.Wreck), later.Objects.Single(o => o.ObjectType == SpaceObjectType.Wreck));
        Assert.Equal(450, later.Objects.Single(o => o.ObjectId == Player).HullCombat!.CurrentHp);
        Assert.NotEqual(initial.Objects.Single(o => o.ObjectId == Player).Y, later.Objects.Single(o => o.ObjectId == Player).Y);
        var saved = run.Engine.CaptureSaveState().GameState;
        Assert.Equal(42UL, saved.MasterSeed);
        Assert.Equal(JsonSerializer.Serialize(randomState), JsonSerializer.Serialize(saved.DialogueState));
        Assert.Equal(3, saved.CombatState!.ProjectileSequence);
        Assert.Equal(3, saved.CombatState.ImpactSequence);
        Assert.Equal(1, saved.CombatState.WreckSequence);
        Assert.Equal(new[] { "proof-1", "proof-2", "proof-3" }, saved.CombatState.ProcessedLaunchCommandIds);
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed1)]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.Speed3)]
    [InlineData(SimulationSpeed.Speed4)]
    public void Combat_results_are_equal_under_pause_speed_and_time_partitions(SimulationSpeed speed)
    {
        using var continuous = new CombatRun();
        using var partitioned = new CombatRun();
        for (int shot = 1; shot <= 3; shot++)
        {
            var launched = continuous.Launch(shot);
            SameWorld(launched, partitioned.Launch(shot));
            long end = EndTime(launched);
            long mid = launched.MotionTimeMs + (end - launched.MotionTimeMs) / 2;
            SameWorld(continuous.AdvanceTo(mid), partitioned.AdvanceTo(mid, speed, partition: true));
            var paused = partitioned.Snapshot();
            partitioned.ElapsePaused(123456);
            SameWorld(paused, partitioned.Snapshot());
            var expected = continuous.AdvanceTo(end);
            var actual = partitioned.AdvanceTo(end, speed, partition: true);
            SameWorld(expected, actual);
            AssertHit(actual, shot);
            Assert.Equal(expected.CombatImpacts.Length, actual.CombatImpacts.Length);
            foreach (var pair in expected.CombatImpacts.Zip(actual.CombatImpacts))
                SameImpact(pair.First, pair.Second);
        }
    }

    internal static ObjectMotionSnapshot Flight(AuthoritativeSnapshot snapshot) =>
        Assert.Single(snapshot.Objects, o => o.Torpedo is not null);

    internal static long EndTime(AuthoritativeSnapshot launch) => Flight(launch).Torpedo!.PredictedImpactMotionTimeMs!.Value + 1000;

    internal static void AssertHit(AuthoritativeSnapshot snapshot, int shot)
    {
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        Assert.Null(snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);
        var hit = snapshot.CombatImpacts[^1];
        Assert.Equal(shot, hit.EventId);
        Assert.Equal(Target, hit.HitObjectId);
        Assert.Equal(150, hit.DamageApplied);
        Assert.NotEmpty(hit.FinalTrail);
        if (shot < 3)
        {
            Assert.Equal(450 - shot * 150, snapshot.Objects.Single(o => o.ObjectId == Target).HullCombat!.CurrentHp);
            Assert.DoesNotContain(snapshot.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        }
        else
        {
            Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == Target);
            Assert.Single(snapshot.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        }
    }

    internal static void SameWorld(AuthoritativeSnapshot expected, AuthoritativeSnapshot actual)
    {
        Assert.Equal(expected.MotionTimeMs, actual.MotionTimeMs);
        Assert.Equal(expected.GameTimeMs, actual.GameTimeMs);
        Assert.Equal(expected.Objects.Select(o => o.ObjectId), actual.Objects.Select(o => o.ObjectId));
        foreach (var (a, b) in expected.Objects.Zip(actual.Objects))
        {
            Assert.Equal(a.X, b.X, 6);
            Assert.Equal(a.Y, b.Y, 6);
            Assert.Equal(a.Direction, b.Direction, 6);
            Assert.Equal(a.SpeedKmS, b.SpeedKmS);
            Assert.Equal(a.HullCombat, b.HullCombat);
            Assert.Equal(a.Torpedo is null, b.Torpedo is null);
            if (a.Torpedo is { } flight)
            {
                Assert.Equal(flight.DistanceTravelledWorldUnits, b.Torpedo!.DistanceTravelledWorldUnits, 6);
                Assert.Equal(flight.TargetObjectId, b.Torpedo.TargetObjectId);
                Assert.Equal(flight.OwnerObjectId, b.Torpedo.OwnerObjectId);
                Assert.Equal(flight.LauncherModuleId, b.Torpedo.LauncherModuleId);
                Assert.Equal(JsonSerializer.Serialize(flight.Route), JsonSerializer.Serialize(b.Torpedo.Route));
                SameTrail(flight.Trail, b.Torpedo.Trail);
            }
        }
        Assert.Equal(JsonSerializer.Serialize(expected.InstalledModules), JsonSerializer.Serialize(actual.InstalledModules));
    }

    internal static void SameImpact(CombatImpactSnapshot expected, CombatImpactSnapshot actual)
    {
        Assert.Equal(expected.EventId, actual.EventId);
        Assert.Equal(expected.TorpedoObjectId, actual.TorpedoObjectId);
        Assert.Equal(expected.TargetObjectId, actual.TargetObjectId);
        Assert.Equal(expected.HitObjectId, actual.HitObjectId);
        Assert.Equal(expected.DamageApplied, actual.DamageApplied);
        Assert.Equal(expected.DestroyedObjectId, actual.DestroyedObjectId);
        Assert.Equal(expected.WreckObjectId, actual.WreckObjectId);
        Assert.Equal(expected.MotionTimeMs, actual.MotionTimeMs, 4);
        Assert.Equal(expected.X, actual.X, 6);
        Assert.Equal(expected.Y, actual.Y, 6);
        SameTrail(expected.FinalTrail, actual.FinalTrail);
    }

    private static void SameTrail(IReadOnlyList<TrailSegment> expected, IReadOnlyList<TrailSegment> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var (a, b) in expected.Zip(actual))
        {
            Assert.Equal(a.StartMotionTimeMs, b.StartMotionTimeMs, 4);
            Assert.Equal(a.PlannerVersion, b.PlannerVersion);
            Assert.Equal(a.Segment.X, b.Segment.X, 6);
            Assert.Equal(a.Segment.Y, b.Segment.Y, 6);
            Assert.Equal(a.Segment.Direction, b.Segment.Direction, 6);
            Assert.Equal(a.Segment.SpeedKmS, b.Segment.SpeedKmS);
            Assert.Equal(a.Segment.AngularVelocityDegPerSec, b.Segment.AngularVelocityDegPerSec);
            Assert.Equal(a.Segment.DurationMs, b.Segment.DurationMs, 4);
        }
    }

    // Only the monotonic clock is controlled. Scenario positions, motion, modules and HP
    // are loaded unchanged, with automatic defense explicitly disabled for the EP6 proof; every shot is a real PlayerCommand.
    internal sealed class CombatRun : IDisposable
    {
        private long _now;
        internal SimulationEngine Engine { get; }
        internal ScenarioFile Source { get; }
        internal CombatRun(string? savePath = null)
        {
            Source = WithoutDefense(ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json")));
            Engine = new SimulationEngine(Registry.Value, clock: new SimulationClock(SimulationSpeed.Speed0, () => _now));
            Engine.LoadScenario(savePath is null
                ? Source with { GameState = Source.GameState with { MasterSeed = 42 } }
                : ScenarioLoader.LoadFromFile(savePath, allowNonZeroGameTime: true), isSave: savePath is not null);
        }
        internal AuthoritativeSnapshot Snapshot() => Engine.CaptureSnapshot(advanceClock: true);
        internal AuthoritativeSnapshot Launch(int shot)
        {
            Engine.ReceiveCommand(Fire("proof-" + shot));
            var snapshot = Snapshot();
            Assert.Contains(snapshot.CommandResults, r => r.CommandId == "proof-" + shot && r.Status == CommandResultStatus.Executed);
            return snapshot;
        }
        internal void ElapsePaused(long realMs)
        {
            Engine.SetSpeed(SimulationSpeed.Speed0);
            _now += realMs;
            Snapshot();
        }
        internal AuthoritativeSnapshot AdvanceTo(long motionMs, SimulationSpeed speed = SimulationSpeed.Speed1, bool partition = false)
        {
            long remaining = motionMs - Snapshot().MotionTimeMs;
            Assert.True(remaining >= 0);
            Engine.SetSpeed(speed);
            long realMs = remaining / (int)speed;
            while (realMs > 0)
            {
                long delta = partition ? Math.Min(realMs, 1379) : realMs;
                _now += delta;
                realMs -= delta;
                Snapshot();
            }
            Engine.SetSpeed(SimulationSpeed.Speed1);
            _now += remaining % (int)speed;
            Engine.SetSpeed(SimulationSpeed.Speed0);
            return Snapshot();
        }
        public void Dispose() => Engine.Dispose();
    }
}
