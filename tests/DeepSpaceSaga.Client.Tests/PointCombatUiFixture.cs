using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>Current source catalog and local transport; only initial public poses and NPC power are controlled.</summary>
internal sealed class PointCombatUiFixture : IAsyncDisposable, IGameSessionFactory
{
    internal const string Player = "SPC-0001", Enemy = "SPC-0002", Torpedo = "MOD-PLAYER-TORPEDO-01";
    internal static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
    internal readonly SimulationEngine Engine;
    internal readonly LocalGameSessionConnection Connection;
    internal readonly GameSessionHandle Handle;
    internal readonly GameSessionScreen Screen;
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly int _width, _height;
    internal readonly float Scale;
    internal long UiMs;
    private long _realMs;
    internal AuthoritativeSnapshot Latest => Handle.Buffer.Latest!.Snapshot;
    internal AuthoritativeSnapshot Presented => Handle.Buffer.Presented!.Snapshot;

    private PointCombatUiFixture(int width, int height, float scale, CombatInputSettings? inputs, int hostileShots, int? accuracySkill)
    {
        (_width, _height, Scale) = (width, height, scale);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
        var source = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios/PlayerShipOnly/scenario.json"));
        if (hostileShots is < 0 or > 16) throw new ArgumentOutOfRangeException(nameof(hostileShots));
        var template = source.GameState.SpaceObjects.Single(o => o.ObjectId == Enemy);
        var extraEnemies = Enumerable.Range(0, hostileShots).Select(i =>
        {
            string suffix = "-budget-" + i;
            return template with
            {
                ObjectId = Enemy + suffix,
                Ai = null,
                PositionX = 300 + i * 10,
                PositionY = 0,
                SpeedMps = 0,
                MovementType = "Stationary",
                DirectionDegrees = 270,
                Modules = template.Modules!.Select(m => m with
                {
                    ModuleId = m.ModuleId + suffix,
                    OperatorCrewId = m.OperatorCrewId is null ? null : m.OperatorCrewId + suffix,
                    PowerState = m.ModuleTypeId == "module.torpedo.launcher.basic" ? "On" : "Off"
                }).ToArray(),
                Crew = template.Crew!.Select(c => c with { CrewId = c.CrewId + suffix }).ToArray()
            };
        }).ToArray();
        Engine = new(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref _realMs)));
        Engine.LoadScenario(source with
        {
            GameState = source.GameState with
            {
                MasterSeed = 42,
                CurrentSpeed = "Speed0",
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = o.ObjectId == Player ? 0 : 300,
                    PositionY = 0,
                    SpeedMps = 0,
                    DirectionDegrees = o.ObjectId == Player ? 90 : 270,
                    MovementType = "Stationary",
                    Ai = null,
                    Crew = o.ObjectId == Player && accuracySkill is { } skill
                        ? o.Crew!.Select(c => c with { AccuracySkill = skill }).ToArray() : o.Crew,
                    Modules = o.Modules!.Select(m => m with { PowerState = o.ObjectId == Player ? "On" : "Off" }).ToArray()
                }).Concat(extraEnemies).ToArray()
            }
        });
        for (int i = 0; i < hostileShots; i++)
        {
            string suffix = "-budget-" + i;
            Assert.Equal(CommandStartDisposition.Started, Engine.TryStartHostilePointLaunch(Enemy + suffix,
                "MOD-PIRATE-TORPEDO-01" + suffix, -100, 0, "slow", 0).Disposition);
        }
        Engine.SetPresentationTimestampForTests(() => 0);
        Connection = new(Engine);
        Handle = new(Connection, new SnapshotBuffer(() => UiMs * Stopwatch.Frequency / 1000));
        Screen = new(Handle.Buffer, new LinearMotionPredictor(), Handle,
            uiScale: scale, timestampProvider: () => UiMs * Stopwatch.Frequency / 1000, combatInputs: inputs);
        _bitmap = new(width, height); _canvas = new(_bitmap);
    }
    internal static async Task<PointCombatUiFixture> Create(int width = 1920, int height = 1080, float scale = 1,
        CombatInputSettings? inputs = null, int hostileShots = 0, int? accuracySkill = null)
    {
        var f = new PointCombatUiFixture(width, height, scale, inputs, hostileShots, accuracySkill);
        await f.Wait(() => f.Handle.Buffer.Presented?.Snapshot.InstalledModules.Length > 0 && f.Screen.RenderStates.Any(o => o.IsPlayerShip));
        f.Screen.OnActivated(); f.Render();
        if (f.Screen.IsPanelVisible)
        {
            var close = f.Screen.LastCloseRect;
            f.Screen.OnMouseDown(close.MidX * scale, close.MidY * scale); f.Render();
        }
        return f;
    }
    internal void Render() => Screen.Render(_canvas, _width, _height);
    internal string DebugMap(double x, double y = 0)
    {
        var pixel = Pixel(x, y);
        return $"point={pixel}; free={Screen.AvailableMapRect()}; aim={Screen.PointAim}; feedback={Screen.PointAimFeedback}; info={Screen.IsPanelVisible}; camera={Screen.CameraFocusX}/{Screen.CameraFocusY}/{Screen.CameraPixelsPerWorldUnit}";
    }
    internal (float X, float Y) Pixel(double x, double y = 0) =>
        ((float)(_width / 2d + (x - Screen.CameraFocusX) * Screen.CameraPixelsPerWorldUnit),
         (float)(_height / 2d + (y - Screen.CameraFocusY) * Screen.CameraPixelsPerWorldUnit));
    internal void Click(double x, double y = 0, MouseButton button = MouseButton.Left)
    { var p = Pixel(x, y); Screen.OnMouseMove(p.X, p.Y); Screen.OnMouseDown(p.X, p.Y, button); }
    internal void Expose(double x, double y = 0)
    {
        Render(); var point = Pixel(x, y); var free = Screen.AvailableMapRect();
        var interior = free; interior.Inflate(-10, -10);
        if (interior.Contains(point.X, point.Y)) return;
        Screen.OnKeyDown(Key.Escape);
        float startX = free.Right - 10, startY = free.Bottom - 10;
        Screen.OnMouseDown(startX, startY);
        Screen.OnMouseMove(startX + free.MidX - point.X, startY + free.MidY - point.Y);
        Screen.OnMouseUp(startX + free.MidX - point.X, startY + free.MidY - point.Y);
        Render(); point = Pixel(x, y);
        Assert.True(Screen.AvailableMapRect().Contains(point.X, point.Y), DebugMap(x, y));
    }
    internal void Aim(bool counter = false, int profile = 2)
    { Screen.OnKeyDown(counter ? Key.E : Key.Q); Screen.OnKeyDown(profile switch { 1 => Key.Number1, 2 => Key.Number2, _ => Key.Number3 }); Render(); }
    internal async Task<CommandResult> Launch(bool counter = false, double x = 300, double y = 0, int profile = 2)
    {
        Expose(x, y); Aim(counter, profile); Click(x, y);
        Assert.True(Screen.LastPointCommandId is not null, DebugMap(x, y));
        string id = Screen.LastPointCommandId!;
        await Wait(() => Handle.Buffer.FindCommandResult(id) is not null);
        return Handle.Buffer.FindCommandResult(id)!;
    }
    internal async Task<AuthoritativeSnapshot> AdvanceTo(long motion)
    {
        await Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1);
        Interlocked.Exchange(ref _realMs, motion * 4);
        await Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
        await Wait(() => Presented.MotionTimeMs >= motion && Presented.CurrentSpeed == SimulationSpeed.Speed0);
        return Presented;
    }
    internal async Task Wait(Func<bool> predicate, bool advanceUi = true)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        while (true)
        {
            if (advanceUi) UiMs += 20;
            Render(); Assert.Null(Handle.Failure); if (predicate()) return;
            await Task.Delay(10, timeout.Token);
        }
    }
    public IGameSessionConnection CreateSession() => throw new NotSupportedException();
    public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
    public IGameSessionConnection CreateSessionFromScenario(string scenarioPath) => throw new NotSupportedException();
    public ScenarioInfo[] ListScenarios() => [];
    public bool HasQuickSave() => false;
    public SaveSlotInfo[] ListSaveSlots() => [];
    public void DeleteSaveSlot(string slotId) => throw new NotSupportedException();
    public async ValueTask DisposeAsync()
    { Screen.OnDeactivated(); _canvas.Dispose(); _bitmap.Dispose(); await Handle.DisposeAsync(); await Connection.DisposeAsync(); }
}
