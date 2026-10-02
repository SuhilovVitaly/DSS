using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TorpedoLaunchTests
{
    private const string Player = "SPC-0001", Target = "SPC-0002", Launcher = "MOD-PLAYER-TORPEDO-01";
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static readonly Lazy<GameDataRegistry> Registry = new(() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _));
    private static PlayerCommand Fire(string id = "fire", string? target = Target) => new(id, 1, Player, Launcher, CombatCommandTypes.Fire, target);
    private static ObjectMotionSnapshot Projectile(AuthoritativeSnapshot snapshot) => Assert.Single(snapshot.Objects.Where(o => o.Torpedo is not null));
    private static SimulationEngine Create(Func<SpaceObjectData, SpaceObjectData>? configure = null)
    {
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json"));
        var engine = new SimulationEngine(Registry.Value);
        engine.LoadScenario(scenario with
        {
            GameState = scenario.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => configure?.Invoke(o) ?? o).ToArray()
            }
        });
        return engine;
    }

    [Fact]
    public void Paused_launch_creates_one_stationary_projectile()
    {
        using var engine = Create();
        var before = engine.CaptureSnapshot();
        engine.ReceiveCommand(Fire());
        var snapshot = engine.CaptureSnapshot();
        var projectile = Projectile(snapshot);
        var carrier = snapshot.Objects.Single(o => o.ObjectId == Player);
        Assert.Equal((carrier.X, carrier.Y, carrier.Direction), (projectile.X, projectile.Y, projectile.Direction));
        Assert.Equal(3, projectile.SpeedKmS);
        Assert.Equal(0, projectile.Torpedo!.DistanceTravelledWorldUnits);
        Assert.Equal(projectile.ObjectId, snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);
        Assert.Equal(Json(projectile), Json(Projectile(engine.CaptureSnapshot(advanceClock: true))));
        Assert.Equal(before.MotionTimeMs, snapshot.MotionTimeMs);
        Assert.Equal(2, before.Objects.Length);
    }

    [Fact]
    public void Second_launch_and_duplicate_command_do_not_create_projectile()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire());
        var first = Projectile(engine.CaptureSnapshot());
        engine.ReceiveCommand(Fire());
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(engine.CaptureSnapshot().CommandResults).Status);
        engine.ReceiveCommand(Fire("second"));
        var result = engine.CaptureSnapshot();
        Assert.Equal(CommandReasonCodes.Busy, result.CommandResults.Single(r => r.CommandId == "second").ReasonCode);
        Assert.Equal(Json(first), Json(Projectile(result)));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("self")]
    [InlineData("absent")]
    [InlineData("npc")]
    [InlineData("module")]
    [InlineData("wrong-module")]
    [InlineData("off")]
    [InlineData("disabled")]
    [InlineData("broken")]
    public void Missing_self_or_invalid_target_is_rejected_without_effect(string kind)
    {
        using var engine = Create(o => o.ObjectId != Player ? o : o with
        {
            Modules = o.Modules!.Select(m =>
            m.ModuleId != Launcher ? m : m with
            {
                PowerState = kind == "off" ? "Off" : m.PowerState,
                OperationalState = kind == "disabled" ? "Disabled" : m.OperationalState,
                StructurePoints = kind == "broken" ? 0 : m.StructurePoints
            }).ToArray()
        });
        var before = engine.CaptureSnapshot();
        var save = engine.CaptureSaveState();
        var command = kind switch
        {
            "missing" => Fire(target: null),
            "self" => Fire(target: Player),
            "absent" => Fire(target: "absent"),
            "npc" => Fire() with { ObjectId = Target, ModuleId = "MOD-PIRATE-TORPEDO-01", TargetObjectId = Player },
            "module" => Fire() with { ModuleId = "absent" },
            "wrong-module" => Fire() with { ModuleId = "MOD-PLAYER-ENGINE-01" },
            _ => Fire()
        };
        engine.ReceiveCommand(command);
        var after = engine.CaptureSnapshot();
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(after.CommandResults).Status);
        Assert.Equal(Json(before.Objects), Json(after.Objects));
        Assert.Equal(Json(before.InstalledModules), Json(after.InstalledModules));
        var savedAfter = engine.CaptureSaveState();
        Assert.Equal(Json(save.GameState), Json(savedAfter.GameState with { CommandReceipts = save.GameState.CommandReceipts }));
    }

    [Fact]
    public void Carrier_maneuver_does_not_retarget_torpedo()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire());
        var launched = Projectile(engine.CaptureSnapshot());
        engine.ReceiveCommand(new("turn", 2, Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnRightUntilCancel));
        engine.CaptureSnapshotForTests();
        var snapshot = engine.CaptureSnapshotForTests(3_000_000, simulationTimeMs: 10_000);
        var flight = Projectile(snapshot);
        var expected = new LinearMotionPredictor().Predict(launched, 10_000);
        Assert.Equal(Target, flight.Torpedo!.TargetObjectId);
        Assert.Equal(launched.Torpedo!.Route.Segments, flight.Torpedo.Route.Segments);
        Assert.Equal((expected.X, expected.Y, expected.Direction), (flight.X, flight.Y, flight.Direction));
        Assert.NotEqual(0, snapshot.Objects.Single(o => o.ObjectId == Player).Direction);
        Assert.Equal(300, flight.Torpedo.DistanceTravelledWorldUnits);
        Assert.InRange(Math.Abs(flight.Torpedo.Trail.Sum(s => s.Segment.DurationMs) - 10000), 0, 1e-8);
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed0)]
    [InlineData(SimulationSpeed.Speed1)]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.Speed3)]
    [InlineData(SimulationSpeed.Speed4)]
    public void Motion_clock_controls_torpedo_under_all_speeds(SimulationSpeed speed)
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire());
        engine.CaptureSnapshot();
        long real = 0;
        var clock = new SimulationClock(speed, () => real);
        real = 1000;
        var time = clock.UpdateAndCapture();
        var snapshot = engine.CaptureSnapshotForTests(time.GameTimeMs, speed, time.MotionTimeMs);
        Assert.Equal((int)speed * 30, Projectile(snapshot).Torpedo!.DistanceTravelledWorldUnits);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Flight_and_history_are_independent_of_snapshot_partitions(bool unreachable)
    {
        SpaceObjectData Configure(SpaceObjectData o) => unreachable && o.ObjectId == Target
            ? o with { SpeedMps = 4000, DirectionDegrees = 270 } : o;
        using var whole = Create(Configure);
        using var split = Create(Configure);
        foreach (var e in new[] { whole, split }) { e.ReceiveCommand(Fire()); e.CaptureSnapshot(); }
        var expected = Projectile(whole.CaptureSnapshotForTests(10000, simulationTimeMs: 10000));
        foreach (long t in new long[] { 7, 101, 999, 1234, 7001, 10000 }) split.CaptureSnapshotForTests(t, simulationTimeMs: t);
        var actual = Projectile(split.CaptureSnapshotForTests(10000, simulationTimeMs: 10000));
        Assert.InRange(Math.Abs(expected.X - actual.X) + Math.Abs(expected.Y - actual.Y), 0, 1e-8);
        Assert.Equal(expected.Torpedo!.TargetObjectId, actual.Torpedo!.TargetObjectId);
        Assert.Equal(expected.Torpedo.Route.StartMotionTimeMs, actual.Torpedo.Route.StartMotionTimeMs);
        Assert.Equal(expected.Torpedo.Route.HasIntercept, actual.Torpedo.Route.HasIntercept);
        Assert.Equal(expected.Torpedo.Route.ElapsedMs, actual.Torpedo.Route.ElapsedMs);
        Assert.Equal(expected.Torpedo.DistanceTravelledWorldUnits, actual.Torpedo.DistanceTravelledWorldUnits, 8);
        Assert.InRange(Math.Abs(actual.Torpedo.Trail.Sum(t => t.Segment.DurationMs) - 10000), 0, 1e-8);
        Assert.Equal(expected.Torpedo.Trail.Length, actual.Torpedo.Trail.Length);
    }

    [Fact]
    public void Lost_target_continues_heading_without_freeing_launcher()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire());
        var first = Projectile(engine.CaptureSnapshot());
        engine.RemoveObjectForTests(Target);
        var lost = Projectile(engine.CaptureSnapshot());
        Assert.Null(lost.Torpedo!.PredictedImpactMotionTimeMs);
        var later = Projectile(engine.CaptureSnapshotForTests(1000, simulationTimeMs: 1000));
        Assert.Equal(first.Direction, later.Direction);
        Assert.Equal(Target, later.Torpedo!.TargetObjectId);
        Assert.Equal(30, later.Torpedo.DistanceTravelledWorldUnits);
        Assert.NotNull(engine.CaptureSnapshotForTests(1000).InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
