using System.Collections;
using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeSessionRoundtripTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Old_save_error_keeps_current_session(bool quick)
    {
        await using var f = await Fixture.Create();
        string slot = quick ? SaveSlots.Quicksave : "old";
        string path = Path.Combine(f.DirectoryPath, slot + ".json");
        byte[] original = System.Text.Encoding.UTF8.GetBytes("{\"saveFormatVersion\":15,\"gameState\":{}}");
        await File.WriteAllBytesAsync(path, original);
        string before = ScenarioLoader.Serialize(f.Engine.CaptureSaveState());
        if (!quick) await f.Route(ScreenEvent.OpenLoadWindow);
        await f.Route(quick ? ScreenEvent.QuickLoad : ScreenEvent.LoadSlotRequested, quick ? null : slot);
        Assert.Same(f.Handle, Get<GameSessionHandle>(f.Window, "_session"));
        var error = Assert.IsType<LoadScreen>(f.Screens.Current);
        Assert.Equal(Localization.Get("Combat.LoadIncompatible"), error.ErrorMessage);
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        error.Render(canvas, 1280, 720);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        Assert.Equal(before, ScenarioLoader.Serialize(f.Engine.CaptureSaveState()));
        await f.Route(ScreenEvent.CloseLoadWindow);
        Assert.Same(f.Game, f.Screens.Current);
        await f.Handle.SaveAsync("valid");
        await f.Route(ScreenEvent.OpenLoadWindow);
        await f.Route(ScreenEvent.LoadSlotRequested, "valid");
        Assert.NotSame(f.Handle, Get<GameSessionHandle>(f.Window, "_session"));
        Assert.IsType<GameSessionScreen>(f.Screens.Current);
    }

    [Fact]
    public async Task Malformed_current_save_shows_generic_error_and_can_retry()
    {
        await using var f = await Fixture.Create();
        await File.WriteAllTextAsync(Path.Combine(f.DirectoryPath, "broken.json"),
            JsonSerializer.Serialize(new { saveFormatVersion = SaveFormat.CurrentSaveFormatVersion, gameState = new { } }));
        await f.Route(ScreenEvent.OpenLoadWindow);
        await f.Route(ScreenEvent.LoadSlotRequested, "broken");
        Assert.Equal(Localization.Get("Combat.LoadFailed"), Assert.IsType<LoadScreen>(f.Screens.Current).ErrorMessage);
        Assert.Same(f.Handle, Get<GameSessionHandle>(f.Window, "_session"));
        await f.Handle.SaveAsync("valid");
        await f.Route(ScreenEvent.LoadSlotRequested, "valid");
        Assert.IsType<GameSessionScreen>(f.Screens.Current);
    }

    [Fact]
    public async Task Real_file_roundtrip_restores_both_panels_and_selection()
    {
        await using var f = await Fixture.Create();
        // Establish the projectile at the fixture epoch; this test isolates Save/Load from AI reaction timing.
        Assert.Equal(CommandStartDisposition.Started, f.Engine.TryStartHostileTorpedoFire("SPC-0002", "MOD-PIRATE-TORPEDO-01", 1).Disposition);
        await f.Advance(1);
        string target = f.Engine.CaptureSnapshot().Objects.Single(o => o.Torpedo is not null).ObjectId;
        foreach (string module in Fixture.Defenses)
            await f.Connection.SendCommandAsync(new(module, 1, Fixture.Player, module, DefenseCommandTypes.Fire, target));
        await Wait(f.Handle, s => s.Objects.Count(o => o.Countermeasure is not null) == 2);
        await f.Advance(500);
        var before = await Wait(f.Handle, s => s.Objects.Count(o => o.Countermeasure is not null) == 2 && s.MotionTimeMs == 501);
        string selected = before.Objects.First(o => o.Countermeasure is not null).ObjectId;
        f.Engine.SetObjectInteractionState(Fixture.Player, selected);
        await f.Handle.SaveAsync("both");
        var saved = ScenarioLoader.LoadFromFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Scenario(Path.Combine(f.DirectoryPath, "both.json")), true, isSave: true);
        await f.Route(ScreenEvent.OpenLoadWindow);
        await f.Route(ScreenEvent.LoadSlotRequested, "both");
        var fresh = Get<GameSessionHandle>(f.Window, "_session");
        var after = await Wait(fresh, s => s.Objects.Count(o => o.Countermeasure is not null) == 2);
        var screen = Assert.IsType<GameSessionScreen>(f.Screens.Current);
        Render(screen);
        Assert.Equal(selected, screen.SelectedObjectId);
        Assert.Equal(2, screen.CommandsPanel.CommandPanelRows.Count(r => r.Weapon is { } w && Fixture.Defenses.Contains(w.ModuleId)));
        Assert.Equal(JsonSerializer.Serialize(saved.GameState.DefenseState!.Projectiles.Select(p => p.Flight)),
            JsonSerializer.Serialize(after.Objects.Where(o => o.Countermeasure is not null).OrderBy(o => o.ObjectId).Select(o => o.Countermeasure)));
        Assert.Equal(2, after.InstalledModules.Count(m => m.Defense?.State == DefenseState.Guiding));
        Assert.Empty(screen.CombatEffects.Active); Assert.Empty(screen.CombatEffects.Results);
        Assert.Null(screen.ManualDefensePreviewPlan);
        Assert.Empty(Get<IDictionary>(screen, "_defenseSubmissions"));
    }

    [Fact]
    public async Task Real_file_load_restores_confirmed_ai_info()
    {
        await using var f = await Fixture.Create(ai: true);
        await f.Advance(155);
        var before = await Wait(f.Handle, s => s.MotionTimeMs == 155);
        var ai = before.Objects.Single(o => o.ObjectId == "SPC-0002").HostileCombat;
        Assert.NotNull(ai);
        await f.Handle.SaveAsync("ai");
        await f.Route(ScreenEvent.OpenLoadWindow);
        await f.Route(ScreenEvent.LoadSlotRequested, "ai");
        var fresh = Get<GameSessionHandle>(f.Window, "_session");
        var after = await Wait(fresh, s => s.MotionTimeMs == 155);
        Assert.Equal(ai, after.Objects.Single(o => o.ObjectId == "SPC-0002").HostileCombat);
        var screen = Assert.IsType<GameSessionScreen>(f.Screens.Current);
        Invoke(screen, "SetSelectedObjectId", "SPC-0002");
        Render(screen);
        Assert.Contains((Localization.Get("Combat.Maneuver"), Localization.Get("Combat.Phase." + ai.Phase)), ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo));
    }

    [Fact]
    public async Task Load_has_no_old_effect_or_pending_replay()
    {
        await using var f = await Fixture.Create();
        // Establish the projectile at the fixture epoch; this test isolates Save/Load from AI reaction timing.
        Assert.Equal(CommandStartDisposition.Started, f.Engine.TryStartHostileTorpedoFire("SPC-0002", "MOD-PIRATE-TORPEDO-01", 1).Disposition);
        await f.Advance(1);
        string target = f.Engine.CaptureSnapshot().Objects.Single(o => o.Torpedo is not null).ObjectId;
        await f.Connection.SendCommandAsync(new("manual", 1, Fixture.Player, Fixture.Defenses[0], DefenseCommandTypes.Fire, target));
        await Wait(f.Handle, s => s.Objects.Any(o => o.Countermeasure is not null));
        await f.Advance(3999);
        var before = await Wait(f.Handle, s => s.MotionTimeMs == 4000);
        Assert.Contains(before.CombatJournal, e => e.Type is CombatEventType.Miss or CombatEventType.Intercept);
        Render(f.Game);
        // Submit through the screen and keep its receipt pending in the old screen until Load replaces it.
        Invoke(f.Game, "SendDefenseCommand", new WeaponPanelInstance(Fixture.Player, Fixture.Defenses[1], "Defense"), DefenseCommandTypes.Enable);
        Assert.NotEmpty(Get<IDictionary>(f.Game, "_defenseSubmissions"));
        await f.Handle.SaveAsync("history");
        await f.Route(ScreenEvent.OpenLoadWindow);
        await f.Route(ScreenEvent.LoadSlotRequested, "history");
        var fresh = Get<GameSessionHandle>(f.Window, "_session");
        await Wait(fresh, s => s.MotionTimeMs == 4000);
        var screen = Assert.IsType<GameSessionScreen>(f.Screens.Current);
        Assert.NotSame(f.Game, screen);
        for (int i = 0; i < 3; i++)
        {
            Render(screen);
            Assert.Empty(screen.CombatEffects.Active); Assert.Empty(screen.CombatEffects.Results);
            Assert.Empty(Get<IDictionary>(screen, "_defenseSubmissions"));
            Assert.Null(screen.ManualDefensePreviewPlan);
        }
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
    }
    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static object? Invoke(object target, string name, params object?[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    private static async Task<AuthoritativeSnapshot> Wait(GameSessionHandle handle, Func<AuthoritativeSnapshot, bool>? predicate = null)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            handle.Buffer.PreparePresentation();
            if (handle.Buffer.Presented is { } latest && predicate?.Invoke(latest.Snapshot) != false) return latest.Snapshot;
            Assert.Null(handle.Failure);
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class Fixture : IAsyncDisposable, IGameSessionFactory
    {
        internal const string Player = "SPC-0001";
        internal static readonly string[] Defenses = ["MOD-PLAYER-COUNTERMEASURE-01", "MOD-PLAYER-COUNTERMEASURE-02"];
        private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
        internal string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "dss-phase-three-ui-" + Guid.NewGuid().ToString("N"));
        internal SimulationEngine Engine { get; }
        internal LocalGameSessionConnection Connection { get; }
        internal GameSessionHandle Handle { get; }
        internal GameSessionScreen Game { get; }
        internal SkiaWindow Window { get; }
        internal ScreenStack Screens => Get<ScreenStack>(Window, "_screens");
        private long _now;
        private Fixture(bool ai)
        {
            Directory.CreateDirectory(DirectoryPath);
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(Root, "Settings.json")), out _, out _);
            var scenario = ScenarioLoader.LoadFromFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Scenario(Path.Combine(Root, "Scenarios/PlayerShipOnly/scenario.json")));
            Engine = new(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref _now)));
            var prepared = scenario with
            {
                GameState = scenario.GameState with
                {
                    MasterSeed = 42,
                    SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                    {
                        ShipClassId = o.ObjectId == Player ? null : o.ShipClassId,
                        PositionX = 0,
                        PositionY = o.ObjectId == Player ? 0 : -200,
                        DirectionDegrees = o.ObjectId == Player ? 0 : 180,
                        SpeedMps = 0,
                        MovementType = "Stationary",
                        Modules = o.Modules!.Select(m => m with
                        { AutoDefenseEnabled = false, PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On" }).ToArray()
                    }).ToArray()
                }
            };
            if (ai) prepared = prepared with
            {
                GameState = prepared.GameState with
                {
                    SpaceObjects = prepared.GameState.SpaceObjects.Select(o => o.ObjectId == Player ? o : o with
                    {
                        PositionX = 1000,
                        PositionY = 0,
                        SpeedMps = 1745.3292519943295,
                        IsKnown = true,
                        Modules = o.Modules!.Select(m => m with { PowerState = m.ModuleTypeId == "module.engine.basic" ? "On" : "Off" }).ToArray()
                    }).ToArray()
                }
            };
            Engine.LoadScenario(prepared);
            Connection = new(Engine, DirectoryPath); Handle = new(Connection,
                new SnapshotBuffer(() => System.Diagnostics.Stopwatch.GetTimestamp() * 20));
            Game = new(Handle.Buffer, new LinearMotionPredictor(), Handle);
            Window = new(Game, this, null, createNativeWindow: false);
            Set(Window, "_session", Handle); Set(Window, "_gameSessionScreen", Game);
        }
        internal static async Task<Fixture> Create(bool ai = false) { var f = new Fixture(ai); await Wait(f.Handle); return f; }
        internal async Task Advance(long delta)
        {
            await Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1);
            Interlocked.Add(ref _now, delta * 4);
            await Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
        }
        internal Task Route(ScreenEvent action, string? payload = null) => (Task)Invoke(Window, "HandleScreenEvent", action, payload)!;
        public IGameSessionConnection CreateSessionFromSave(string slot) => LocalGameSessionConnection.CreateFromSaveFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(Root, "Settings.json")), Path.Combine(DirectoryPath, slot + ".json"), DirectoryPath);
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public bool HasQuickSave() => File.Exists(Path.Combine(DirectoryPath, "quicksave.json"));
        public SaveSlotInfo[] ListSaveSlots() => Directory.GetFiles(DirectoryPath, "*.json").Select(p => new SaveSlotInfo(Path.GetFileNameWithoutExtension(p), Path.GetFileNameWithoutExtension(p), File.GetLastWriteTimeUtc(p))).ToArray();
        public void DeleteSaveSlot(string slot) { }
        public async ValueTask DisposeAsync()
        {
            if (Get<GameSessionHandle?>(Window, "_session") is { } current) await current.DisposeAsync();
            Window.Dispose(); await Handle.DisposeAsync(); Directory.Delete(DirectoryPath, true);
        }
    }
}
