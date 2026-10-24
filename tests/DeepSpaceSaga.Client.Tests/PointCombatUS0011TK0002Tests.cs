using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0011TK0002Tests
{
    private sealed class Session : IAsyncDisposable
    {
        internal readonly PointCombatSessionFixture World;
        internal readonly GameSessionHandle Handle;
        internal readonly GameSessionScreen Screen;
        internal long RealMs;
        private readonly SKBitmap _bitmap = new(1920, 1200);
        private readonly SKCanvas _canvas;
        private Session()
        {
            World = new(); Handle = new(World.Connection);
            Screen = new(Handle.Buffer, new LinearMotionPredictor(), Handle, timestampProvider: () => RealMs * Stopwatch.Frequency / 1000,
                mapSettings: new TacticalMapSettings { ZoomAnimationMs = 0, WheelFactor = 2 });
            _canvas = new(_bitmap);
        }
        internal static async Task<Session> Create()
        {
            var s = new Session(); await s.Wait(() => s.Handle.Buffer.Presented?.Snapshot.InstalledModules.Length > 0 && s.Screen.RenderStates.Any(o => o.IsPlayerShip));
            for (int i = 0; i < 1; i++) s.Screen.OnKeyDown(Key.Minus);
            s.Render(); return s;
        }
        internal void Render() => Screen.Render(_canvas, 1920, 1200);
        internal void Capture()
        {
            if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is not { Length: > 0 } directory) return;
            Directory.CreateDirectory(directory);
            using var image = SKImage.FromBitmap(_bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var output = File.Create(Path.Combine(directory, "torpedo-aim-preview-headless.png")); data.SaveTo(output);
        }
        internal void Move(double x, double y = 0) => Screen.OnMouseMove(
            (float)(960 + (x - Screen.CameraFocusX) * Screen.CameraPixelsPerWorldUnit),
            (float)(600 + (y - Screen.CameraFocusY) * Screen.CameraPixelsPerWorldUnit));
        internal void Click(double x, double y = 0)
        {
            Move(x, y);
            Screen.OnMouseDown((float)(960 + (x - Screen.CameraFocusX) * Screen.CameraPixelsPerWorldUnit),
                (float)(600 + (y - Screen.CameraFocusY) * Screen.CameraPixelsPerWorldUnit));
        }
        internal async Task Wait(Func<bool> predicate)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            while (true) { Render(); Assert.Null(Handle.Failure); if (predicate()) return; await Task.Delay(10, deadline.Token); }
        }
        public async ValueTask DisposeAsync()
        { Screen.OnDeactivated(); _canvas.Dispose(); _bitmap.Dispose(); await Handle.DisposeAsync(); await World.DisposeAsync(); }
    }

    [Fact]
    public async Task Torpedo_preview_is_linear_at_eta_and_has_no_chance()
    {
        await using var s = await Session.Create(); s.Screen.OnKeyDown(Key.Q); s.Screen.OnKeyDown(Key.Number1); s.Move(600); s.Render();
        Assert.True(s.Screen.CurrentTorpedoAimPreview is not null, $"Aim={s.Screen.PointAim}, ppu={s.Screen.CameraPixelsPerWorldUnit}, focus={s.Screen.CameraFocusX},{s.Screen.CameraFocusY}, free={s.Screen.AvailableMapRect()}");
        var preview = s.Screen.CurrentTorpedoAimPreview!;
        Assert.Equal(60, preview.X / 10, 5); Assert.Equal(9, preview.RadiusKm); Assert.Equal(.3, preview.CoreFraction);
        Assert.Equal(10, preview.PhysicalSeconds, 5); Assert.Equal(40, preview.RealSeconds, 5);
        Assert.Equal(0, preview.SigmaKm); Assert.False(preview.OutOfRange);
        Assert.Contains(preview.Shadows, p => p.ObjectId == PointCombatSessionFixture.Enemy);
        Assert.True(s.Screen.AvailableMapRect().Contains(s.Screen.TorpedoAimLabelBounds));
        s.Capture();
        string json = JsonSerializer.Serialize(preview);
        Assert.DoesNotContain("Chance", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HostileCombat", json);
        s.Screen.OnKeyDown(Key.Number3); s.Render(); preview = s.Screen.CurrentTorpedoAimPreview!;
        Assert.Equal(2.5, preview.RadiusKm); Assert.Equal(15, preview.RealSeconds, 5);
        s.Move(701); s.Render(); Assert.True(s.Screen.CurrentTorpedoAimPreview!.OutOfRange);
        int before = s.World.Engine.ReceivedCommandCount; s.Click(701);
        Assert.Equal(before, s.World.Engine.ReceivedCommandCount);
        Assert.Equal(GameSessionScreen.CombatText("Reason." + CommandReasonCodes.LaunchOutOfRange, CommandReasonCodes.LaunchOutOfRange), s.Screen.PointAimFeedback);
    }

    [Theory]
    [InlineData(0, 100, 60, 0)]
    [InlineData(90, 160, 60, 60)]
    [InlineData(180, 100, 60, 120)]
    public void Linear_shadow_ignores_hidden_controls_and_marks_physical_coverage(double direction, double x, double targetY, double expectedY)
    {
        var enemy = new ObjectMotionSnapshot("enemy", 100, 60, 2, direction,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel, TurnStepDegrees: 90, TurnStepRemainingMs: 1,
            TurnHoldRateDegPerSec: 24, RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy);
        var shadow = GameSessionScreen.LinearAimShadow(enemy, x, targetY, 3, 9, .3, false);
        Assert.Equal(direction == 90 ? 160 : 100, shadow.X, 7); Assert.Equal(expectedY, shadow.Y, 7);
        double distance = double.Hypot(shadow.X - x, shadow.Y - targetY) / 10;
        double expected = distance <= 2.7 ? 1 : distance >= 9 ? 0 : (9 - distance) / 6.3;
        Assert.Equal(expected, shadow.Coverage, 10);
        Assert.Equal(shadow, GameSessionScreen.LinearAimShadow(enemy with { TurnHoldRateDegPerSec = null, ActiveEngineCommandType = null }, x, targetY, 3, 9, .3, false));
    }

    [Fact]
    public async Task Preview_reads_actual_nondefault_rules_and_operator_without_rng_draw()
    {
        var basis = PointCombatSessionFixture.Registry();
        var rules = basis.CombatRules! with { Blast = new(.45), Scatter = new(10, .02, .4) };
        await using var world = new PointCombatSessionFixture(PointCombatSessionFixture.Registry(.04, rules));
        var snapshot = world.Engine.CaptureSnapshot();
        var launcher = snapshot.InstalledModules.Single(m => m.ModuleId == PointCombatSessionFixture.Torpedo).LauncherCombat!;
        int skill = launcher.Operator!.Skill;
        double expected = .04 * Math.Max(.4, 1 - (skill - 10) * .02);
        Assert.Equal(expected, launcher.EffectiveScatterPercent!.Value, 12); Assert.Equal(.45, launcher.CoreFraction);
        var before = world.Engine.CaptureLaunchScatterState();
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0,
            mapSettings: new TacticalMapSettings { ZoomAnimationMs = 0, WheelFactor = 2 });
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1200);
        for (int i = 0; i < 1; i++) screen.OnKeyDown(Key.Minus);
        screen.OnKeyDown(Key.Q); screen.OnKeyDown(Key.Number1);
        float x = (float)(960 + (500 - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
        float y = (float)(600 - screen.CameraFocusY * screen.CameraPixelsPerWorldUnit);
        screen.OnMouseMove(x, y); screen.Render(canvas, 1920, 1200);
        Assert.Equal(50 * expected, screen.CurrentTorpedoAimPreview!.SigmaKm!.Value, 6);
        Assert.Equal(.45, screen.CurrentTorpedoAimPreview.CoreFraction);
        Assert.Equal(before, world.Engine.CaptureLaunchScatterState());
        Assert.All(snapshot.Objects.Where(o => o.ObjectId != PointCombatSessionFixture.Player), o => Assert.True(o.ModuleHealth.IsDefaultOrEmpty));
        screen.OnDeactivated();
    }

    [Fact]
    public async Task Friendly_fire_requires_second_click_within_one_second()
    {
        await using var s = await Session.Create(); s.Screen.OnKeyDown(Key.Q); s.Screen.OnKeyDown(Key.Number1); s.Move(10); s.Render();
        Assert.True(s.Screen.CurrentTorpedoAimPreview!.FriendlyFire); s.Click(10);
        Assert.Equal(0, s.World.Engine.ReceivedCommandCount); Assert.Null(s.Screen.LastPointCommandId);
        s.RealMs = 1000; s.Click(10);
        string id = Assert.IsType<string>(s.Screen.LastPointCommandId);
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed });
        Assert.Equal(1, s.World.Engine.ReceivedCommandCount);
    }

    [Theory]
    [InlineData("point")]
    [InlineData("profile")]
    [InlineData("cancel")]
    [InlineData("launcher")]
    [InlineData("timeout")]
    public async Task Changed_context_invalidates_friendly_confirmation(string change)
    {
        await using var s = await Session.Create(); s.Screen.OnKeyDown(Key.Q); s.Screen.OnKeyDown(Key.Number1); s.Click(10);
        switch (change)
        {
            case "point": s.Move(11); s.Move(10); break;
            case "profile": s.Screen.OnKeyDown(Key.Number3); s.Screen.OnKeyDown(Key.Number1); break;
            case "cancel": s.Screen.OnKeyDown(Key.Escape); s.Screen.OnKeyDown(Key.Q); s.Screen.OnKeyDown(Key.Number1); break;
            case "launcher": s.Screen.OnKeyDown(Key.E); s.Screen.OnKeyDown(Key.Q); s.Screen.OnKeyDown(Key.Number1); break;
            case "timeout": s.RealMs = 1001; break;
        }
        s.Click(10); Assert.Equal(0, s.World.Engine.ReceivedCommandCount);
        s.RealMs += 1; s.Click(10); Assert.NotNull(s.Screen.LastPointCommandId);
    }

    [Fact]
    public async Task Stationary_cursor_can_confirm_friendly_fire_while_camera_follows_moving_ship()
    {
        await using var f = await PointCombatUiFixture.Create();
        await f.Handle.SendCommandAsync(PointCombatUiFixture.Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.Accelerate);
        await f.AdvanceTo(1000);
        f.Expose(10); f.Aim();
        var pixel = f.Pixel(f.Screen.CameraFocusX + 10, f.Screen.CameraFocusY);
        f.Screen.OnMouseMove(pixel.X, pixel.Y); f.Render();
        Assert.True(f.Screen.CurrentTorpedoAimPreview!.FriendlyFire);
        f.Screen.OnMouseDown(pixel.X, pixel.Y);
        Assert.Null(f.Screen.LastPointCommandId);
        double firstX = f.Screen.CurrentTorpedoAimPreview.X;
        await f.AdvanceTo(1100);
        Assert.NotEqual(firstX, f.Screen.CurrentTorpedoAimPreview!.X);
        f.Screen.OnMouseDown(pixel.X, pixel.Y);
        Assert.NotNull(f.Screen.LastPointCommandId);
        await f.Wait(() => f.Handle.Buffer.FindCommandResult(f.Screen.LastPointCommandId!) is { Status: CommandResultStatus.Executed });
    }

    [Fact]
    public async Task Every_visible_enemy_has_its_own_linear_shadow()
    {
        await using var world = new PointCombatSessionFixture();
        var scene = world.Engine.CaptureSnapshot();
        var enemy = scene.Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Enemy);
        scene = scene with { Objects = scene.Objects.Add(enemy with { ObjectId = "second", X = 500, Y = 10, SpeedKmS = 1, Direction = 90 }) };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(scene);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0,
            mapSettings: new TacticalMapSettings { ZoomAnimationMs = 0, WheelFactor = 2 });
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1200);
        for (int i = 0; i < 1; i++) screen.OnKeyDown(Key.Minus);
        screen.OnKeyDown(Key.Q); screen.OnKeyDown(Key.Number1);
        screen.OnMouseMove((float)(960 + (600 - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit),
            (float)(600 - screen.CameraFocusY * screen.CameraPixelsPerWorldUnit)); screen.Render(canvas, 1920, 1200);
        var shadows = screen.CurrentTorpedoAimPreview!.Shadows.Where(s => !s.Friendly).ToArray(); Assert.Equal(2, shadows.Length);
        var second = Assert.Single(shadows, s => s.ObjectId == "second");
        Assert.Equal(600, second.X, 6); Assert.Equal(10, second.Y, 6); Assert.Equal(1, second.Coverage);
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(1920, 1200, 1f)]
    [InlineData(1280, 720, 1.5f)]
    public async Task Preview_label_stays_in_available_map_at_supported_scale(int width, int height, float scale)
    {
        await using var world = new PointCombatSessionFixture();
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(world.Engine.CaptureSnapshot());
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, uiScale: scale);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, width, height);
        screen.OnKeyDown(Key.Q); screen.OnKeyDown(Key.Number1); var free = screen.AvailableMapRect();
        screen.OnMouseMove(free.MidX, free.MidY); screen.Render(canvas, width, height);
        Assert.NotNull(screen.CurrentTorpedoAimPreview);
        Assert.True(free.Contains(screen.TorpedoAimLabelBounds), $"{free}: {screen.TorpedoAimLabelBounds}");
        screen.OnDeactivated(); Assert.Null(screen.CurrentTorpedoAimPreview);
    }
}
