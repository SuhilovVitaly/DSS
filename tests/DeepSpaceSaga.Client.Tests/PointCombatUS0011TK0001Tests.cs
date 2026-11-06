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
            await result.Wait(() => result.Handle.Buffer.Presented?.Snapshot.InstalledModules.Length > 0 && result.Screen.RenderStates.Any(o => o.IsPlayerShip));
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
    public void Shipped_input_is_packaged_and_invalid_bindings_report_path()
    {
        var loaded = CombatInputSettings.Load(SourceInput);
        Assert.Equal(Key.Q, loaded.TorpedoAim); Assert.Equal(Key.E, loaded.CountermissileAim);
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
    [InlineData("schemaVersion", "4")]
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
            { "4" => JsonValue.Create(4), "empty" => new JsonArray(), "reserved" => JsonValue.Create("F10"), _ => JsonValue.Create("invalid") };
            File.WriteAllText(file, json.ToJsonString());
            var error = Assert.Throws<InvalidDataException>(() => CombatInputSettings.Load(file));
            Assert.Contains(file, error.Message); Assert.Contains(change == "reserved" ? "F10" : key, error.Message);
        }
        finally { File.Delete(file); }
    }
}
