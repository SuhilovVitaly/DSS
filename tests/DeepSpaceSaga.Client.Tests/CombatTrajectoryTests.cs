using System.Collections.Immutable;
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
        [new(0, new(100, 160, 0, 3, 0, 2000), 1)], 12000, ExpiresAtMotionTimeMs: 300_000);
    private static readonly ObjectMotionSnapshot Torpedo = new("torpedo", 100, 100, 3, 0,
        RenderObjectType: SpaceObjectType.Missile, Torpedo: Flight);
    private static AuthoritativeSnapshot Snapshot => new(1, 9000000000, SimulationSpeed.Speed0,
        [Player, Target, Torpedo, new("other", 300, 100, 0, 0)], "player", SimulationTimeMs: 2000);
    private static long Ticks(int ms) => Stopwatch.Frequency * ms / 1000;

    private static void ObserveStableTarget(SnapshotBuffer buffer, AuthoritativeSnapshot snapshot)
    {
        // The geometry tests start after the target's observation window.
        for (long time = 0; time < snapshot.MotionTimeMs; time += 1000)
            buffer.Update(snapshot with { SnapshotSequence = 0, SimulationTimeMs = time });
    }

    [Fact]
    public void Recorded_target_corrections_do_not_permanently_hide_player_flight()
    {
        // CombatMetrics 20261018-090739, SPC-MUKHA-02, motion 18000..21400.
        (long Time, double X, double Y, double Heading, double Turn)[] samples =
        [
            (0, 1181.6233528884936, -1279.9823410557951, 212.3972785128924, -1.8238930773156428),
            (100, 1177.3478179006984, -1273.2207031977337, 212.21488920516083, -0.9224544761639208),
            (200, 1173.0884990523018, -1266.4488357209302, 212.12264375754444, 0.27754552383607933),
            (300, 1168.8329915943173, -1259.6745715525085, 212.15039830992805, -0.9224544761639208),
            (400, 1164.5812977105952, -1252.8979141689, 212.05815286231166, -1.5615032085304392),
            (500, 1160.3443041605806, -1246.1120578453392, 211.90200254145861, -2.7615032085304394),
            (600, 1156.132943352167, -1239.3102701208447, 211.62585222060557, -1.5615032085304392),
            (700, 1151.9472696757002, -1232.492638841507, 211.46970189975252, -0.36150320853043905),
            (800, 1147.7730416025624, -1225.6679910212679, 211.43355157889948, -0.23284724450832073),
            (900, 1143.6023537808953, -1218.8411790117455, 211.41026685444865, 0.4770782466715673),
            (1000, 1139.4302111353493, -1212.0152562135074, 211.45797467911575, -0.7229217533284329),
            (1100, 1135.2595331681605, -1205.188438740962, 211.38568250378285, -1.5835803690140438),
            (1200, 1131.102605724958, -1198.353242072923, 211.22732446688144, -0.3835803690140436),
            (1300, 1126.9574169471098, -1191.5109173506064, 211.18896642998004, -1.5835803690140438),
            (1400, 1122.8239815342474, -1184.6614888382035, 211.03060839307864, -0.3835803690140436),
            (1500, 1118.702309190561, -1177.8049726163226, 210.99225035617724, -1.5835803690140438),
            (1600, 1114.5924145322883, -1170.941392999481, 210.83389231927583, -0.3835803690140436),
            (1700, 1110.4943072083522, -1164.0707661015097, 210.79553428237443, 0.8164196309859566),
            (1800, 1106.3936052498402, -1157.201688097974, 210.87717624547304, -0.3835803690140436),
            (1900, 1102.290308701141, -1150.3341590632297, 210.83881820857164, -1.350420333742477),
            (2000, 1098.1974096695294, -1143.4604302433106, 210.7037761751974, -2.398308458818974),
            (2100, 1094.1270211279095, -1136.5733522218493, 210.4639453293155, -1.1983084588189739),
            (2200, 1090.0782664427406, -1129.6735288784137, 210.3441144834336, 0.0016915411810263237),
            (2300, 1086.0367183349338, -1122.7694801465293, 210.3442836375517, -0.004269381070116651),
            (2400, 1081.9951857584217, -1115.865422322851, 210.3438566994447, 0.5556360132612781),
            (2500, 1077.9503318646312, -1108.963310185095, 210.39942030077077, -0.6443639867387221),
            (2600, 1073.906012466533, -1102.0608849692278, 210.33498390209684, -1.004227036081602),
            (2700, 1069.8716278460377, -1095.1526489896153, 210.23456119848868, 0.19577296391839827),
            (2800, 1065.8421175575365, -1088.2415675827592, 210.25413849488052, -1.004227036081602),
            (2900, 1061.8174845990354, -1081.32764589059, 210.15371579127236, -0.16535871397991286),
            (3000, 1057.799909971741, -1074.4096191726674, 210.13717991987437, -1.365358713979913),
            (3100, 1053.7915809811354, -1067.4862335545813, 210.00064404847637, -0.16535871397991286),
            (3200, 1049.7925029105425, -1060.557498161952, 209.98410817707838, 0.6946827638512332),
            (3300, 1045.7902249600634, -1053.6306111723354, 210.0535764534635, -0.505317236148767),
            (3400, 1041.7868022627038, -1046.7043854643575, 210.00304472984863, -0.8251632086341942),
        ];
        var buffer = new SnapshotBuffer(() => 0);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        var origin = Player with { X = 200, Y = -280, SpeedKmS = 10, Direction = 74 };
        var first = samples[0];
        var target = Target with { X = first.X, Y = first.Y, Direction = first.Heading, SpeedKmS = 8, MaxSpeedKmS = 8 };
        var route = TorpedoGuidanceMath.Plan(origin, target, 10, 90);
        var projectile = origin with
        {
            ObjectId = "torpedo",
            Torpedo = new("player", "launcher", "target", 0, 10, 90, 150, 0, route, ExpiresAtMotionTimeMs: 120000)
        };
        int visible = 0;
        foreach (var sample in samples)
        {
            var current = target with
            {
                X = sample.X,
                Y = sample.Y,
                Direction = sample.Heading,
                HostileCombat = new("player", "engine", "weapon", 50, false, HostileCombatPhase.FlankApproach,
                    new(sample.Time, 100, 0, sample.X, sample.Y, 8, sample.Heading, 0, sample.Turn))
            };
            buffer.Update(new((ulong)sample.Time + 1, sample.Time, SimulationSpeed.Speed0,
                [Player, current, new LinearMotionPredictor().Predict(projectile, sample.Time)], "player", SimulationTimeMs: sample.Time));
            Render(screen);
            if (sample.Time < 2000)
            {
                Assert.True(screen.CombatTrajectories["torpedo"].Prediction.Count >= 2);
                Assert.Null(screen.CombatTrajectories["torpedo"].Intercept);
                Assert.Null(screen.CombatTrajectories["torpedo"].EtaSeconds);
                continue;
            }
            Assert.True(buffer.HasStableNpcMotion("target"));
            Assert.Equal(0, buffer.GetNpcObservedTurnRate("target"));
            var geometry = screen.CombatTrajectories["torpedo"];
            Assert.NotNull(geometry.Intercept);
            var expected = new LinearMotionPredictor().Predict(current with { HostileCombat = null },
                (long)Math.Round(geometry.EndMotionTimeMs - sample.Time));
            Assert.InRange(Math.Abs(expected.X - geometry.Target[^1].X), 0, .08);
            Assert.InRange(Math.Abs(expected.Y - geometry.Target[^1].Y), 0, .08);
            visible++;
        }
        Assert.Equal(15, visible);
        screen.OnDeactivated();
    }

    [Fact]
    public void Player_flight_remains_visible_but_contact_waits_for_stable_target()
    {
        long now = 0;
        ulong sequence = 0;
        var predictor = new LinearMotionPredictor();
        var target = Target with { SpeedKmS = 1, Direction = 90 };
        var route = TorpedoGuidanceMath.Plan(Player, target, 3, 90);
        var torpedo = Torpedo with { X = Player.X, Y = Player.Y, Torpedo = Flight with { Route = route, Trail = [] } };
        var enemy = torpedo with { ObjectId = "enemy-flight", Torpedo = torpedo.Torpedo! with { OwnerObjectId = "enemy" } };
        var buffer = new SnapshotBuffer(() => now);
        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => now);
        void Frame(long time, double speed = 1, double heading = 90)
        {
            buffer.Update(new(++sequence, time, SimulationSpeed.Speed0,
                [Player, predictor.Predict(target, time) with { SpeedKmS = speed, Direction = heading },
                    predictor.Predict(torpedo, time), predictor.Predict(enemy, time)], "player", SimulationTimeMs: time));
            Render(screen);
        }
        void AssertProvisional()
        {
            var geometry = screen.CombatTrajectories["torpedo"];
            Assert.True(geometry.Prediction.Count >= 2);
            Assert.Null(geometry.Intercept);
            Assert.Null(geometry.EtaSeconds);
            Assert.Equal(CombatForecastStatus.InsufficientData, geometry.Status);
        }
        Frame(0);
        AssertProvisional();
        Assert.Contains("enemy-flight", screen.CombatTrajectories.Keys);
        Frame(1000);
        Frame(1999);
        AssertProvisional();
        now += Ticks(10000);
        Render(screen); // Waiting on pause does not complete the physical-time window.
        AssertProvisional();
        Frame(2000);
        Assert.NotNull(screen.CombatTrajectories["torpedo"].Intercept);
        Frame(2100, speed: 2);
        AssertProvisional();
        Frame(3100, speed: 2);
        Frame(4099, speed: 2);
        AssertProvisional();
        Frame(4100, speed: 2);
        Assert.Contains("torpedo", screen.CombatTrajectories.Keys);
        Frame(4200, speed: 2, heading: 91);
        Assert.Contains("torpedo", screen.CombatTrajectories.Keys);
        Frame(5000, speed: 2, heading: 99);
        Frame(6000, speed: 2, heading: 108);
        AssertProvisional();
        Frame(0); // A replaced timeline cannot reuse the old stability window.
        AssertProvisional();
        screen.OnDeactivated();
    }

    [Fact]
    public void Continually_accelerating_target_does_not_hide_player_torpedo_path()
    {
        var predictor = new LinearMotionPredictor();
        var buffer = new SnapshotBuffer(() => 0);
        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => 0);
        var target = Target with { SpeedKmS = 1, Direction = 90 };
        var route = TorpedoGuidanceMath.Plan(Player, target, 3, 90);
        var torpedo = Torpedo with { X = Player.X, Y = Player.Y, Torpedo = Flight with { Route = route, Trail = [] } };
        for (long time = 0; time <= 3000; time += 100)
        {
            buffer.Update(new((ulong)(time + 1), time, SimulationSpeed.Speed0,
                [Player, predictor.Predict(target, time) with { SpeedKmS = 1 + time / 10000d },
                    predictor.Predict(torpedo, time)], "player", SimulationTimeMs: time));
            Render(screen);
            Assert.False(buffer.HasStableNpcMotion("target"));
            var geometry = screen.CombatTrajectories["torpedo"];
            Assert.True(geometry.Prediction.Count >= 2);
            Assert.Null(geometry.Intercept);
            Assert.Null(geometry.EtaSeconds);
        }
        screen.OnDeactivated();
    }

    [Fact]
    public void Missing_torpedo_expiry_invalidates_a_previously_displayed_contact()
    {
        var buffer = new SnapshotBuffer(() => 0);
        ObserveStableTarget(buffer, Snapshot);
        buffer.Update(Snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        screen.ForecastCache.UseWorker = false;
        Render(screen);
        Assert.NotNull(screen.CombatTrajectories["torpedo"].Intercept);
        buffer.Update(Snapshot with
        {
            SnapshotSequence = 2,
            Objects = Snapshot.Objects.Select(o => o.Torpedo is { } flight
                ? o with { Torpedo = flight with { ExpiresAtMotionTimeMs = null } } : o).ToImmutableArray()
        });
        Render(screen);
        var geometry = screen.CombatTrajectories["torpedo"];
        Assert.Equal(CombatForecastStatus.InsufficientData, geometry.Status);
        Assert.Null(geometry.Intercept);
        Assert.Null(geometry.EtaSeconds);
        Assert.InRange(geometry.EndMotionTimeMs, 2000, 2100);
        screen.OnDeactivated();
    }

    [Fact]
    public void Forecast_starts_from_the_displayed_clamped_hostile_pose_not_an_older_conditional_pose()
    {
        long now = 0;
        var target = Target with
        {
            SpeedKmS = 1,
            Direction = 90,
            MaxSpeedKmS = 2,
            HostileCombat = new("player", "engine", "weapon", 50, true, HostileCombatPhase.Pursuit,
                new(0, 100, 0, Target.X, Target.Y, 1, 90, 0, 20))
        };
        var route = TorpedoGuidanceMath.Plan(Player, target, 3, 90, 2000);
        var torpedo = Torpedo with { X = 0, Y = 0, Torpedo = Flight with { Route = route, Trail = [] } };
        var buffer = new SnapshotBuffer(() => now);
        var initial = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed1, [Player, target, torpedo], "player", SimulationTimeMs: 2000);
        ObserveStableTarget(buffer, initial);
        buffer.Update(initial);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        screen.ForecastCache.UseWorker = false;
        Render(screen);
        now = Ticks(500);
        Render(screen);
        var displayed = screen.RenderStates.Single(s => s.Source.ObjectId == "target").Predicted;
        var geometry = screen.CombatTrajectories["torpedo"];
        Assert.NotNull(geometry.Intercept);
        // Speed1 advances physical motion at 1/4 real time: 500 real ms = 125 motion ms.
        double start = 2000 + 500 * SimulationSpeed.Speed1.MotionTimeMultiplier();
        var observed = FutureTrajectoryProjector.ObservedNpcForecastState(displayed, buffer.GetNpcObservedTurnRate("target"));
        var expected = CurrentManeuverForecast.Sample(observed, geometry.EndMotionTimeMs - start);
        Assert.Equal(expected.X, geometry.Target[^1].X, 6);
        Assert.Equal(expected.Y, geometry.Target[^1].Y, 6);
        Assert.Equal(start, screen.ForecastCache.Results["torpedo"].StartMotionTimeMs);
        screen.OnDeactivated();
    }

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
        // Complete the observed constant-velocity window before checking geometry.
        buffer.Update(Frame(3000, SimulationSpeed.Speed0, 3));
        Render(screen);
        CheckAlignment();
        Assert.Equal(-103, screen.RenderStates.Single(s => s.Source.ObjectId == "target").Pose.Y);
        // Ordinary map contacts retain the existing frozen visual anchor policy.
        Assert.Equal(-1, screen.RenderStates.Single(s => s.Source.ObjectId == "other").Pose.Y);
        now += Ticks(1000);
        Render(screen);
        CheckAlignment();
        buffer.Update(Frame(3100, SimulationSpeed.Speed1, 4));
        Render(screen);
        CheckAlignment();
        now += Ticks(20);
        buffer.Update(Frame(3400, SimulationSpeed.Speed1, 5));
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
            AssertContactDistance(geometry);
        }
    }

    [Fact]
    public void Flight_lines_survive_selection_change_and_pause()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        ObserveStableTarget(buffer, Snapshot);
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
            Assert.True(bitmap.GetPixel(1060, 345).Alpha > 100); // confirmed encounter cross
        };
        screen.ForecastCache.UseWorker = false;
        screen.Render(canvas, 1920, 1080);
        string geometry = JsonSerializer.Serialize(screen.CombatTrajectories["torpedo"]);
        screen.OnMouseDown(1260, 640);
        Assert.Equal("other", screen.SelectedObjectId);
        now = Ticks(10000);
        screen.ForecastCache.UseWorker = false;
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
        AssertContactDistance(geometry);
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
    public void Unknown_target_does_not_disclose_conditional_hostile_motion()
    {
        var target = Target with
        {
            RenderObjectType = SpaceObjectType.UnknownSpaceObject,
            SpeedKmS = 1,
            HostileCombat = new("player", "engine", "weapon", 50, true, HostileCombatPhase.Pursuit,
                new(2000, 100, 0, Target.X, Target.Y, 1, 0, 0, 20))
        };
        var predictor = new LinearMotionPredictor();
        var geometry = CombatTrajectoryProjector.Project(Flight, target, predictor, new(0, 0, 1), 800, 600);
        Assert.Null(geometry.Intercept);
        Assert.Empty(geometry.Target);
        var projector = new FutureTrajectoryProjector(predictor);
        Assert.Empty(projector.Project(target));
        var points = new List<FutureTrajectoryPoint>();
        projector.ProjectViewportInto(target, points, new(0, 0, 1), 800, 600);
        Assert.Empty(points);
    }

    [Fact]
    public void No_contact_keeps_both_lines_to_a_physical_horizon()
    {
        var target = Target with { SpeedKmS = 20, Direction = 0 };
        var flight = Flight with { Route = Flight.Route with { HasIntercept = false }, PredictedImpactMotionTimeMs = null };
        var camera = new CameraState(100, 0, 1);
        var geometry = CombatTrajectoryProjector.Project(flight, target, new LinearMotionPredictor(), camera, 800, 600);
        Assert.Null(geometry.Intercept);
        Assert.NotEmpty(geometry.Target);
        Assert.Equal(CombatForecastStatus.NoContact, geometry.Status);
        Assert.Equal(200, geometry.HorizonSeconds);
        Assert.Equal(geometry.Prediction, CombatTrajectoryProjector.Project(flight, target, new LinearMotionPredictor(), new(0, 0, .1), 400, 300).Prediction);
        var lost = CombatTrajectoryProjector.Project(Flight, null, new LinearMotionPredictor(), camera, 800, 600);
        Assert.Null(lost.Intercept);
        Assert.Equal(CombatForecastStatus.TargetLost, lost.Status);
    }

    [Fact]
    public void Turning_target_replaces_old_aim_with_a_conditional_contact()
    {
        var predictor = new LinearMotionPredictor();
        var target = Target with { SpeedKmS = 1, Direction = 90 };
        var route = TorpedoGuidanceMath.Plan(Player, target, 3, 90);
        var flight = Flight with { Route = route, Trail = [] };
        target = target with { TurnStepDegrees = 90, TurnStepRemainingMs = 100, TurnStepIntervalMs = 1000 };
        string before = JsonSerializer.Serialize(flight);
        var oldAim = TorpedoGuidanceMath.PredictPose(route, route.Segments.Sum(s => s.DurationMs));
        var geometry = CombatTrajectoryProjector.Project(flight, target, predictor, new(0, 0, 1), 1920, 1080);
        AssertContactDistance(geometry);
        Assert.NotEqual(new FutureTrajectoryPoint(oldAim.X, oldAim.Y), geometry.Prediction[^1]);
        Assert.Equal(before, JsonSerializer.Serialize(flight));
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1)]
    [InlineData(3)]
    public void Conditional_forecast_continues_beyond_confirmed_AI_watermark(double zoom)
    {
        var target = Target with
        {
            SpeedKmS = 1,
            MaxSpeedKmS = 2,
            HostileCombat = new("player", "engine", "weapon", 50, false, HostileCombatPhase.Pursuit,
                new(1971, 100, 29, Target.X, Target.Y, 1, 0, 0, 20))
        };
        var geometry = CombatTrajectoryProjector.Project(Flight, target, new LinearMotionPredictor(), new(100, 0, zoom), 1920, 1920);
        AssertContactDistance(geometry);
        Assert.True(geometry.EndMotionTimeMs > 2071);
        Assert.Equal(new LinearMotionPredictor().Predict(target, 100), new LinearMotionPredictor().Predict(target, 20_000));
        Assert.Equal(new FutureTrajectoryPoint(Torpedo.X, Torpedo.Y), geometry.Prediction[0]);
    }

    private static void AssertContactDistance(CombatTrajectoryProjector.Geometry geometry)
    {
        Assert.NotNull(geometry.Intercept);
        double dx = geometry.Prediction[^1].X - geometry.Target[^1].X;
        double dy = geometry.Prediction[^1].Y - geometry.Target[^1].Y;
        Assert.InRange(Math.Sqrt(dx * dx + dy * dy), 4.99999, 5.00001);
        Assert.NotNull(geometry.EtaSeconds);
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
        ObserveStableTarget(buffer, Snapshot);
        buffer.Update(Snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        Render(screen);
        var prediction = screen.CombatTrajectories["torpedo"].Prediction;
        Assert.Equal(new FutureTrajectoryPoint(100, 100), prediction[0]);
        Assert.InRange(prediction[^1].Y, -195.00001, -194.99999);
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
            Assert.True(behind.Blue > ahead.Blue && behind.Alpha > 0, $"Expected fading blue exhaust, got {behind}");
            Assert.True(behind.Alpha > ahead.Alpha, $"Exhaust must point behind the torpedo: {behind}, {ahead}");
        };
        screen.ForecastCache.UseWorker = false;
        screen.Render(canvas, 1920, 1080);
        Assert.True(observed);
        screen.OnDeactivated();
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.ForecastCache.UseWorker = false;
        screen.Render(canvas, 1920, 1080);
    }
}
