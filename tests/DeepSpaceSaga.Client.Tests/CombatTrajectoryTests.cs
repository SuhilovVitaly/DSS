using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatTrajectoryTests
{
    private static readonly ObjectMotionSnapshot Player = new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip);
    private static readonly ObjectMotionSnapshot Target = new("target", 100, -200, 0, 0, RenderObjectType: SpaceObjectType.NpcShip);
    private static readonly TorpedoSnapshot Flight = new("player", "launcher", "target", 0, 3, 90, 150, 60,
        new(0, 1, TorpedoRoutePhase.Straight, true, [new(100, 160, 0, 3, 0, 12000)], 2000),
        [new(0, new(100, 160, 0, 3, 0, 2000), 1)], 12000);
    private static readonly ObjectMotionSnapshot Torpedo = new("torpedo", 100, 100, 3, 0,
        RenderObjectType: SpaceObjectType.Missile, Torpedo: Flight);
    private static AuthoritativeSnapshot Snapshot => new(1, 9000000000, SimulationSpeed.Speed0,
        [Player, Target, Torpedo, new("other", 300, 100, 0, 0)], "player", SimulationTimeMs: 2000);
    private static long Ticks(int ms) => Stopwatch.Frequency * ms / 1000;

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    public void Combat_target_marker_and_line_share_pose_through_pause_and_reconciliation(int predictionLeadMs)
    {
        long now = 0;
        var predictor = new LinearMotionPredictor();
        var target = Target with { X = 200, Y = -100, SpeedKmS = .1 };
        var route = TorpedoGuidanceMath.Plan(Player, target, 3, 90);
        var torpedo = Torpedo with { X = 0, Y = 0, Torpedo = Flight with { Route = route, Trail = [] } };
        var other = new ObjectMotionSnapshot("other", 500, 0, .1, 0);
        var buffer = new SnapshotBuffer(() => now);
        AuthoritativeSnapshot Frame(long time, SimulationSpeed speed, ulong sequence) => new(sequence, 9000000000, speed,
            [Player, predictor.Predict(target, time), predictor.Predict(torpedo, time), predictor.Predict(other, time)],
            "player", SimulationTimeMs: time);
        buffer.Update(Frame(1000, SimulationSpeed.Speed1, 1));
        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => now);
        Render(screen);
        Assert.Equal(-101, screen.RenderStates.Single(s => s.Source.ObjectId == "target").Pose.Y);
        now = Ticks(1000 + predictionLeadMs);
        buffer.Update(Frame(2000, SimulationSpeed.Speed0, 2));
        Render(screen);
        CheckAlignment();
        Assert.Equal(-102, screen.RenderStates.Single(s => s.Source.ObjectId == "target").Pose.Y);
        // Ordinary map contacts retain the existing frozen visual anchor policy.
        Assert.Equal(-1, screen.RenderStates.Single(s => s.Source.ObjectId == "other").Pose.Y);
        now += Ticks(1000);
        Render(screen);
        CheckAlignment();
        buffer.Update(Frame(2100, SimulationSpeed.Speed1, 3));
        Render(screen);
        CheckAlignment();
        now += Ticks(20);
        buffer.Update(Frame(2400, SimulationSpeed.Speed1, 4));
        Render(screen);
        CheckAlignment();
        screen.OnDeactivated();

        void CheckAlignment()
        {
            var pose = screen.RenderStates.Single(s => s.Source.ObjectId == "target").Pose;
            var geometry = screen.CombatTrajectories["torpedo"];
            Assert.NotEmpty(geometry.Target);
            Assert.Equal(pose.X, geometry.Target[0].X, 8);
            Assert.Equal(pose.Y, geometry.Target[0].Y, 8);
            Assert.NotNull(geometry.Intercept);
            Assert.Equal(geometry.Intercept.Value.X, geometry.Target[^1].X, 6);
            Assert.Equal(geometry.Intercept.Value.Y, geometry.Target[^1].Y, 6);
        }
    }

    [Fact]
    public void Flight_lines_survive_selection_change_and_pause()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.Update(Snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), showTrajectoryPrediction: false, timestampProvider: () => now);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        bool observed = false;
        screen.RenderStageCompleted = stage =>
        {
            if (stage == "trails") canvas.Clear(SKColors.Transparent);
            if (stage != "combat_trajectories") return;
            observed = true;
            var solid = bitmap.GetPixel(1060, 680);
            Assert.Equal(0, solid.Alpha); // No travelled path behind the torpedo.
            int dashPixels = Enumerable.Range(420, 100).Count(y => bitmap.GetPixel(1060, y).Alpha > 20);
            Assert.InRange(dashPixels, 30, 80);
            Assert.All(Enumerable.Range(420, 100), y => Assert.InRange(bitmap.GetPixel(1060, y).Alpha, (byte)0, (byte)102));
            Assert.True(bitmap.GetPixel(1060, 340).Alpha > 100); // confirmed encounter cross
        };
        screen.Render(canvas, 1920, 1080);
        string geometry = JsonSerializer.Serialize(screen.CombatTrajectories["torpedo"]);
        screen.OnMouseDown(1260, 640);
        Assert.Equal("other", screen.SelectedObjectId);
        now = Ticks(10000);
        screen.Render(canvas, 1920, 1080);
        Assert.True(observed);
        Assert.Equal(geometry, JsonSerializer.Serialize(screen.CombatTrajectories["torpedo"]));
        Assert.Equal("target", buffer.Latest!.Snapshot.Objects.Single(o => o.Torpedo is not null).Torpedo!.TargetObjectId);
        screen.OnDeactivated();
    }

    [Fact]
    public void Target_line_ends_at_same_predicted_encounter()
    {
        var target = Target with { SpeedKmS = 1, Direction = 90 };
        var origin = Torpedo with { Y = 160, Torpedo = null };
        var route = TorpedoGuidanceMath.Plan(origin, target, 3, 90);
        const long elapsed = 2000;
        var flight = Flight with { Route = route with { ElapsedMs = elapsed }, Trail = [] };
        var predictor = new LinearMotionPredictor();
        var geometry = CombatTrajectoryProjector.Project(flight, predictor.Predict(target, elapsed), predictor, new(0, 0, 1), 1920, 1080);
        Assert.True(route.HasIntercept);
        Assert.NotNull(geometry.Intercept);
        Assert.Equal(geometry.Prediction[^1], geometry.Intercept);
        Assert.Equal(geometry.Intercept!.Value.X, geometry.Target[^1].X, 6);
        Assert.Equal(geometry.Intercept.Value.Y, geometry.Target[^1].Y, 6);
        var current = TorpedoGuidanceMath.PredictPose(route, elapsed);
        Assert.Equal(new FutureTrajectoryPoint(current.X, current.Y), geometry.Prediction[0]);
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed0)]
    [InlineData(SimulationSpeed.Speed4)]
    public void Impact_hides_all_trajectories_while_explosion_lives_two_real_seconds(SimulationSpeed speed)
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.Update(Snapshot with { Objects = [Player, Target], CurrentSpeed = speed });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        var impact = new CombatImpactSnapshot(1, "torpedo", "player", "launcher", "target", "target", 11833.333,
            100, -195, [new(0, new(100, 160, 0, 3, 0, 11833.333), 1)]);
        // No active-flight snapshot is ever delivered to this screen.
        buffer.Update(Snapshot with { SnapshotSequence = 2, Objects = [Player, Target], CombatImpacts = [impact], CurrentSpeed = speed });
        now = Ticks(1000);
        buffer.Update(buffer.Latest!.Snapshot with { SnapshotSequence = 3 });
        Render(screen);
        Assert.Empty(screen.CombatTrajectories);
        Assert.Single(screen.CombatEffects.Active);
        now = Ticks(1999);
        Render(screen);
        Assert.Empty(screen.CombatTrajectories);
        Assert.Single(screen.CombatEffects.Active);
        now = Ticks(2000);
        Render(screen);
        Assert.Empty(screen.CombatTrajectories);
        screen.OnDeactivated();
        screen.OnActivated();
        Render(screen);
        Assert.Empty(screen.CombatTrajectories);
        var loadedScreen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        Render(loadedScreen);
        Assert.Empty(loadedScreen.CombatTrajectories);
        screen.OnDeactivated();
        loadedScreen.OnDeactivated();
    }

    [Fact]
    public void No_intercept_renders_no_false_cross()
    {
        var flight = Flight with { Route = Flight.Route with { HasIntercept = false }, PredictedImpactMotionTimeMs = null };
        var camera = new CameraState(100, 0, 1);
        var geometry = CombatTrajectoryProjector.Project(flight, Target, new LinearMotionPredictor(), camera, 800, 600);
        Assert.Null(geometry.Intercept);
        Assert.Empty(geometry.Target);
        Assert.InRange(geometry.Prediction.Count, 2, 4);
        Assert.Equal(-300, geometry.Prediction[^1].Y);
        var lost = CombatTrajectoryProjector.Project(Flight, null, new LinearMotionPredictor(), camera, 800, 600);
        Assert.Null(lost.Intercept);
    }

    [Fact]
    public void Zoom_changes_screen_geometry_not_flight_state()
    {
        string before = JsonSerializer.Serialize(Flight);
        var camera = new CameraState(0, 0, 1);
        var geometry = CombatTrajectoryProjector.Project(Flight, Target, new LinearMotionPredictor(), camera, 1920, 1080);
        using var path = new SKPath();
        CombatTrajectoryProjector.BuildPath(path, geometry.Prediction, camera, 1920, 1080);
        float x = path.LastPoint.X;
        camera.SetZoom(2);
        camera.SetFocus(10, 0);
        CombatTrajectoryProjector.BuildPath(path, geometry.Prediction, camera, 1920, 1080);
        Assert.NotEqual(x, path.LastPoint.X);
        Assert.Equal(1140, path.LastPoint.X);
        Assert.Equal(before, JsonSerializer.Serialize(Flight));
        CombatTrajectoryProjector.BuildPath(path, [new(-1e12, 0), new(1e12, 0)], camera, 1920, 1080);
        Assert.InRange(path.Bounds.Left, -2.1f, 0);
        Assert.InRange(path.Bounds.Right, 1920, 1922.1f);
    }

    [Fact]
    public void Prediction_starts_at_current_pose_after_session_replacement()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(Snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        Render(screen);
        var prediction = screen.CombatTrajectories["torpedo"].Prediction;
        Assert.Equal(new FutureTrajectoryPoint(100, 100), prediction[0]);
        Assert.Equal(new FutureTrajectoryPoint(100, -200), prediction[^1]);
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(0, 0, 4)]
    [InlineData(90, -4, 0)]
    public void Torpedo_draws_ship_engine_flame_behind_its_current_heading(double heading, int offsetX, int offsetY)
    {
        var flight = Flight with
        {
            Route = new(0, 1, TorpedoRoutePhase.Straight, false, [new(100, 100, heading, 3, 0, 12000)], 0)
        };
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(Snapshot with { Objects = [Player, Torpedo with { Direction = heading, Torpedo = flight }] });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        bool observed = false;
        screen.RenderStageCompleted = stage =>
        {
            if (stage == "combat_trajectories") canvas.Clear(SKColors.Transparent);
            if (stage != "marker_geometry") return;
            observed = true;
            var behind = bitmap.GetPixel(1060 + offsetX, 640 + offsetY);
            var ahead = bitmap.GetPixel(1060 - offsetX, 640 - offsetY);
            Assert.True(behind.Red > behind.Green && behind.Alpha > 100, $"Expected orange exhaust, got {behind}");
            Assert.True(behind.Alpha > ahead.Alpha, $"Exhaust must point behind the torpedo: {behind}, {ahead}");
        };
        screen.Render(canvas, 1920, 1080);
        Assert.True(observed);
        screen.OnDeactivated();
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
    }
}
