using System.Diagnostics;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0011TK0001Tests
{
    private static string SourceInput => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client", CombatInputSettings.RelativePath));

    private sealed class Session : IAsyncDisposable
    {
        internal readonly PointCombatSessionFixture World = new();
        internal readonly GameSessionHandle Handle;
        internal readonly GameSessionScreen Screen;
        private readonly KeyboardEdgeTracker _keys;
        private readonly HashSet<Key> _down = new();
        private readonly Key[] _pressed = new Key[64], _released = new Key[64];
        private readonly SKBitmap _bitmap = new(1920, 1200);
        private readonly SKCanvas _canvas;
        private Session(CombatInputSettings? bindings)
        {
            Handle = new(World.Connection);
            Screen = new(Handle.Buffer, new DeepSpaceSaga.Motion.LinearMotionPredictor(), Handle, combatInputs: bindings);
            _keys = new(bindings); _canvas = new(_bitmap);
        }
        internal static async Task<Session> Create(CombatInputSettings? bindings = null)
        {
            var result = new Session(bindings);
            await result.Wait(() => result.Handle.Buffer.Presented?.Snapshot.InstalledModules.Length > 0);
            return result;
        }
        internal void Render() => Screen.Render(_canvas, 1920, 1200);
        private void Poll()
        {
            var (p, r) = _keys.PollBoth(_down.Contains, _pressed, _released);
            Screen.SynchronizeModifiers(_down.Contains(Key.ControlLeft), _down.Contains(Key.ControlRight));
            for (int i = 0; i < p; i++) Screen.OnKeyDown(_pressed[i]);
            for (int i = 0; i < r; i++) Screen.OnKeyUp(_released[i]);
        }
        internal void Tap(params Key[] keys)
        {
            _down.UnionWith(keys); Poll(); Render();
            _down.Clear(); Poll();
        }
        internal void Click(double worldX, double worldY, MouseButton button = MouseButton.Left)
        {
            float x = (float)(960 + (worldX - Screen.CameraFocusX) * Screen.CameraPixelsPerWorldUnit);
            float y = (float)(600 + (worldY - Screen.CameraFocusY) * Screen.CameraPixelsPerWorldUnit);
            Screen.OnMouseMove(x, y); Screen.OnMouseDown(x, y, button);
        }
        internal async Task Wait(Func<bool> predicate)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            while (true)
            {
                Render(); Assert.Null(Handle.Failure);
                if (predicate()) return;
                await Task.Delay(10, deadline.Token);
            }
        }
        public async ValueTask DisposeAsync()
        {
            Screen.OnDeactivated(); _canvas.Dispose(); _bitmap.Dispose();
            await Handle.DisposeAsync(); await World.DisposeAsync();
        }
    }

    [Fact]
    public async Task Aiming_keys_select_launcher_profile_and_cancel()
    {
        await using var s = await Session.Create();
        var speed = s.World.Engine.CurrentSpeed;
        s.Tap(Key.Q, Key.Number3); // simultaneous entry/profile cannot route Number3 to simulation speed
        Assert.Equal(ProjectileKind.Torpedo, s.Screen.PointAim!.Kind);
        Assert.Equal("fast", s.Screen.PointAim.ProfileId); Assert.Equal(speed, s.World.Engine.CurrentSpeed);
        double zoom = s.Screen.CameraPixelsPerWorldUnit;
        s.Screen.OnMouseWheel(1000, 400, -1); Assert.Equal("medium", s.Screen.PointAim.ProfileId);
        Assert.Equal(zoom, s.Screen.CameraPixelsPerWorldUnit);
        s.Tap(Key.Number1); Assert.Equal("slow", s.Screen.PointAim.ProfileId);
        s.Tap(Key.Escape); Assert.Null(s.Screen.PointAim);
        s.Tap(Key.W); Assert.Equal(ProjectileKind.Countermissile, s.Screen.PointAim!.Kind);
        Assert.Equal("MOD-PLAYER-COUNTERMEASURE-01", s.Screen.PointAim.ModuleId);
        s.Tap(Key.Number2); Assert.Equal("fast", s.Screen.PointAim.ProfileId);
        s.Tap(Key.Number3); Assert.Equal("fast", s.Screen.PointAim.ProfileId);
        s.Click(100, 0, MouseButton.Right); Assert.Null(s.Screen.PointAim);
        Assert.Equal(0, s.World.Engine.ReceivedCommandCount);
    }

    [Fact]
    public async Task LMB_submits_real_point_coordinates_profile_and_no_optimistic_projectile()
    {
        await using var s = await Session.Create(); s.Tap(Key.Q, Key.Number3);
        s.Click(300, 0);
        string id = Assert.IsType<string>(s.Screen.LastPointCommandId);
        Assert.DoesNotContain(s.Handle.Buffer.Presented!.Snapshot.Objects, o => o.OwnProjectile is not null);
        Assert.NotNull(s.Handle.Buffer.FindCommandAcceptance(id));
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed } &&
            s.Handle.Buffer.Presented!.Snapshot.Objects.Any(o => o.OwnProjectile is not null));
        var own = Assert.Single(s.Handle.Buffer.Presented!.Snapshot.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal(300, own.DetonationX); Assert.Equal(0, own.DetonationY); Assert.Equal("fast", own.ProfileId);
        Assert.Equal(PointCombatSessionFixture.Torpedo, own.LauncherModuleId);
        Assert.Equal(1, s.World.Engine.ReceivedCommandCount);
    }

    [Fact]
    public async Task Reload_aim_does_not_queue_launch()
    {
        await using var s = await Session.Create(); s.Tap(Key.Q, Key.Number3); s.Click(300, 0);
        await s.Wait(() => s.Handle.Buffer.Presented!.Snapshot.InstalledModules.Single(m => m.ModuleId == PointCombatSessionFixture.Torpedo)
            .LauncherCombat!.ReadyAtMotionTimeMs > 0);
        int received = s.World.Engine.ReceivedCommandCount;
        s.Tap(Key.Q); Assert.NotNull(s.Screen.PointAim);
        s.Click(300, 0); Assert.Equal(received, s.World.Engine.ReceivedCommandCount);
        Assert.Equal(GameSessionScreen.CombatText("Reloading", "Reloading"), s.Screen.PointAimFeedback);
        await s.Handle.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1);
        s.World.SetReal(40000);
        await s.Handle.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
        await s.Wait(() => s.Handle.Buffer.Presented!.Snapshot.MotionTimeMs == 10000);
        Assert.Equal(GameSessionScreen.CombatText("Ready", "Ready"), s.Screen.PointAimFeedback);
        Assert.Equal(received, s.World.Engine.ReceivedCommandCount);
        Assert.Empty(s.Handle.Buffer.Presented!.Snapshot.Objects.Where(o => o.OwnProjectile is not null));
        Assert.Equal(1, s.Handle.Buffer.Presented.Snapshot.Encounter!.Stats.TorpedoesLaunched);
    }

    [Fact]
    public async Task W_prefers_ready_counter_and_panel_button_addresses_exact_launcher()
    {
        await using var s = await Session.Create();
        await s.Handle.SendPointLaunchAsync(PointCombatSessionFixture.Player, "MOD-PLAYER-COUNTERMEASURE-01", ProjectileKind.Countermissile,
            200, 0, "wide", out var id);
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed } &&
            s.Handle.Buffer.Presented!.Snapshot.InstalledModules.Single(m => m.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01").Defense!.ReadyAtMotionTimeMs > 0);
        s.Tap(Key.W); Assert.Equal("MOD-PLAYER-COUNTERMEASURE-02", s.Screen.PointAim!.ModuleId);
        foreach (string name in new[] { "Navigation", "Maneuver", "Engine", "Space Control", "Torpedo Launcher" })
        {
            var row = s.Screen.CommandsPanel.CommandPanelRows.FirstOrDefault(r => r.Name == name && r.Opened);
            if (row.Name is null) continue;
            s.Screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY); s.Render();
        }
        var first = s.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01");
        if (!first.Opened) { s.Screen.OnMouseDown(first.CaptionRect.MidX, first.CaptionRect.MidY); s.Render(); }
        var button = Assert.Single(s.Screen.CommandsPanel.AllCommandButtons,
            b => b.Weapon?.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01" && b.CommandTypeId == DefenseCommandTypes.Launch);
        Assert.True(button.Enabled); s.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        Assert.Equal("MOD-PLAYER-COUNTERMEASURE-01", s.Screen.PointAim!.ModuleId); // aiming is allowed during reload
        s.Screen.OnDeactivated(); Assert.Null(s.Screen.PointAim);
    }

    [Fact]
    public async Task JSON_remap_reaches_keyboard_and_screen_and_load_reset_cancels_aim()
    {
        string file = Path.Combine(Path.GetTempPath(), "dss-combat-input-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(SourceInput))!;
            json["torpedoAim"] = "T"; json["countermissileAim"] = "R";
            File.WriteAllText(file, json.ToJsonString());
            var bindings = CombatInputSettings.Load(file);
            await using var s = await Session.Create(bindings);
            s.Tap(Key.Q); Assert.Null(s.Screen.PointAim);
            s.Tap(Key.T); Assert.Equal(ProjectileKind.Torpedo, s.Screen.PointAim!.Kind);
            s.Tap(Key.R); Assert.Equal(ProjectileKind.Countermissile, s.Screen.PointAim!.Kind);
            var state = s.Handle.Buffer.Presented!.Snapshot;
            s.Handle.Buffer.UpdatePresentation(new(Guid.NewGuid(), 1, state.MotionTimeMs, [], state, false, 0));
            s.Render(); Assert.Null(s.Screen.PointAim);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void Shipped_input_is_packaged_and_invalid_bindings_report_path()
    {
        var loaded = CombatInputSettings.Load(SourceInput);
        Assert.Equal(Key.Q, loaded.TorpedoAim); Assert.Equal(Key.W, loaded.CountermissileAim);
        Assert.Equal(new[] { Key.Number1, Key.Number2, Key.Number3 }, loaded.Profiles.ToArray());
        Assert.Equal(Key.Escape, loaded.Cancel); Assert.Equal(MouseButton.Left, loaded.LaunchMouse);
        Assert.Equal(MouseButton.Right, loaded.CancelMouse); Assert.True(loaded.WheelProfiles);
        Assert.Equal(File.ReadAllText(SourceInput), File.ReadAllText(Path.Combine(AppContext.BaseDirectory, CombatInputSettings.RelativePath)));
        string file = Path.GetTempFileName();
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(SourceInput))!;
            json["countermissileAim"] = "Q"; File.WriteAllText(file, json.ToJsonString());
            var error = Assert.Throws<InvalidDataException>(() => CombatInputSettings.Load(file));
            Assert.Contains(file, error.Message); Assert.Contains("Duplicate", error.Message);
            json["countermissileAim"] = "not-a-key"; File.WriteAllText(file, json.ToJsonString());
            error = Assert.Throws<InvalidDataException>(() => CombatInputSettings.Load(file));
            Assert.Contains("countermissileAim", error.Message);
        }
        finally { File.Delete(file); }
    }

    [Theory]
    [InlineData("schemaVersion", "2")]
    [InlineData("profiles", "empty")]
    [InlineData("cancel", "missing")]
    [InlineData("wheelProfiles", "string")]
    [InlineData("unexpected", "extra")]
    [InlineData("torpedoAim", "reserved")]
    public void Input_schema_errors_identify_file_and_field(string key, string change)
    {
        string file = Path.GetTempFileName();
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(SourceInput))!.AsObject();
            if (change == "missing") json.Remove(key);
            else json[key] = change switch
            { "2" => JsonValue.Create(2), "empty" => new JsonArray(), "reserved" => JsonValue.Create("F10"), _ => JsonValue.Create("invalid") };
            File.WriteAllText(file, json.ToJsonString());
            var error = Assert.Throws<InvalidDataException>(() => CombatInputSettings.Load(file));
            Assert.Contains(file, error.Message); Assert.Contains(key, error.Message);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task Control_modified_aim_key_does_not_start_aiming()
    {
        await using var s = await Session.Create();
        s.Tap(Key.ControlLeft, Key.Q); Assert.Null(s.Screen.PointAim);
        s.Tap(Key.Q); Assert.NotNull(s.Screen.PointAim);
        Assert.Equal(0, s.World.Engine.ReceivedCommandCount);
    }
}
