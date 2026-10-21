using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Defeat;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0005Tests
{
    private static CombatEncounterStats Stats => new(1, 2, 3, 4, 5, 6, 7, 8, 90, 100);
    private static AuthoritativeSnapshot Scene(CombatEncounterState state, double duration = 2, long motion = 2000, SimulationSpeed speed = SimulationSpeed.Speed1) =>
        new(1, motion, speed, [new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip)], "player",
            CombatJournal: [], Encounter: new(state, 0, state is CombatEncounterState.Won or CombatEncounterState.Lost ? motion : null, Stats, duration));
    private sealed class View : IDisposable
    {
        internal long NowMs;
        internal readonly SnapshotBuffer Buffer;
        internal readonly GameSessionScreen Screen;
        internal readonly List<CombatAlertCue> Audio = [];
        private readonly SKBitmap _bitmap;
        private readonly SKCanvas _canvas;
        private readonly int _width, _height;
        internal View(AuthoritativeSnapshot scene, float scale = 1, bool initialAudio = false, int width = 1920, int height = 1080, CombatVisualSettings? settings = null)
        {
            _width = width; _height = height; _bitmap = new(width, height);
            Buffer = new(() => NowMs * Stopwatch.Frequency / 1000); Buffer.Update(scene);
            Screen = new(Buffer, new LinearMotionPredictor(), timestampProvider: () => NowMs * Stopwatch.Frequency / 1000,
                uiScale: scale, alertAudio: Audio.Add, playInitialCombatStart: initialAudio, combatSettings: settings);
            _canvas = new(_bitmap); Render();
        }
        internal void Render() => Screen.Render(_canvas, _width, _height);
        internal void Capture(string name)
        {
            if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is not { Length: > 0 } directory) return;
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(_bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, name)); png.SaveTo(file);
        }
        public void Dispose() { Screen.OnDeactivated(); _canvas.Dispose(); _bitmap.Dispose(); }
    }

    [Theory]
    [InlineData(1.5f)]
    [InlineData(1.2f)]
    public void Won_summary_remains_visible_with_production_modules_and_short_space_control_panel(float scale)
    {
        string client = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(client, "Settings.json"), out _, out _);
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/CombatMukha/scenario.json")));
        // Explicit UI-only Won fixture over the actual production module projection. Native evidence uses a real completed battle.
        var initial = engine.CaptureSnapshot();
        var report = new BattleReportSnapshot(1, 0, BattleReportKind.PlayerTorpedoLaunch, "ui-layout", new(BattleReportReporterRole.TorpedoOperator));
        var scene = initial with { Encounter = new(CombatEncounterState.Won, 0, 1000, Stats, 1), CombatJournal = [], CombatReports = new(0, 1, [report]) };
        using var v = new View(scene, scale, width: 1280, height: 720);
        foreach (var row in v.Screen.CommandsPanel.CommandPanelRows.Where(r => r.Weapon is null && r.Opened).ToArray())
            v.Screen.CommandsPanel.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        v.Render();
        var space = v.Screen.CommandsPanel.CommandPanelRows.First(r => r.Weapon is null && r.Name.Contains("space", StringComparison.OrdinalIgnoreCase));
        if (!space.Opened) v.Screen.CommandsPanel.OnMouseDown(space.CaptionRect.MidX, space.CaptionRect.MidY);
        v.Render(); v.Render();
        Assert.False(v.Screen.CombatSummary.Bounds.IsEmpty, $"scale={scale},largest={v.Screen.AvailableMapRect(includeEncounterHud: false)}");
        Assert.True(v.Screen.CombatSummary.Bounds.Contains(v.Screen.CombatSummary.ContinueRect));
        Assert.True(new SKRect(0, 0, 1280 / scale, 720 / scale).Contains(v.Screen.CombatSummary.Bounds));
    }

    [Fact]
    public void Combat_banner_timer_summary_and_defeat_stats()
    {
        using var v = new View(Scene(CombatEncounterState.Active), initialAudio: true);
        Assert.True(v.Screen.CombatBorderVisible); Assert.False(v.Screen.CombatBannerRect.IsEmpty);
        Assert.Equal(8, v.Screen.CombatElapsedRealSeconds);
        Assert.Equal(CombatAlertKind.CombatStarted, Assert.Single(v.Audio).Kind);
        v.NowMs = 16; v.Render(); Assert.InRange(v.Screen.CombatElapsedRealSeconds, 8.012, 8.020);
        var paused = Scene(CombatEncounterState.Active, 2.004, 2004, SimulationSpeed.Speed0) with { SnapshotSequence = 2 };
        v.Buffer.Update(paused); v.Render(); double frozen = v.Screen.CombatElapsedRealSeconds;
        v.NowMs += 1000; v.Render(); Assert.Equal(frozen, v.Screen.CombatElapsedRealSeconds);
        Assert.Single(v.Audio);
        v.Capture("combat-active-headless.png");
        v.Buffer.Update(Scene(CombatEncounterState.Won, 2.5, 2500, SimulationSpeed.Speed2) with { SnapshotSequence = 3 }); v.Render();
        Assert.False(v.Screen.CombatBorderVisible); Assert.True(v.Screen.CombatBannerRect.IsEmpty);
        Assert.False(v.Screen.CombatSummary.Bounds.IsEmpty); Assert.Equal(11, v.Screen.CombatSummary.Lines.Count);
        Assert.Contains("00:00:10", v.Screen.CombatSummary.Lines[0]);
        v.NowMs += 1000; v.Render(); Assert.Equal(10, v.Screen.CombatElapsedRealSeconds);
        Assert.Equal(SimulationSpeed.Speed2, v.Buffer.CurrentSpeed); // nonmodal results do not pause or change speed
        v.Capture("combat-won-headless.png");
    }

    [Fact]
    public void Loaded_active_baseline_does_not_replay_sound_but_new_admitted_episode_does()
    {
        using var v = new View(Scene(CombatEncounterState.Active)); Assert.Empty(v.Audio);
        v.Render(); Assert.Empty(v.Audio);
        v.Buffer.Update(Scene(CombatEncounterState.Idle) with { SnapshotSequence = 2 }); v.Render();
        v.Buffer.Update(Scene(CombatEncounterState.Active) with { SnapshotSequence = 3 }); v.Render();
        Assert.Single(v.Audio); v.Render(); Assert.Single(v.Audio);
        v.Buffer.Update(Scene(CombatEncounterState.Active) with { SnapshotSequence = 1 }); v.Render();
        Assert.Single(v.Audio);
    }

    [Fact]
    public void New_combat_introduction_is_UI_feedback_during_pause_and_respects_sound_preference()
    {
        using var paused = new View(Scene(CombatEncounterState.Active, speed: SimulationSpeed.Speed0), initialAudio: true);
        Assert.Equal(CombatAlertKind.CombatStarted, Assert.Single(paused.Audio).Kind);
        paused.NowMs = 1000; paused.Render(); Assert.Single(paused.Audio); Assert.Equal(8, paused.Screen.CombatElapsedRealSeconds);
        var settings = CombatVisualSettings.Default with { ThreatAlerts = CombatVisualSettings.Default.ThreatAlerts with { SoundEnabled = false } };
        using var muted = new View(Scene(CombatEncounterState.Active), initialAudio: true, settings: settings);
        Assert.Empty(muted.Audio); Assert.True(muted.Screen.CombatBorderVisible);
    }

    [Theory]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1920, 1080, 1.5f)]
    [InlineData(1280, 720, 1.5f)]
    public void Won_summary_stays_within_free_map_at_supported_viewports(int width, int height, float scale)
    {
        using var v = new View(Scene(CombatEncounterState.Won, speed: SimulationSpeed.Speed2), scale, width: width, height: height);
        var summary = v.Screen.CombatSummary;
        Assert.False(summary.Bounds.IsEmpty); Assert.True(summary.Bounds.Contains(summary.ContinueRect));
        Assert.True(v.Screen.AvailableMapRect(includeEncounterHud: false).Contains(new SKRect(summary.Bounds.Left * scale, summary.Bounds.Top * scale, summary.Bounds.Right * scale, summary.Bounds.Bottom * scale)));
        Assert.True(v.Screen.IsCombatSpeedEnabled(SimulationSpeed.Speed4));
        v.Screen.OnKeyDown(Key.Space); Assert.Equal(SimulationSpeed.Speed0, v.Buffer.CurrentSpeed);
        Assert.False(summary.Bounds.IsEmpty);
        if (width == 1280) v.Capture("combat-won-compact-headless.png");
    }

    [Fact]
    public void Threat_panel_and_combat_banner_have_stable_non_overlapping_layout()
    {
        var scene = Scene(CombatEncounterState.Active);
        scene = scene with
        {
            Objects = scene.Objects.AddRange(Enumerable.Range(0, 10).Select(i => new ObjectMotionSnapshot("threat-" + i, 100 + i * 10, 0, 6, 270,
                RenderObjectType: SpaceObjectType.Missile, ObservedProjectile: new(ProjectileKind.Torpedo, 100 + i * 10, 0, -1, 0, 6, "slow", 9))))
        };
        using var v = new View(scene);
        var threat = v.Screen.ThreatsPanel.Bounds; var banner = v.Screen.CombatBannerRect;
        Assert.False(threat.IsEmpty); Assert.False(banner.IsEmpty); Assert.False(threat.IntersectsWith(banner));
        for (int i = 0; i < 5; i++)
        {
            v.Render(); Assert.Equal(threat, v.Screen.ThreatsPanel.Bounds); Assert.Equal(banner, v.Screen.CombatBannerRect);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Combat_speed_buttons_show_exact_allowed_set(int index)
    {
        using var v = new View(Scene(CombatEncounterState.Active));
        var speed = new[] { SimulationSpeed.Speed0, SimulationSpeed.Speed1, SimulationSpeed.SpeedDouble, SimulationSpeed.Speed2, SimulationSpeed.SpeedTen }[index];
        Assert.True(v.Screen.IsCombatSpeedEnabled(speed));
        Assert.False(v.Screen.IsCombatSpeedEnabled(SimulationSpeed.Speed3));
        Assert.False(v.Screen.IsCombatSpeedEnabled(SimulationSpeed.Speed4));
        Assert.Equal(5, v.Screen.SpeedOptions.Count);
        var rect = v.Screen.SpeedButtonRects[index]; v.Screen.OnMouseMove(rect.MidX, rect.MidY); v.Render();
        Assert.Null(v.Screen.CombatSpeedTooltip);
        v.Screen.OnMouseDown(rect.MidX, rect.MidY); Assert.Equal(speed, v.Buffer.CurrentSpeed);
    }

    [Fact]
    public void Space_resumes_allowed_x5_after_combat_admission_while_paused()
    {
        using var v = new View(Scene(CombatEncounterState.Idle, speed: SimulationSpeed.Speed2));
        v.Screen.OnKeyDown(Key.Space); Assert.Equal(SimulationSpeed.Speed0, v.Buffer.CurrentSpeed);
        v.Screen.OnKeyUp(Key.Space);
        v.Buffer.Update(Scene(CombatEncounterState.Active, speed: SimulationSpeed.Speed0) with { SnapshotSequence = 2 }); v.Render();
        v.Screen.OnKeyDown(Key.Space); Assert.Equal(SimulationSpeed.Speed2, v.Buffer.CurrentSpeed);
    }

    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(480, 320, 1.5f)]
    public void Defeat_renders_all_captured_stats_in_scrollable_block_without_resume(int width, int height, float scale)
    {
        var encounter = new CombatEncounterSnapshot(CombatEncounterState.Lost, 0, 2500, Stats, 2.5);
        var screen = new DefeatScreen(scale, encounter);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, width, height);
        Assert.Equal(11, screen.StatsLines.Count); Assert.Contains("00:00:10", screen.StatsLines[0]);
        Assert.Contains(screen.StatsLines, line => line.EndsWith(": 100", StringComparison.Ordinal));
        Assert.True(new SKRect(0, 0, width / scale, height / scale).Contains(screen.Layout.Panel));
        Assert.True(screen.Layout.Panel.Contains(screen.Layout.Stats)); Assert.True(screen.Layout.Stats.Bottom < screen.Layout.Load.Top);
        var stats = screen.Layout.Stats;
        Assert.Equal(ScreenEvent.None, screen.OnMouseWheel(stats.MidX * scale, stats.MidY * scale, -1));
        screen.Render(canvas, width, height); Assert.True(screen.StatsScrollOffset > 0);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnMouseDown(screen.Layout.Load.MidX * scale, screen.Layout.Load.MidY * scale, MouseButton.Left));
        Assert.Equal(ScreenEvent.MainMenu, screen.OnMouseDown(screen.Layout.MainMenu.MidX * scale, screen.Layout.MainMenu.MidY * scale, MouseButton.Left));
        if (scale == 1 && Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, "defeat-stats-headless.png")); png.SaveTo(file);
        }
    }

    [Fact]
    public async Task Won_continue_uses_real_acknowledgement_without_modal_pause_or_underlying_launch()
    {
        var registry = PointCombatSessionFixture.Registry();
        var rules = registry.CombatRules! with { ShipDestruction = registry.CombatRules!.ShipDestruction with { DestroyedModuleFraction = .01m } };
        await using var world = new PointCombatSessionFixture(PointCombatSessionFixture.Registry(rules: rules), launchHostile: true);
        long uiMs = 0;
        await using var handle = new GameSessionHandle(world.Connection, new SnapshotBuffer(() => uiMs * Stopwatch.Frequency / 1000));
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, timestampProvider: () => uiMs * Stopwatch.Frequency / 1000);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        async Task Wait(Func<bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            while (true)
            {
                uiMs += 16; screen.Render(canvas, 1920, 1080); Assert.Null(handle.Failure);
                if (predicate()) return;
                await Task.Delay(10, timeout.Token);
            }
        }
        await Wait(() => screen.RenderStates.Any(s => s.IsPlayerShip));
        await handle.SendPointLaunchAsync(PointCombatSessionFixture.Player, PointCombatSessionFixture.Torpedo, ProjectileKind.Torpedo, 400, 0, "fast", out _);
        await Wait(() => handle.Buffer.Presented!.Snapshot.Objects.Any(o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo));
        await handle.SetSpeedAsync(SimulationSpeed.Speed1); world.SetReal(10000); world.Engine.CaptureSnapshot(advanceClock: true);
        await Wait(() => handle.Buffer.Presented!.Snapshot.Encounter?.State == CombatEncounterState.Won && !screen.CombatSummary.Bounds.IsEmpty);
        Assert.False(handle.IsDefeated); Assert.True(screen.CombatSummary.CanContinue);
        screen.OnKeyDown(Key.Q); Assert.NotNull(screen.PointAim); // summary is not modal
        int received = world.Engine.ReceivedCommandCount;
        var button = screen.CombatSummary.ContinueRect; screen.OnMouseDown(button.MidX, button.MidY);
        string acknowledgement = Assert.IsType<string>(screen.LastEncounterAcknowledgementCommandId);
        screen.OnMouseDown(button.MidX, button.MidY); // a second click before the next frame must not resubmit
        Assert.False(screen.CombatSummary.Bounds.IsEmpty); // no optimistic dismissal
        await Wait(() => handle.Buffer.Presented!.Snapshot.Encounter?.State == CombatEncounterState.Idle);
        Assert.True(screen.CombatSummary.Bounds.IsEmpty); Assert.Equal(received + 1, world.Engine.ReceivedCommandCount);
        Assert.Equal(CommandResultStatus.Executed, handle.Buffer.FindCommandResult(acknowledgement)!.Status);
        screen.OnDeactivated();
    }
}
