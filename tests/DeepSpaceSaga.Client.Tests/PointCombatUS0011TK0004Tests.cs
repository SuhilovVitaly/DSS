using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0011TK0004Tests
{
    private sealed class Session : IAsyncDisposable
    {
        internal readonly PointCombatSessionFixture World = new();
        internal readonly GameSessionHandle Handle;
        internal readonly GameSessionScreen Screen;
        internal long UiMs;
        private readonly SKBitmap _bitmap = new(1920, 1200);
        private readonly SKCanvas _canvas;
        private Session()
        {
            Handle = new(World.Connection, new SnapshotBuffer(() => UiMs * Stopwatch.Frequency / 1000));
            Screen = new(Handle.Buffer, new LinearMotionPredictor(), Handle, timestampProvider: () => UiMs * Stopwatch.Frequency / 1000);
            _canvas = new(_bitmap);
        }
        internal static async Task<Session> Create()
        { var s = new Session(); await s.Wait(() => s.Handle.Buffer.Presented?.Snapshot.InstalledModules.Length > 0 && s.Screen.RenderStates.Any(o => o.IsPlayerShip)); return s; }
        internal void Render() => Screen.Render(_canvas, 1920, 1200);
        internal void Click(double x = 300, double y = 0)
        {
            float sx = (float)(960 + (x - Screen.CameraFocusX) * Screen.CameraPixelsPerWorldUnit);
            float sy = (float)(600 + (y - Screen.CameraFocusY) * Screen.CameraPixelsPerWorldUnit);
            Screen.OnMouseMove(sx, sy); Screen.OnMouseDown(sx, sy);
        }
        internal async Task Wait(Func<bool> predicate, bool render = true)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            while (true) { if (render) Render(); Assert.Null(Handle.Failure); if (predicate()) return; await Task.Delay(10, timeout.Token); }
        }
        public async ValueTask DisposeAsync()
        { Screen.OnDeactivated(); _canvas.Dispose(); _bitmap.Dispose(); await Handle.DisposeAsync(); await World.DisposeAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Launch_sends_explicit_point_profile_and_selected_module(bool counter)
    {
        await using var s = await Session.Create();
        s.Click(400); // unrelated object selection before aim must not become a launch target
        s.Screen.OnKeyDown(counter ? Key.W : Key.Q); s.Screen.OnKeyDown(Key.Number2);
        string module = s.Screen.PointAim!.ModuleId;
        string profile = s.Screen.PointAim.ProfileId;
        s.Click(250, 30);
        Assert.True(s.Screen.LastPointCommandId is not null, $"Aim={s.Screen.PointAim}; feedback={s.Screen.PointAimFeedback}; camera={s.Screen.CameraFocusX},{s.Screen.CameraFocusY}/{s.Screen.CameraPixelsPerWorldUnit}; free={s.Screen.AvailableMapRect()}; modules={string.Join(';', s.Handle.Buffer.Presented!.Snapshot.InstalledModules.Where(m => m.LauncherCombat is not null || m.Defense is not null).Select(m => m.ModuleId + ':' + m.PowerState + '/' + m.OperationalState))}");
        string id = s.Screen.LastPointCommandId!;
        s.Screen.OnKeyDown(counter ? Key.Q : Key.W); // change active UI context after submission
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is not null);
        var result = s.Handle.Buffer.FindCommandResult(id)!;
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        s.UiMs = 200; // admit the initial 150 ms scene reserve; receipt alone does not advance it
        await s.Wait(() => s.Handle.Buffer.Presented!.Snapshot.Objects.Any(o => o.OwnProjectile is not null));
        Assert.Equal(module, result.ModuleId); Assert.Equal(counter ? DefenseCommandTypes.Launch : CombatCommandTypes.Launch, result.CommandType);
        var own = Assert.Single(s.Handle.Buffer.Presented!.Snapshot.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal(module, own.LauncherModuleId); Assert.Equal(profile, own.ProfileId);
        Assert.Equal(250, own.DetonationX, 6); Assert.Equal(30, own.DetonationY, 6);
        Assert.Equal(PointCombatSessionFixture.Player, own.OwnerObjectId);
    }

    [Fact]
    public async Task Click_feedback_under_100ms_and_projectile_awaits_scene()
    {
        await using var s = await Session.Create(); s.Screen.OnKeyDown(Key.Q); s.Screen.OnKeyDown(Key.Number3);
        await s.Handle.SetSpeedAsync(SimulationSpeed.Speed1);
        s.UiMs = 150; // initial reserve elapsed; later packets are received at this UI time
        s.World.SetReal(1000); Assert.Equal(250, s.World.Engine.CaptureSnapshot(advanceClock: true).MotionTimeMs);
        var watch = Stopwatch.StartNew(); s.Click(); string id = Assert.IsType<string>(s.Screen.LastPointCommandId);
        string? immediate = s.Screen.PointAimFeedback; watch.Stop();
        Assert.True(watch.Elapsed.TotalMilliseconds <= 100, $"Click/feedback {watch.Elapsed.TotalMilliseconds:0.000}ms exceeds 100ms");
        Assert.Equal(GameSessionScreen.CombatText("Accepted", "Command accepted"), immediate);
        Assert.DoesNotContain(s.Handle.Buffer.Presented!.Snapshot.Objects, o => o.OwnProjectile is not null);
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed }, render: false);
        s.Render(); // admit newer earlier revisions while the t=250 launch stays delayed
        Assert.True(s.Handle.Buffer.Presented!.Snapshot.MotionTimeMs < 250,
            $"Presented={s.Handle.Buffer.Presented.Snapshot.MotionTimeMs}/{s.Handle.Buffer.Presented.Snapshot.CurrentSpeed}; latest={s.Handle.Buffer.Latest?.Snapshot.MotionTimeMs}/{s.Handle.Buffer.Latest?.Snapshot.CurrentSpeed}; tracker={s.Handle.Buffer.CurrentSpeed}; engine={s.World.Engine.CurrentSpeed}");
        Assert.DoesNotContain(s.Handle.Buffer.Presented.Snapshot.Objects, o => o.OwnProjectile is not null);
        Assert.Equal(GameSessionScreen.CombatText("Accepted", "Command accepted"), s.Screen.PointAimFeedback);
        int received = s.World.Engine.ReceivedCommandCount; s.Click(); Assert.Equal(received, s.World.Engine.ReceivedCommandCount);
        await s.Handle.SetSpeedAsync(SimulationSpeed.Speed0);
        await Task.Delay(150); s.UiMs = 350;
        await s.Wait(() => s.Handle.Buffer.Presented!.Snapshot.Objects.Any(o => o.OwnProjectile is not null));
        Assert.Equal(GameSessionScreen.CombatText("Reloading", "Reloading"), s.Screen.PointAimFeedback);
    }

    [Fact]
    public async Task Rejected_launch_shows_localized_reason_and_no_queued_retry()
    {
        await using var s = await Session.Create(); s.Screen.OnKeyDown(Key.Q); s.Screen.OnKeyDown(Key.Number3);
        // Actual external command completes while the screen has not admitted its reload state.
        await s.Handle.SendPointLaunchAsync(PointCombatSessionFixture.Player, PointCombatSessionFixture.Torpedo,
            ProjectileKind.Torpedo, 300, 0, "fast", out var first);
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(first) is { Status: CommandResultStatus.Executed }, render: false);
        s.Click(); string rejected = Assert.IsType<string>(s.Screen.LastPointCommandId);
        Assert.NotEqual(first, rejected);
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(rejected) is { Status: CommandResultStatus.Rejected }, render: false);
        s.Render();
        Assert.Equal(CommandReasonCodes.LauncherReloading, s.Handle.Buffer.FindCommandResult(rejected)!.ReasonCode);
        Assert.Equal(GameSessionScreen.PointLaunchReason(CommandReasonCodes.LauncherReloading), s.Screen.PointAimFeedback);
        int received = s.World.Engine.ReceivedCommandCount;
        await s.Handle.SetSpeedAsync(SimulationSpeed.Speed1); s.World.SetReal(40000);
        await s.Handle.SetSpeedAsync(SimulationSpeed.Speed0); await Task.Delay(150); s.UiMs = 200;
        await s.Wait(() => s.Handle.Buffer.Presented!.Snapshot.MotionTimeMs == 10000);
        Assert.Equal(received, s.World.Engine.ReceivedCommandCount);
        Assert.Equal(1, s.Handle.Buffer.Presented!.Snapshot.Encounter!.Stats.TorpedoesLaunched);
        Assert.Equal(GameSessionScreen.CombatText("Ready", "Ready"), s.Screen.PointAimFeedback);
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Every_point_launch_reason_is_localized_in_source_and_packaged_locale(string language)
    {
        string source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client", "Data", "Locale", language + ".json"));
        var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(source))!;
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Locale", language + ".json")));
        string[] reasons = [CommandReasonCodes.UnknownObject, CommandReasonCodes.UnknownModule, CommandReasonCodes.UnknownCommandType,
            CommandReasonCodes.ModuleUnavailable, CommandReasonCodes.ModuleDisabled, CommandReasonCodes.ModuleDestroyed, CommandReasonCodes.Busy,
            CommandReasonCodes.LauncherReloading, CommandReasonCodes.MissingProfile, CommandReasonCodes.UnknownProfile, CommandReasonCodes.InvalidTargetCoordinates,
            CommandReasonCodes.NoWeaponOperator, CommandReasonCodes.LaunchOutOfRange, CommandReasonCodes.InvalidTime, CommandReasonCodes.ProjectileIdExhausted,
            CommandReasonCodes.SessionDefeated, "connection_unavailable", "send_failed", "command_rejected", "friendly_fire_confirmation"];
        foreach (string reason in reasons)
        { Assert.True(strings.TryGetValue("Combat.Reason." + reason, out var text), reason); Assert.False(string.IsNullOrWhiteSpace(text)); Assert.NotEqual(reason, text); }
        Assert.True(strings.ContainsKey("Combat.CommandRejected"));
    }

    [Fact]
    public void Unknown_server_reason_uses_localized_generic_message()
    {
        Assert.Equal(GameSessionScreen.CombatText("CommandRejected", "Command rejected"), GameSessionScreen.PointLaunchReason("future_server_reason"));
        Assert.DoesNotContain("future_server_reason", GameSessionScreen.PointLaunchReason("future_server_reason"));
    }
}
