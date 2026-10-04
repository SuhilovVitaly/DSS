using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class BasicCombatUiFlowTests
{
    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1920, 1080, 1.2f)]
    public async Task Ui_fire_flows_through_real_session_to_torpedo_snapshot(int width, int height, float scale)
    {
        await using var f = await Fixture.Create(width, height, scale);
        Assert.False(f.Button.Enabled);
        f.Select(Fixture.Player);
        Assert.False(f.Button.Enabled);
        f.Select(Fixture.Target);
        Assert.True(f.Button.Enabled);
        for (int shot = 1; shot <= 3; shot++)
        {
            f.HoverFire();
            Assert.NotNull(f.Screen.LaunchPreviewGeometry!.Intercept);
            var preview = Assert.IsType<TorpedoRoute>(f.Screen.LaunchPreviewRoute);
            Assert.DoesNotContain(f.Latest.Objects, o => o.Torpedo is not null);
            Assert.True(f.Button.Enabled);
            var launch = await f.Fire();
            var missile = Assert.Single(launch.Objects, o => o.Torpedo is not null);
            var owner = launch.Objects.Single(o => o.ObjectId == Fixture.Player);
            Assert.Equal((owner.X, owner.Y, owner.Direction), (missile.X, missile.Y, missile.Direction));
            Assert.Equal((owner.X, owner.Y, owner.Direction), (preview.Segments[0].X, preview.Segments[0].Y, preview.Segments[0].Direction));
            Assert.Equal(3, missile.SpeedKmS);
            Assert.Equal(Fixture.Target, missile.Torpedo!.TargetObjectId);
            Assert.Equal(Fixture.Player, missile.Torpedo.OwnerObjectId);
            Assert.Equal(Fixture.Launcher, missile.Torpedo.LauncherModuleId);
            Assert.Equal(SimulationSpeed.Speed0, launch.CurrentSpeed);
            Assert.False(f.Button.Enabled);
            Assert.Null(f.Screen.LaunchPreviewGeometry);
            Assert.Equal("Guiding", f.LauncherStatus);

            long end = missile.Torpedo.PredictedImpactMotionTimeMs!.Value + 1000;
            var impact = await f.AdvanceTo(end);
            Assert.Equal(shot, impact.CombatImpacts.Length);
            Assert.DoesNotContain(impact.Objects, o => o.Torpedo is not null);
            Assert.Equal(150, impact.CombatImpacts[^1].DamageApplied);
            Assert.Equal("Ready", f.LauncherStatus);
            var effect = Assert.Single(f.Screen.CombatEffects.Active);
            Assert.Equal(shot, effect.Impact.EventId);
            Assert.DoesNotContain(missile.ObjectId, f.Screen.CombatTrajectories.Keys);
            // Real-time effect expiry is independent of the paused simulation clock.
            f.UiNow = f.Handle.Buffer.Latest!.ReceivedAtTimestamp + 2 * Stopwatch.Frequency;
            f.Render();
            Assert.Empty(f.Screen.CombatEffects.Active);
            Assert.DoesNotContain(missile.ObjectId, f.Screen.CombatTrajectories.Keys);
            if (shot < 3)
            {
                Assert.Equal(450 - shot * 150, impact.Objects.Single(o => o.ObjectId == Fixture.Target).HullCombat!.CurrentHp);
                f.Select(Fixture.Target);
                Assert.True(f.Button.Enabled);
            }
            else
            {
                Assert.DoesNotContain(impact.Objects, o => o.ObjectId == Fixture.Target);
                var wreck = Assert.Single(impact.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
                Assert.Equal(0, wreck.SpeedKmS);
                Assert.Equal(0, wreck.Direction);
                f.Select(wreck.ObjectId);
                Assert.True(f.Button.Enabled);
                var wreckShot = await f.Fire();
                var flight = Assert.Single(wreckShot.Objects, o => o.Torpedo is not null).Torpedo!;
                Assert.Equal(wreck.ObjectId, flight.TargetObjectId);
                var after = await f.AdvanceTo(flight.PredictedImpactMotionTimeMs!.Value + 1000);
                Assert.Equal(0, after.CombatImpacts[^1].DamageApplied);
                Assert.Equal(wreck, Assert.Single(after.Objects, o => o.ObjectType == SpaceObjectType.Wreck));
                Assert.Equal("Ready", f.LauncherStatus);
            }
        }
        Assert.Null(f.Handle.Failure);
    }

    [Fact]
    public async Task Ui_selection_changes_do_not_retarget_active_torpedo()
    {
        await using var f = await Fixture.Create(1920, 1080, 1f);
        f.Select(Fixture.Target);
        var launched = await f.Fire();
        var original = Assert.Single(launched.Objects, o => o.Torpedo is not null);
        f.Screen.OnKeyDown(Key.Right);
        await f.Wait(s => s.Objects.Single(o => o.ObjectId == Fixture.Player).ActiveEngineCommandType == ShipEngineCommandTypes.TurnRightStep);
        var mid = await f.AdvanceTo(launched.MotionTimeMs + 30000);
        Assert.NotEqual(launched.Objects.Single(o => o.ObjectId == Fixture.Player).Direction,
            mid.Objects.Single(o => o.ObjectId == Fixture.Player).Direction);
        f.Select(Fixture.Player);
        Assert.False(f.Button.Enabled);
        f.Select(original.ObjectId);
        var inspection = Assert.IsType<TorpedoInspectionData>(f.Screen.SelectedOrActiveObjectInfo!.Value.Torpedo);
        Assert.Equal(100, inspection.HitChancePercent);
        Assert.True(inspection.TravelledKm > 0);
        Assert.NotNull(inspection.EtaSeconds);
        Assert.False(f.Button.Enabled);
        var geometry = f.Screen.CombatTrajectories[original.ObjectId];
        Assert.NotEmpty(geometry.Prediction);
        Assert.NotEmpty(geometry.Target);
        Assert.NotNull(geometry.Intercept);
        await f.Handle.Connection.SaveAsync("selection-midflight");
        var saved = ScenarioLoader.LoadFromFile(Path.Combine(f.DirectoryPath, "selection-midflight.json"), true);
        Assert.Equal(Fixture.Target, Assert.Single(saved.GameState.CombatState!.Projectiles).Flight.TargetObjectId);
        await using var restored = LocalGameSessionConnection.CreateFromSaveFile(Fixture.Settings,
            Path.Combine(f.DirectoryPath, "selection-midflight.json"));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var snapshots = restored.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await snapshots.MoveNextAsync());
        var loaded = Assert.Single(snapshots.Current.Objects, o => o.Torpedo is not null);
        Assert.Equal(original.ObjectId, loaded.ObjectId);
        Assert.Equal(Fixture.Target, loaded.Torpedo!.TargetObjectId);
        Assert.Equal(mid.MotionTimeMs, snapshots.Current.MotionTimeMs);
        Assert.NotEmpty(loaded.Torpedo.Trail);
        Assert.Empty(snapshots.Current.CombatImpacts);
        var hit = await f.AdvanceTo(original.Torpedo!.PredictedImpactMotionTimeMs!.Value + 1000);
        Assert.Equal(Fixture.Target, Assert.Single(hit.CombatImpacts).HitObjectId);
        Assert.Equal(300, hit.Objects.Single(o => o.ObjectId == Fixture.Target).HullCombat!.CurrentHp);
        Assert.Equal(450, hit.Objects.Single(o => o.ObjectId == Fixture.Player).HullCombat!.CurrentHp);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal const string Player = "SPC-0001", Target = "SPC-0002", Launcher = "MOD-PLAYER-TORPEDO-01";
        private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        internal static string Settings => Path.Combine(Root, "Settings.json");
        internal string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "dss-ui-combat-" + Guid.NewGuid().ToString("N"));
        internal GameSessionHandle Handle { get; }
        internal GameSessionScreen Screen { get; }
        internal long UiNow = Stopwatch.GetTimestamp();
        private long _realMs;
        private readonly int _width, _height;
        private readonly float _scale;
        private readonly SKBitmap _bitmap;
        private readonly SKCanvas _canvas;
        internal AuthoritativeSnapshot Latest => Handle.Buffer.Latest!.Snapshot;
        internal CommandButtonGeometry Button => Assert.Single(Screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.Fire);
        internal string LauncherStatus => Screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher").StatusBarText!;

        private Fixture(int width, int height, float scale)
        {
            (_width, _height, _scale) = (width, height, scale);
            Directory.CreateDirectory(DirectoryPath);
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Settings, out _, out _);
            var source = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios", "PlayerShipOnly", "scenario.json"));
            var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref _realMs)));
            engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 42 } });
            Handle = new GameSessionHandle(new LocalGameSessionConnection(engine, DirectoryPath));
            Screen = new GameSessionScreen(Handle.Buffer, new LinearMotionPredictor(), Handle, timestampProvider: () => UiNow, uiScale: scale);
            _bitmap = new(width, height);
            _canvas = new(_bitmap);
        }
        internal static async Task<Fixture> Create(int width, int height, float scale)
        {
            var fixture = new Fixture(width, height, scale);
            await fixture.Wait(s => s.Objects.Length == 2);
            fixture.Screen.OnActivated();
            fixture.Render();
            if (fixture.Screen.IsPanelVisible)
            {
                var close = fixture.Screen.LastCloseRect;
                fixture.Screen.OnMouseDown(close.MidX * scale, close.MidY * scale);
                fixture.Render();
            }
            return fixture;
        }
        internal void Render() => Screen.Render(_canvas, _width, _height);
        internal void Select(string id)
        {
            var commandsToggle = Screen.CommandsPanel.HideShowButtonRect;
            Screen.OnMouseDown(commandsToggle.MidX * _scale, commandsToggle.MidY * _scale);
            Render();
            bool restoreInfo = Screen.ObjectInfoPanel.State == ObjectInfoPanelState.Open;
            if (restoreInfo)
            {
                var toggle = Screen.ObjectInfoPanel.HideShowButtonRect;
                Screen.OnMouseDown(toggle.MidX * _scale, toggle.MidY * _scale);
                Render();
            }
            Screen.OnKeyDown(Key.End);
            // Frame through ordinary camera input, never select an offscreen coordinate.
            while (Screen.CameraPixelsPerWorldUnit > .1) Screen.OnKeyDown(Key.Minus);
            Render();
            SKPoint Position(string objectId)
            {
                var pose = Screen.RenderStates.Single(o => o.Source.ObjectId == objectId).Pose;
                return new((float)(_width / 2.0 + (pose.X - Screen.CameraFocusX) * Screen.CameraPixelsPerWorldUnit),
                    (float)(_height / 2.0 + (pose.Y - Screen.CameraFocusY) * Screen.CameraPixelsPerWorldUnit));
            }
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var rect = Screen.AvailableMapRect();
                rect.Inflate(-Math.Min(40, rect.Width / 4), -Math.Min(40, rect.Height / 4));
                var target = Position(id);
                if (rect.Contains(target)) break;
                var candidates = new[] { new SKPoint(rect.Left, rect.Top), new SKPoint(rect.Right, rect.Top),
                    new SKPoint(rect.Left, rect.Bottom), new SKPoint(rect.Right, rect.Bottom) }
                    .OrderByDescending(p => Math.Abs(Math.Clamp(p.X + rect.MidX - target.X, rect.Left, rect.Right) - p.X)
                        + Math.Abs(Math.Clamp(p.Y + rect.MidY - target.Y, rect.Top, rect.Bottom) - p.Y))
                    .Where(p => Screen.RenderStates.All(o => SKPoint.Distance(p, Position(o.Source.ObjectId)) > 35)).ToArray();
                // A label plaque is also selectable; alternate corners if it catches a drag.
                var start = candidates[attempt % candidates.Length];
                var end = new SKPoint(Math.Clamp(start.X + rect.MidX - target.X, rect.Left, rect.Right),
                    Math.Clamp(start.Y + rect.MidY - target.Y, rect.Top, rect.Bottom));
                Screen.OnMouseDown(start.X, start.Y);
                Screen.OnMouseMove(end.X, end.Y);
                Screen.OnMouseUp(end.X, end.Y);
                Render();
            }
            var point = Position(id);
            Assert.True(Screen.AvailableMapRect().Contains(point), $"{id} must be visible before clicking.");
            Screen.OnMouseDown(point.X, point.Y);
            Render();
            Assert.Equal(id, Screen.SelectedObjectId);
            if (restoreInfo)
            {
                var toggle = Screen.ObjectInfoPanel.HideShowButtonRect;
                Screen.OnMouseDown(toggle.MidX * _scale, toggle.MidY * _scale);
                Render();
            }
            commandsToggle = Screen.CommandsPanel.HideShowButtonRect;
            Screen.OnMouseDown(commandsToggle.MidX * _scale, commandsToggle.MidY * _scale);
            Render();
        }
        internal void HoverFire()
        {
            Screen.OnMouseMove(Button.Rect.MidX * _scale, Button.Rect.MidY * _scale);
            Render();
        }
        internal async Task<AuthoritativeSnapshot> Fire()
        {
            Assert.True(Button.Enabled);
            var button = Button;
            Screen.OnMouseDown(button.Rect.MidX * _scale, button.Rect.MidY * _scale);
            // A second click before confirmation uses the real UI pending guard.
            Screen.OnMouseDown(button.Rect.MidX * _scale, button.Rect.MidY * _scale);
            var launched = await Wait(s => s.Objects.Any(o => o.Torpedo is not null));
            Render();
            Assert.Single(launched.Objects, o => o.Torpedo is not null);
            return launched;
        }
        internal async Task<AuthoritativeSnapshot> AdvanceTo(long motionMs)
        {
            long remaining = motionMs - Latest.MotionTimeMs;
            Assert.True(remaining >= 0);
            await Handle.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed4);
            Interlocked.Add(ref _realMs, remaining / 100);
            await Handle.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1);
            Interlocked.Add(ref _realMs, remaining % 100);
            await Handle.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
            var result = await Wait(s => s.MotionTimeMs == motionMs && s.CurrentSpeed == SimulationSpeed.Speed0);
            UiNow = Handle.Buffer.Latest!.ReceivedAtTimestamp;
            Render();
            return result;
        }
        internal async Task<AuthoritativeSnapshot> Wait(Func<AuthoritativeSnapshot, bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (Handle.Buffer.Latest is not { } buffered || !predicate(buffered.Snapshot))
            {
                Assert.Null(Handle.Failure);
                // Wait only for transport delivery; physical and effect clocks are explicit.
                await Task.Delay(10, timeout.Token);
            }
            return Latest;
        }
        public async ValueTask DisposeAsync()
        {
            Screen.OnDeactivated();
            _canvas.Dispose();
            _bitmap.Dispose();
            await Handle.DisposeAsync();
            Directory.Delete(DirectoryPath, true);
        }
    }
}
