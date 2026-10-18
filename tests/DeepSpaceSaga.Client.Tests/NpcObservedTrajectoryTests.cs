using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

public sealed class NpcObservedTrajectoryTests
{
    private static readonly ObjectMotionSnapshot Npc = new("npc", 100, 50, 1, 90,
        RenderObjectType: SpaceObjectType.NpcShip);
    private static readonly Guid Session = Guid.Parse("2d66be90-768c-4226-b327-7e7a809f609d");
    private static long Ticks(long ms) => ms * Stopwatch.Frequency / 1000;
    private static AuthoritativeSnapshot State(long time, double direction, ObjectMotionSnapshot? npc = null) =>
        new((ulong)time + 1, time * 300, SimulationSpeed.Speed1,
            [(npc ?? Npc) with { Direction = direction }], SimulationTimeMs: time);
    private static CombatPresentationUpdate Update(long sequence, AuthoritativeSnapshot state, bool full = false) =>
        new(Session, sequence, state.MotionTimeMs, [new(state.MotionTimeMs, state)], full ? state : null, false, 0);

    private static SnapshotBuffer LegacyTrend()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(State(0, 70)); buffer.Update(State(1000, 80)); buffer.Update(State(2000, 90));
        return buffer;
    }

    [Fact]
    public void UnconfirmedNpcUsesCurrentCourse()
    {
        var state = Npc with { TurnStepDegrees = 90, TurnStepIntervalMs = 1000, TurnStepRemainingMs = 1000 };
        var points = new FutureTrajectoryProjector(new LinearMotionPredictor()).Project(state);
        Assert.Equal(2100, points[^1].X, 8);
        Assert.Equal(50, points[^1].Y, 8);
    }

    [Fact]
    public void HiddenControlsDoNotChangeObservedNpcLine()
    {
        var projector = new FutureTrajectoryProjector(new RejectingPredictor());
        var hidden = Npc with
        {
            ActiveEngineCommandType = ShipEngineCommandTypes.Orbit,
            TurnStepDegrees = 90,
            TurnStepIntervalMs = 1000,
            TurnStepRemainingMs = 1000,
            NavigationTargetX = 999,
            NavigationTargetY = -999,
            HostileCombat = new("player", "engine", "weapon", 50, true, HostileCombatPhase.Pursuit,
                new(2000, 100, 0, Npc.X, Npc.Y, 1, 90, 2, 20))
        };
        Assert.Equal(projector.Project(Npc, 10), projector.Project(hidden, 10));
        Assert.Equal(projector.Project(Npc), projector.Project(hidden));
        var camera = new CameraState(0, 0, 1);
        var a = new List<FutureTrajectoryPoint>(); var b = new List<FutureTrajectoryPoint>();
        projector.ProjectViewportInto(Npc, a, camera, 1920, 1080, 10);
        projector.ProjectViewportInto(hidden, b, camera, 1920, 1080, 10);
        Assert.Equal(a, b);
    }

    [Fact]
    public void NpcWithoutHostileCombatUsesSameObservationRule()
    {
        var buffer = LegacyTrend();
        Assert.Equal(10, buffer.GetNpcObservedTurnRate("npc"));
        var points = new FutureTrajectoryProjector(new RejectingPredictor()).Project(Npc, buffer.GetNpcObservedTurnRate("npc"));
        Assert.True(points[4].Y > Npc.Y);
    }

    [Fact]
    public void PresentedFramesOnlyAffectTrend()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.UpdatePresentation(Update(1, State(0, 70), true));
        now = Ticks(4150);
        buffer.UpdatePresentation(Update(2, State(1000, 80)));
        buffer.UpdatePresentation(Update(3, State(2000, 90), true));
        buffer.Update(State(3000, 100)); // legacy full ingress must be ignored in presentation mode
        Assert.Equal(0, buffer.GetNpcObservedTurnRate("npc"));
        buffer.PreparePresentation();
        Assert.Equal(1000, buffer.Presented!.Snapshot.MotionTimeMs);
        Assert.Equal(0, buffer.GetNpcObservedTurnRate("npc"));
        now = Ticks(8150);
        buffer.PreparePresentation();
        Assert.Equal(10, buffer.GetNpcObservedTurnRate("npc"));
    }

    [Fact]
    public void CatchUpConsumesEveryPresentedFrame()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.UpdatePresentation(Update(1, State(0, 70), true));
        buffer.UpdatePresentation(Update(2, State(2000, 90)) with
        { Frames = [new(1000, State(1000, 80)), new(2000, State(2000, 90))] });
        now = Ticks(2150);
        buffer.PreparePresentation();
        Assert.Equal(10, buffer.GetNpcObservedTurnRate("npc"));
    }

    [Fact]
    public void LegacyAndPresentationObserveEquivalentMovement()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.UpdatePresentation(Update(1, State(0, 70), true));
        for (int i = 1; i <= 20; i++)
            buffer.UpdatePresentation(Update(i + 1, State(i * 100, 70 + i)));
        now = Ticks(2150); buffer.PreparePresentation();
        Assert.Equal(LegacyTrend().GetNpcObservedTurnRate("npc"), buffer.GetNpcObservedTurnRate("npc"), 9);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    public void BothProjectionEntrypointsUseObservedRate(double rate)
    {
        var projector = new FutureTrajectoryProjector(new RejectingPredictor());
        var points = new List<FutureTrajectoryPoint>();
        projector.ProjectViewportInto(Npc, points, new CameraState(0, 0, 1), 1920, 1080, rate);
        Assert.Equal(projector.Project(Npc, rate), points);
        Assert.Equal(FutureTrajectoryProjector.MaxSamplePoints, points.Count);
        var direct = new List<FutureTrajectoryPoint>();
        projector.ProjectInto(Npc, direct, rate);
        Assert.Equal(points, direct);
    }

    [Theory]
    [InlineData(90, 10)]
    [InlineData(-90, -10)]
    public void ObservedArcStartsAtDisplayedPose(double rate, double yOffset)
    {
        var projector = new FutureTrajectoryProjector(new RejectingPredictor());
        var displayed = Npc with { X = 333, Y = 444 };
        var points = projector.Project(displayed, rate);
        Assert.Equal(new FutureTrajectoryPoint(333, 444), points[0]);
        // Quarter circle after one second; r = v / |w|, independent analytic oracle.
        double r = 20 / Math.PI;
        Assert.Equal(333 + r, points[4].X, 8);
        Assert.Equal(444 + Math.Sign(yOffset) * r, points[4].Y, 8);
        var stationary = projector.Project(displayed with { SpeedKmS = 0 }, rate);
        Assert.All(stationary, p => Assert.Equal(points[0], p));
    }

    [Fact]
    public void StraightNpcExtendsToViewportWithoutHiddenTerminal()
    {
        var projector = new FutureTrajectoryProjector(new RejectingPredictor());
        var points = new List<FutureTrajectoryPoint>();
        var camera = new CameraState(0, 0, 1);
        projector.ProjectViewportInto(Npc with { TurnStepDegrees = 90 }, points, camera, 1920, 1080);
        Assert.Equal(new FutureTrajectoryPoint(100, 50), points[0]);
        Assert.True(points[^1].X > points[0].X);
        Assert.Equal(50, points[^1].Y, 8);
        projector.ProjectViewportInto(Npc with { SpeedKmS = 0 }, points, camera, 1920, 1080);
        Assert.Single(points);
    }

    private sealed class RejectingPredictor : IMotionPredictor
    {
        public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long deltaMs) =>
            throw new InvalidOperationException("NPC projection must never read hidden motion controls.");
    }
}

