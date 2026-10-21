using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
namespace DeepSpaceSaga.Engine.Tests;

public sealed class BasicCombatEndToEndTests
{
    [Fact]
    public void Repeated_point_core_blasts_destroy_the_ship_once_and_preserve_unrelated_rng()
    {
        using var run = new CombatRun(); var dialogue = JsonSerializer.Serialize(run.Save().GameState.DialogueState);
        int shots = 0; AuthoritativeSnapshot result = run.Snapshot();
        while (result.Objects.Any(o => o.ObjectId == Target) && shots < 12)
        {
            var launch = run.Launch(++shots); var own = Flight(launch).OwnProjectile!;
            Assert.Equal(200, own.Damage); Assert.Equal(20, own.ArmorPiercingPercent); Assert.Equal("medium", own.ProfileId);
            Assert.Null(Flight(launch).Torpedo); Assert.Equal(-100, own.DetonationY);
            result = run.AdvanceTo(EndTime(launch));
        }
        Assert.DoesNotContain(result.Objects, o => o.ObjectId == Target); Assert.Single(result.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        Assert.Single(result.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed);
        Assert.Equal(SessionOutcomeState.Running, result.EffectiveOutcome.State);
        Assert.Equal(dialogue, JsonSerializer.Serialize(run.Save().GameState.DialogueState));
        Assert.DoesNotContain(result.CombatJournal, e => e.ChanceTenths is not null || e.Type == CombatEventType.TorpedoHitRoll);
    }
    [Theory]
    [InlineData(SimulationSpeed.Speed1)]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.Speed3)]
    [InlineData(SimulationSpeed.Speed4)]
    public void Actual_point_combat_is_partition_invariant(SimulationSpeed speed)
    {
        using var whole = new CombatRun(); using var split = new CombatRun();
        var launched = whole.Launch(1); split.Launch(1); long end = EndTime(launched);
        for (long time = 17; time < end; time += 37) split.AdvanceTo(time, speed);
        var a = whole.AdvanceTo(end); var b = split.AdvanceTo(end, speed);
        Assert.Equal(JsonSerializer.Serialize(a.Objects), JsonSerializer.Serialize(b.Objects));
        Assert.Equal(JsonSerializer.Serialize(a.CombatJournal), JsonSerializer.Serialize(b.CombatJournal));
        Assert.Equal(ScenarioLoader.Serialize(whole.Save()), ScenarioLoader.Serialize(split.Save()));
    }
    internal static ObjectMotionSnapshot Flight(AuthoritativeSnapshot snapshot) => Assert.Single(snapshot.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo);
    internal static long EndTime(AuthoritativeSnapshot launch) => (long)Math.Ceiling(Flight(launch).OwnProjectile!.DetonationMotionTimeMs);
    internal sealed class CombatRun : IDisposable
    {
        internal SimulationEngine Engine { get; }
        private long _motion;
        internal CombatRun(string? path = null)
        {
            Engine = new(Registry.Value);
            Engine.LoadScenario(path is null ? TorpedoImpactTests.Scenario() : ScenarioLoader.LoadFromFile(path, true, isSave: true), isSave: path is not null);
            _motion = Engine.CaptureSnapshot().MotionTimeMs;
        }
        internal ScenarioFile Save() => Engine.CaptureSaveStateForTests(_motion, SimulationSpeed.Speed0, _motion);
        internal AuthoritativeSnapshot Snapshot() => At(Engine, _motion);
        internal AuthoritativeSnapshot Launch(int shot)
        {
            _motion = Math.Max(_motion, (shot - 1) * 10000L); At(Engine, _motion);
            Engine.ReceiveCommand(LaunchAt("proof-" + shot, 0, -100, "medium")); return Snapshot();
        }
        internal AuthoritativeSnapshot AdvanceTo(long time, SimulationSpeed speed = SimulationSpeed.Speed0)
        { _motion = time; return Engine.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, time); }
        public void Dispose() => Engine.Dispose();
    }
}
