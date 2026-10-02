using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TorpedoPreviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    public async Task Paused_preview_matches_authoritative_launch_after_visual_reconciliation(int predictionLeadMs)
    {
        await using var f = new Fixture(moving: true, deterministicClock: true);
        f.Buffer.Update(f.Engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1, 1000));
        f.Render();
        Assert.Equal(-1, f.Screen.RenderStates.Single(s => s.IsPlayerShip).Pose.Y);
        f.Now = System.Diagnostics.Stopwatch.Frequency * (1000 + predictionLeadMs) / 1000;
        var paused = f.Engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed0, 2000);
        f.Buffer.Update(paused);
        f.Render();
        var target = f.Screen.RenderStates.Single(s => s.Source.ObjectId == "SPC-0002").Pose;
        f.Screen.OnMouseDown((float)(960 + target.X - f.Screen.CameraFocusX),
            (float)(540 + target.Y - f.Screen.CameraFocusY));
        f.Render();
        f.HoverFire();
        var preview = Assert.IsType<TorpedoRoute>(f.Screen.LaunchPreviewRoute);
        var button = f.FireButton;
        f.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        var launched = Assert.Single(f.Engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed0, 2000).Objects.Where(o => o.Torpedo is not null));
        Assert.Equal((launched.X, launched.Y), (preview.Segments[0].X, preview.Segments[0].Y));
        Assert.Equal(JsonSerializer.Serialize(launched.Torpedo!.Route), JsonSerializer.Serialize(preview));
        var ownerPose = f.Screen.RenderStates.Single(s => s.IsPlayerShip).Pose;
        Assert.Equal((ownerPose.X, ownerPose.Y), (preview.Segments[0].X, preview.Segments[0].Y));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public async Task Hover_preview_matches_launch_from_same_state(float scale)
    {
        await using var f = new Fixture(scale);
        f.SelectTarget();
        f.HoverFire();
        var preview = Assert.IsType<TorpedoRoute>(f.Screen.LaunchPreviewRoute);
        var owner = f.Buffer.Latest!.Snapshot.Objects.Single(o => o.ObjectId == "SPC-0001");
        Assert.Equal((owner.X, owner.Y, owner.Direction), (preview.Segments[0].X, preview.Segments[0].Y, preview.Segments[0].Direction));
        Assert.All(preview.Segments, s => Assert.Equal(3, s.SpeedKmS));
        var button = f.FireButton;
        f.Screen.OnMouseDown(button.Rect.MidX * scale, button.Rect.MidY * scale);
        Assert.Single(f.Connection.Commands);
        var launched = f.Engine.CaptureSnapshotForTests();
        var flight = Assert.Single(launched.Objects.Where(o => o.Torpedo is not null)).Torpedo!;
        Assert.Equal(JsonSerializer.Serialize(preview), JsonSerializer.Serialize(flight.Route));
        Assert.Equal("SPC-0002", flight.TargetObjectId);
        f.Buffer.Update(launched);
        f.Render();
        Assert.Null(f.Screen.LaunchPreviewGeometry);
    }

    [Fact]
    public async Task Preview_uses_gray_config_color()
    {
        var color = new SKColor(100, 110, 120);
        await using var f = new Fixture(palette: CombatVisualSettings.Default with { Preview = color });
        f.SelectTarget();
        var button = f.FireButton;
        f.Screen.OnMouseMove(button.Rect.MidX, button.Rect.MidY);
        bool observed = false;
        f.Screen.RenderStageCompleted = stage =>
        {
            if (stage == "combat_trajectories") f.Canvas.Clear(SKColors.Transparent);
            if (stage != "launch_preview") return;
            int matching = f.Bitmap.Pixels.Count(p => p.Alpha > 100 &&
                Math.Abs(p.Red - color.Red) < 3 && Math.Abs(p.Green - color.Green) < 3 && Math.Abs(p.Blue - color.Blue) < 3);
            Assert.True(matching > 50, $"Only {matching} preview-colored pixels.");
            observed = true;
        };
        f.Render();
        Assert.True(observed);
        Assert.NotNull(f.Screen.LaunchPreviewGeometry!.Intercept);
    }

    [Fact]
    public async Task Hover_has_no_authoritative_side_effects()
    {
        await using var f = new Fixture();
        f.SelectTarget();
        string before = JsonSerializer.Serialize(f.Engine.CaptureSaveState());
        f.HoverFire();
        var route = f.Screen.LaunchPreviewRoute;
        for (int i = 0; i < 5; i++) f.Render();
        Assert.Same(route, f.Screen.LaunchPreviewRoute); // paused hover reuses its plan
        f.Screen.OnMouseMove(800, 800);
        f.Render();
        Assert.Null(f.Screen.LaunchPreviewRoute);
        Assert.Empty(f.Connection.Commands);
        Assert.Equal(before, JsonSerializer.Serialize(f.Engine.CaptureSaveState()));
        Assert.DoesNotContain(f.Engine.CaptureSnapshot().Objects, o => o.Torpedo is not null);
        Assert.All(f.Engine.CaptureSnapshot().InstalledModules, m => Assert.Null(m.LauncherCombat?.ActiveTorpedoObjectId));
    }

    [Theory]
    [InlineData("no-target")]
    [InlineData("self")]
    [InlineData("busy")]
    [InlineData("off")]
    [InlineData("missing")]
    [InlineData("hidden")]
    public async Task Busy_or_no_target_has_no_launch_preview(string state)
    {
        await using var f = new Fixture();
        if (state != "no-target") f.SelectTarget();
        else f.Render();
        if (state == "self") f.Screen.OnMouseDown(960, 540);
        var button = f.FireButton;
        var snapshot = f.Buffer.Latest!.Snapshot;
        if (state is "busy" or "off" or "missing")
            f.Buffer.Update(snapshot with
            {
                SnapshotSequence = snapshot.SnapshotSequence + 1,
                InstalledModules = state == "missing" ? [] : snapshot.InstalledModules.Select(m => m.LauncherCombat is null ? m : m with
                {
                    PowerState = state == "off" ? "Off" : m.PowerState,
                    LauncherCombat = m.LauncherCombat with { ActiveTorpedoObjectId = state == "busy" ? "existing" : null }
                }).ToImmutableArray()
            });
        if (state == "hidden")
        {
            var caption = f.Screen.CommandsPanel.HideShowButtonRect;
            f.Screen.OnMouseDown(caption.MidX, caption.MidY);
        }
        // Use the previously drawn button to exercise stale enablement/geometry too.
        f.Screen.OnMouseMove(button.Rect.MidX, button.Rect.MidY);
        f.Render();
        Assert.Null(f.Screen.LaunchPreviewRoute);
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Preview_refreshes_when_selected_target_changes()
    {
        await using var f = new Fixture();
        var snapshot = f.Buffer.Latest!.Snapshot;
        f.Buffer.Update(snapshot with
        {
            SnapshotSequence = snapshot.SnapshotSequence + 1,
            Objects = snapshot.Objects.Add(new("other", 0, 100, 0, 0, RenderObjectType: SpaceObjectType.Wreck))
        });
        f.SelectTarget();
        f.HoverFire();
        string original = JsonSerializer.Serialize(f.Screen.LaunchPreviewRoute);
        f.Screen.OnMouseDown(960, 640);
        f.Render();
        Assert.Equal("other", f.Screen.SelectedObjectId);
        f.HoverFire();
        Assert.NotEqual(original, JsonSerializer.Serialize(f.Screen.LaunchPreviewRoute));
        Assert.NotNull(f.Screen.LaunchPreviewGeometry!.Intercept);
        Assert.Equal(0, f.Screen.LaunchPreviewGeometry.Intercept.Value.X, 8);
        Assert.Equal(100, f.Screen.LaunchPreviewGeometry.Intercept.Value.Y, 8);
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Unreachable_preview_has_no_encounter_cross()
    {
        await using var f = new Fixture();
        var snapshot = f.Buffer.Latest!.Snapshot;
        f.Buffer.Update(snapshot with
        {
            SnapshotSequence = snapshot.SnapshotSequence + 1,
            Objects = snapshot.Objects.Select(o => o.ObjectId == "SPC-0002" ? o with { X = 0, Y = -200, SpeedKmS = 4 } : o).ToImmutableArray()
        });
        f.Render();
        f.Screen.OnMouseDown(960, 340);
        f.Render();
        f.HoverFire();
        Assert.False(f.Screen.LaunchPreviewRoute!.HasIntercept);
        Assert.Null(f.Screen.LaunchPreviewGeometry!.Intercept);
        Assert.NotEmpty(f.Screen.LaunchPreviewGeometry.Prediction);
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Preview_updates_current_pose_and_clears_on_modal_or_mouse_leave()
    {
        await using var f = new Fixture();
        f.SelectTarget();
        f.HoverFire();
        string before = JsonSerializer.Serialize(f.Screen.LaunchPreviewRoute);
        var snapshot = f.Buffer.Latest!.Snapshot;
        f.Buffer.Update(snapshot with
        {
            SnapshotSequence = snapshot.SnapshotSequence + 1,
            CurrentSpeed = SimulationSpeed.Speed1,
            Objects = snapshot.Objects.Select(o => o.ObjectId == "SPC-0001" ? o with { X = 10, Direction = 90 } : o).ToImmutableArray()
        });
        f.Render();
        f.Now += System.Diagnostics.Stopwatch.Frequency / 2;
        f.Render();
        var owner = f.Screen.PlayerShipInfo;
        Assert.NotNull(owner);
        Assert.NotEqual(before, JsonSerializer.Serialize(f.Screen.LaunchPreviewRoute));
        Assert.Empty(f.Connection.Commands);
        f.Screen.OnDeactivated();
        Assert.Null(f.Screen.LaunchPreviewRoute);
        f.Screen.OnActivated();
        f.Render();
        Assert.Null(f.Screen.LaunchPreviewRoute);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly float _scale;
        internal long Now;
        internal SimulationEngine Engine { get; }
        internal Connection Connection { get; }
        internal GameSessionHandle Handle { get; }
        internal SnapshotBuffer Buffer { get; }
        internal GameSessionScreen Screen { get; }
        internal SKBitmap Bitmap { get; } = new(1920, 1080);
        internal SKCanvas Canvas { get; }
        internal CommandButtonGeometry FireButton => Assert.Single(Screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.Fire);

        internal Fixture(float scale = 1, CombatVisualSettings? palette = null, bool moving = false, bool deterministicClock = false)
        {
            _scale = scale;
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
            var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "PlayerShipOnly", "scenario.json"));
            Engine = new SimulationEngine(registry);
            Engine.LoadScenario(scenario with
            {
                GameState = scenario.GameState with
                {
                    MasterSeed = 42,
                    SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                    {
                        PositionX = o.ObjectId == "SPC-0001" ? 0 : moving ? 200 : -200,
                        PositionY = o.ObjectId == "SPC-0001" ? 0 : moving ? -100 : 100,
                        DirectionDegrees = 0,
                        SpeedMps = moving ? 100 : 0,
                        MovementType = moving ? "Linear" : "Stationary"
                    }).ToArray()
                }
            });
            Connection = new Connection(Engine);
            Handle = new GameSessionHandle(Connection);
            Buffer = deterministicClock ? new SnapshotBuffer(() => Now) : Handle.Buffer;
            Buffer.Update(Engine.CaptureSnapshotForTests());
            Screen = new GameSessionScreen(Buffer, new LinearMotionPredictor(), Handle, uiScale: scale, combatSettings: palette, timestampProvider: () => Now);
            Canvas = new SKCanvas(Bitmap);
        }

        internal void Render() => Screen.Render(Canvas, 1920, 1080);
        internal void SelectTarget()
        {
            Render();
            Screen.OnMouseDown(760, 640);
            Render();
            Assert.Equal("SPC-0002", Screen.SelectedObjectId);
        }
        internal void HoverFire()
        {
            var button = FireButton;
            Screen.OnMouseMove(button.Rect.MidX * _scale, button.Rect.MidY * _scale);
            Render();
        }
        public async ValueTask DisposeAsync()
        {
            Screen.OnDeactivated();
            Canvas.Dispose();
            Bitmap.Dispose();
            await Handle.DisposeAsync();
            Engine.Dispose();
        }
    }

    private sealed class Connection(SimulationEngine engine) : IGameSessionConnection
    {
        internal List<PlayerCommand> Commands { get; } = [];
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            engine.ReceiveCommand(command);
            return ValueTask.CompletedTask;
        }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // The fixture explicitly delivers snapshots; keep the transport open.
            await Task.Delay(Timeout.Infinite, cancellationToken);
            yield break;
        }
    }
}
