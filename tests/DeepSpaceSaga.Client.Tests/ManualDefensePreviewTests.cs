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

public class ManualDefensePreviewTests
{
    private const string First = "MOD-PLAYER-COUNTERMEASURE-01";
    private const string Second = "MOD-PLAYER-COUNTERMEASURE-02";

    [Theory]
    [InlineData(1f, First)]
    [InlineData(1.2f, Second)]
    [InlineData(1.5f, First)]
    public async Task Hover_preview_matches_real_launch_geometry_without_rng_or_busy_side_effects(float scale, string module)
    {
        await using var f = new Fixture(scale);
        string before = JsonSerializer.Serialize(f.Engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0));
        var button = f.Hover(module);
        var plan = Assert.IsType<CountermeasurePlan>(f.Screen.ManualDefensePreviewPlan);
        Assert.Equal(module, f.Screen.ManualDefensePreviewWeapon!.ModuleId);
        Assert.NotNull(plan.PredictedEncounterMotionTimeMs);
        Assert.NotNull(f.Screen.ManualDefensePreviewGeometry!.Intercept);
        Assert.Equal(1951, f.Buffer.Latest!.Snapshot.InstalledModules.Single(m => m.ModuleId == module).DefensePreview!.FirstTargetContactMotionTimeMs!.Value, 2);
        f.Render(); f.Render();
        Assert.Same(plan, f.Screen.ManualDefensePreviewPlan);
        Assert.Empty(f.Connection.Commands);
        Assert.Equal(before, JsonSerializer.Serialize(f.Engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0)));
        f.Screen.OnMouseDown(button.Rect.MidX * scale, button.Rect.MidY * scale);
        var actual = Assert.Single(f.Engine.CaptureSnapshotForTests(1, SimulationSpeed.Speed0, 1).Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(module, actual.LauncherModuleId);
        Assert.Equal(JsonSerializer.Serialize(plan.Route), JsonSerializer.Serialize(actual.Route));
        Assert.Equal(plan.PredictedEncounterMotionTimeMs, actual.PredictedEncounterMotionTimeMs);
        f.Render();
        Assert.Null(f.Screen.ManualDefensePreviewPlan); // pending command immediately suppresses stale hover
    }

    [Theory]
    [InlineData("short-life")]
    [InlineData("earlier-target-contact")]
    public async Task Unreachable_preview_has_no_false_cross_and_zero_chance_remains_enabled(string reason)
    {
        await using var f = new Fixture();
        f.Update(s => s with
        {
            InstalledModules = s.InstalledModules.Select(m => m.ModuleId != First ? m : m with
            {
                Defense = m.Defense! with { MaxFlightTimeMs = reason == "short-life" ? 1 : 60000 },
                DefensePreview = m.DefensePreview! with { ChanceTenths = 0, FirstTargetContactMotionTimeMs = reason == "earlier-target-contact" ? 2 : null }
            }).ToImmutableArray()
        });
        Assert.True(f.Hover(First).Enabled);
        Assert.NotNull(f.Screen.ManualDefensePreviewPlan);
        Assert.Null(f.Screen.ManualDefensePreviewGeometry!.Intercept);
        Assert.Empty(f.Screen.ManualDefensePreviewGeometry.Target);
        Assert.Equal(Localization.Get("Combat.UnknownIntercept"), f.Screen.ManualDefensePreviewMessage);
        Assert.InRange(f.Screen.ManualDefensePreviewPlan!.Route.Segments.Sum(s => s.DurationMs), 0, 100);
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Hover_switch_uses_each_modules_constraints_and_clears_on_stale_target_or_overlay()
    {
        await using var f = new Fixture();
        f.Update(s => s with
        {
            InstalledModules = s.InstalledModules.Select(m => m.ModuleId != Second ? m : m with
            { Defense = m.Defense! with { SpeedKmS = 6, TurnRateDegPerSec = 30 } }).ToImmutableArray()
        });
        f.Hover(First);
        Assert.All(f.Screen.ManualDefensePreviewPlan!.Route.Segments, segment => Assert.Equal(20, segment.SpeedKmS));
        var button = f.Hover(Second);
        Assert.Equal(Second, f.Screen.ManualDefensePreviewWeapon!.ModuleId);
        Assert.All(f.Screen.ManualDefensePreviewPlan!.Route.Segments, segment => Assert.Equal(6, segment.SpeedKmS));
        typeof(GameSessionScreen).GetField("_lastPanelRect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(f.Screen, button.Rect);
        typeof(GameSessionScreen).GetField("_panelVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(f.Screen, true);
        f.Render();
        Assert.Null(f.Screen.ManualDefensePreviewPlan);
        typeof(GameSessionScreen).GetField("_panelVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(f.Screen, false);
        f.Update(s => s with
        {
            InstalledModules = s.InstalledModules.Select(m => m.ModuleId != Second ? m : m with
            { DefensePreview = m.DefensePreview! with { TargetTorpedoId = "stale" } }).ToImmutableArray()
        });
        f.Render();
        Assert.Null(f.Screen.ManualDefensePreviewGeometry);
    }

    [Fact]
    public async Task Preview_draws_configured_gray_geometry_on_real_screen()
    {
        await using var f = new Fixture();
        f.Hover(First);
        bool observed = false;
        f.Screen.RenderStageCompleted = stage =>
        {
            if (stage == "combat_trajectories") f.Canvas.Clear(SKColors.Transparent);
            if (stage != "launch_preview") return;
            Assert.True(f.Bitmap.Pixels.Count(p => p.Alpha > 100 && Math.Abs(p.Red - 100) < 3 && Math.Abs(p.Green - 110) < 3 && Math.Abs(p.Blue - 120) < 3) > 30);
            observed = true;
        };
        f.Render();
        Assert.True(observed);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly float _scale;
        internal SimulationEngine Engine { get; }
        internal TorpedoCommandPanelTests.RecordingConnection Connection { get; } = new();
        private readonly GameSessionHandle _handle;
        internal SnapshotBuffer Buffer => _handle.Buffer;
        internal GameSessionScreen Screen { get; }
        internal SKBitmap Bitmap { get; } = new(1920, 1080);
        internal SKCanvas Canvas { get; }
        internal Fixture(float scale = 1)
        {
            _scale = scale;
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
            var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "PlayerShipOnly", "scenario.json"));
            Engine = new(registry);
            Engine.LoadScenario(scenario with
            {
                GameState = scenario.GameState with
                {
                    MasterSeed = 42,
                    SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                    {
                        PositionX = 0,
                        PositionY = o.ObjectId == "SPC-0001" ? 0 : -200,
                        DirectionDegrees = o.ObjectId == "SPC-0001" ? 0 : 180,
                        SpeedMps = 0,
                        MovementType = "Stationary",
                        Modules = o.Modules!.Select(m => m with
                        {
                            AutoDefenseEnabled = false,
                            PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On"
                        }).ToArray()
                    }).ToArray()
                }
            });
            Engine.TryStartHostileTorpedoFire("SPC-0002", "MOD-PIRATE-TORPEDO-01", 1);
            string target = Assert.Single(Engine.CaptureSnapshotForTests(1, SimulationSpeed.Speed0, 1).Objects, o => o.Torpedo is not null).ObjectId;
            Engine.SetObjectInteractionState(null, target);
            _handle = new(Connection);
            Buffer.Update(Engine.CaptureSnapshotForTests(1, SimulationSpeed.Speed0, 1));
            Connection.OnSend = c => { Engine.ReceiveCommand(c); return ValueTask.CompletedTask; };
            Screen = new(Buffer, new LinearMotionPredictor(), _handle, uiScale: scale,
                combatSettings: CombatVisualSettings.Default with { Preview = new SKColor(100, 110, 120) });
            Canvas = new(Bitmap);
        }
        internal void Update(Func<AuthoritativeSnapshot, AuthoritativeSnapshot> change)
        {
            var old = Buffer.Latest!.Snapshot;
            Buffer.Update(change(old) with { SnapshotSequence = old.SnapshotSequence + 1 });
        }
        internal void Render() => Screen.Render(Canvas, 1920, 1080);
        internal CommandButtonGeometry Hover(string module)
        {
            Render();
            var row = Screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == module);
            if (!row.Opened) Screen.OnMouseDown(row.CaptionRect.MidX * _scale, row.CaptionRect.MidY * _scale);
            Render();
            var button = Screen.CommandsPanel.AllCommandButtons.Single(b => b.Weapon?.ModuleId == module && b.CommandTypeId == DefenseCommandTypes.Fire);
            Screen.OnMouseMove(button.Rect.MidX * _scale, button.Rect.MidY * _scale);
            Render();
            return button;
        }
        public async ValueTask DisposeAsync()
        {
            Screen.OnDeactivated(); Canvas.Dispose(); Bitmap.Dispose(); await _handle.DisposeAsync(); Engine.Dispose();
        }
    }
}
