using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class OrbitalSynchronizationTests
{
    internal const string Ship = "SPC-0001", Station = "SPC-0002", EngineId = "MOD-PLAYER-ENGINE-01";
    internal static SimulationEngine Create(ulong seed)
    {
        var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        var scenario = SeededWorldBootstrapTests.Scenario();
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))!;
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: config);
        return engine;
    }

    internal static AuthoritativeSnapshot At(SimulationEngine engine, long time) =>
        engine.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed0, time);
    internal static ObjectMotionSnapshot Player(AuthoritativeSnapshot snapshot) => snapshot.Objects.Single(o => o.ObjectId == Ship);
    internal static ObjectMotionSnapshot Target(AuthoritativeSnapshot snapshot) => snapshot.Objects.Single(o => o.ObjectId == Station);
    internal static void Send(SimulationEngine engine, string type, string id = "command") =>
        engine.ReceiveCommand(new(id, 1, Ship, EngineId, type, TargetObjectId: Station));
    internal static double Distance(AuthoritativeSnapshot s) => Math.Sqrt(Math.Pow(Player(s).X - Target(s).X, 2) + Math.Pow(Player(s).Y - Target(s).Y, 2));

    [Fact]
    public void MovingStationSynchronizationSamplesCurrentTangent()
    {
        using var engine = Create(42);
        var initial = At(engine, 0);
        Send(engine, ShipEngineCommandTypes.SpeedSynchronization, "speed");
        At(engine, 0);
        long speedEnd = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Ship).Modules.Single(m => m.ModuleId == EngineId).ActiveCycle!.DurationMs;
        speedEnd = Math.Max(1, speedEnd);
        var speed = At(engine, speedEnd);
        Assert.Equal(Target(speed).SpeedKmS, Player(speed).SpeedKmS);
        Send(engine, ShipEngineCommandTypes.DirectionSynchronization, "course");
        At(engine, speedEnd);
        long end = speedEnd + engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Ship).Modules.Single(m => m.ModuleId == EngineId).ActiveCycle!.DurationMs;
        end = Math.Max(speedEnd + 1, end);
        var aligned = At(engine, end);
        Assert.NotEqual(Target(initial).Direction, Target(aligned).Direction);
        Assert.Equal(Target(aligned).Direction, Player(aligned).Direction);
        Assert.Equal(Target(aligned).SpeedKmS, Player(aligned).SpeedKmS, 12);
        Assert.Null(Player(aligned).Orbit);
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(42UL)]
    public void OrbitalApproachVisitIsFinite(ulong seed)
    {
        using var engine = Create(seed);
        var initial = At(engine, 0);
        double speed = Player(initial).SpeedKmS;
        long budget = (long)Math.Max(2 * Distance(initial) / (speed * 10) * 1000, 10 * GameCalendar.DayMs / 300);
        Send(engine, NavigationComputerCommandTypes.Approach);
        At(engine, 0);
        AuthoritativeSnapshot snapshot = initial;
        for (long time = 1000; time <= budget; time += 1000)
        {
            snapshot = At(engine, time);
            Assert.Equal(speed, Player(snapshot).SpeedKmS);
            if (Distance(snapshot) <= 100) return;
        }
        Assert.Fail($"seed={seed}, budget={budget}, distance={Distance(snapshot)}, ship={Player(snapshot)}");
    }

    [Fact]
    public void CancelRepeatAndUnreachableTarget()
    {
        using var engine = Create(42);
        Send(engine, NavigationComputerCommandTypes.Approach);
        var first = At(engine, 0);
        var moving = At(engine, 1000);
        Send(engine, ShipEngineCommandTypes.MaintainCourse, "cancel");
        var cancelled = At(engine, 1000);
        Assert.Equal(Player(moving).X, Player(cancelled).X);
        Assert.Equal(Player(moving).Y, Player(cancelled).Y);
        Assert.Null(Player(cancelled).ApproachRoute);
        cancelled = At(engine, 3000);
        Send(engine, NavigationComputerCommandTypes.Approach, "repeat");
        var repeated = At(engine, 3000);
        Assert.Equal(Player(cancelled).X, Player(repeated).X);
        Assert.True(Player(repeated).ApproachRoute is not null, string.Join("; ", repeated.CommandResults));
        Assert.Equal(Player(first).SpeedKmS, Player(repeated).SpeedKmS);

        // Faster target moving away: the unchanged planner chooses a finite fallback,
        // not a false rendezvous. Its endpoint differs from the future target pose.
        var slow = new ObjectMotionSnapshot("slow", 0, 0, 1, 90);
        var route = DeepSpaceSaga.Motion.ApproachLineCaptureMath.Plan(slow, 1000, 0, 90, 10, 10, 4)!;
        var endpoint = DeepSpaceSaga.Motion.ApproachLineCaptureMath.Predict(slow with { ApproachRoute = route }, route.DurationMs);
        double targetEndX = 1000 + 100 * route.DurationMs / 1000;
        Assert.True(Math.Abs(endpoint.X - targetEndX) > 1000);
        Assert.Equal(1, endpoint.SpeedKmS);
    }
}

