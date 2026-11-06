using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class NpcObservedTrajectoryTests
{
    private static readonly ObjectMotionSnapshot Npc = new("npc", 100, 50, 1, 90,
        RenderObjectType: SpaceObjectType.NpcShip);
    private static long Ticks(long ms) => ms * Stopwatch.Frequency / 1000;
    private static AuthoritativeSnapshot State(long time, double direction, ObjectMotionSnapshot? npc = null) =>
        new((ulong)time + 1, time * 300, SimulationSpeed.Speed1,
            [(npc ?? Npc) with { Direction = direction }], SimulationTimeMs: time);

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

    [Theory]
    [InlineData(90)]
    [InlineData(80)]
    [InlineData(105)]
    public void ChangedTurnImmediatelyReturnsToStraight(double nextHeading)
    {
        var buffer = LegacyTrend();
        buffer.Update(State(3000, nextHeading));
        Assert.Equal(0, buffer.GetNpcObservedTurnRate("npc"));
    }

    [Fact]
    public void StraightThenTurnNeedsTwoIntervals()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(State(0, 70)); buffer.Update(State(1000, 70)); buffer.Update(State(2000, 70));
        buffer.Update(State(3000, 80));
        Assert.Equal(0, buffer.GetNpcObservedTurnRate("npc"));
        buffer.Update(State(4000, 90));
        Assert.Equal(10, buffer.GetNpcObservedTurnRate("npc"));
    }

    [Fact]
    public void PauseAndSpeedDoNotInventSeconds()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.Update(State(0, 70)); buffer.Update(State(1000, 80));
        for (int i = 0; i < 100; i++)
        {
            now += Ticks(1000);
            buffer.CurrentSpeed = i % 2 == 0 ? SimulationSpeed.Speed0 : SimulationSpeed.Speed4;
            _ = buffer.LatestPrediction;
            buffer.Update(State(1000, 80));
        }
        Assert.Equal(0, buffer.GetNpcObservedTurnRate("npc"));
        buffer.Update(State(2000, 90));
        Assert.Equal(10, buffer.GetNpcObservedTurnRate("npc"));
    }

    [Theory]
    [InlineData("removed")]
    [InlineData("unknown")]
    [InlineData("destroyed")]
    [InlineData("rollback")]
    [InlineData("duplicate-pose-change")]
    [InlineData("gap")]
    public void LoadRemovalUnknownAndDestroyedResetObservation(string kind)
    {
        var buffer = LegacyTrend();
        var next = State(3000, 100);
        next = kind switch
        {
            "removed" => next with { Objects = [] },
            "unknown" => next with { Objects = [Npc with { RenderObjectType = SpaceObjectType.UnknownSpaceObject }] },
            "destroyed" => next with { Objects = [Npc with { IsDestroyed = true }] },
            "rollback" => State(1000, 80) with { SnapshotSequence = 4001 },
            "duplicate-pose-change" => State(2000, 90, Npc with { X = 101 }),
            "gap" => State(3001, 100),
            _ => next
        };
        buffer.Update(next);
        Assert.Equal(0, buffer.GetNpcObservedTurnRate("npc"));
        if (kind is "removed" or "unknown" or "destroyed")
        {
            buffer.Update(State(4000, 110));
            Assert.Equal(0, buffer.GetNpcObservedTurnRate("npc"));
        }
        Assert.Equal(0, new SnapshotBuffer(() => 0).GetNpcObservedTurnRate("npc"));
    }

    [Fact]
    public async Task CameraAndSelectionPreserveTrend()
    {
        var buffer = new SnapshotBuffer(() => 0);
        var player = new ObjectMotionSnapshot("player", 10000, 10000, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip);
        for (int i = 0; i <= 2; i++)
            buffer.Update(State(i * 1000, 70 + i * 10, Npc with { X = 10050, Y = 10000 }) with
            { Objects = [player, Npc with { X = 10050, Y = 10000, Direction = 70 + i * 10 }], PlayerShipObjectId = "player", SelectedObjectId = "npc", CurrentSpeed = SimulationSpeed.Speed0 });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        TacticalMapSnapshotDocument? captured = null;
        screen.SnapshotWriter = (document, _) => { captured = document; return "in-memory"; };
        var rect = screen.MapViewButtonRects[4];
        screen.OnMouseDown(rect.MidX, rect.MidY);
        screen.Render(canvas, 1920, 1080);
        await screen.SnapshotSaveTask;
        Assert.NotNull(captured);
        var trajectory = Assert.Single(captured.State.Trajectories, t => t.ObjectId == "npc" && t.Kind == "future");
        var pose = captured.State.Objects.Single(o => o.Authoritative.ObjectId == "npc").Rendered;
        Assert.Equal(pose.X, trajectory.Points[0].X, 8);
        Assert.Equal(pose.Y, trajectory.Points[0].Y, 8);
        // The real draw call must pass the measured 10 deg/s, not its optional zero default.
        double w = Math.PI / 18;
        Assert.Equal(pose.X + 10 * Math.Sin(w) / w, trajectory.Points[4].X, 8);
        Assert.Equal(pose.Y + 10 * (1 - Math.Cos(w)) / w, trajectory.Points[4].Y, 8);
        screen.OnMouseDown(1700, 700); // selection changes do not authorize observations
        screen.Render(canvas, 1280, 720); // viewport/camera changes do not reset history
        Assert.Equal(10, buffer.GetNpcObservedTurnRate("npc"));
        screen.OnDeactivated();
    }

    private sealed class RejectingPredictor : IMotionPredictor
    {
        public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long deltaMs) =>
            throw new InvalidOperationException("NPC projection must never read hidden motion controls.");
    }
}
