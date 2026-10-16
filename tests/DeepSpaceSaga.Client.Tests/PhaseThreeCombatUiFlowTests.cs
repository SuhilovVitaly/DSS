using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeCombatUiFlowTests
{
    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.2f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1920, 1080, 1.2f)]
    [InlineData(1920, 1080, 1.5f)]
    public async Task Two_panels_real_click_flow(int width, int height, float scale)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        string directory = Path.Combine(Path.GetTempPath(), "dss-manual-defense-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        long now = 0;
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "PlayerShipOnly", "scenario.json"));
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = 10000,
                    PositionY = o.ObjectId == "SPC-0001" ? 10000 : 10060,
                    SpeedMps = 0,
                    DirectionDegrees = 0,
                    MovementType = "Stationary",
                    Modules = o.Modules!.Select(m => m with
                    {
                        AutoDefenseEnabled = false,
                        PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On"
                    }).ToArray()
                }).ToArray()
            }
        };
        var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref now)));
        engine.LoadScenario(scenario);
        var connection = new LocalGameSessionConnection(engine, directory);
        var handle = new GameSessionHandle(connection);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, uiScale: scale);
        try
        {
            await Wait(s => s.Objects.Length == 2);
            screen.OnActivated();
            await connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1);
            Interlocked.Exchange(ref now, 5004); // Include the 2–5s Speed1 automatic reaction.
            await connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
            var launched = await Wait(s => s.Objects.Any(o => o.Torpedo is not null));
            string target = launched.Objects.Single(o => o.Torpedo is not null).ObjectId;
            await connection.SetObjectInteractionStateAsync(null, target);
            await Wait(s => s.SelectedObjectId == target && s.InstalledModules.Count(m => m.DefensePreview?.CanFire == true) == 2);
            Render();
            if (screen.IsPanelVisible)
            {
                var close = screen.LastCloseRect;
                screen.OnMouseDown(close.MidX * scale, close.MidY * scale);
            }
            foreach (string module in new[] { "MOD-PLAYER-COUNTERMEASURE-01", "MOD-PLAYER-COUNTERMEASURE-02" })
            {
                var button = Button(module);
                Assert.True(button.Enabled);
                Assert.InRange(button.Rect.Left * scale, 0, width);
                Assert.InRange(button.Rect.Right * scale, 0, width);
                Assert.InRange(button.Rect.Top * scale, 0, height);
                Assert.InRange(button.Rect.Bottom * scale, 0, height);
                screen.OnMouseMove(button.Rect.MidX * scale, button.Rect.MidY * scale);
                Render();
                Assert.NotNull(screen.ManualDefensePreviewPlan);
                var body = screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == module).BodyRect;
                foreach (var footer in new[] { screen.MapToolbarRect, screen.LastScalePanelRect, screen.LastSpeedPanelRect, screen.LastMechanicsPanelRect })
                    Assert.False(body.IntersectsWith(footer), $"Defense details intersect footer at {width}x{height}/{scale}: {body} / {footer}");
                Click(button.Rect); Click(button.Rect);
            }
            var after = await Wait(s => s.Objects.Count(o => o.Countermeasure is not null) == 2);
            Render(); Render();
            Assert.Empty(screen.DefenseFlightLabels);
            Assert.All(screen.DefenseChanceLabels.Values, label => Assert.NotNull(label.Encounter));
            Assert.Equal(2, screen.DefenseGeometry.Count);
            Assert.Equal(launched.MotionTimeMs, after.MotionTimeMs);
            Assert.Equal(SimulationSpeed.Speed0, after.CurrentSpeed);
            Assert.All(after.Objects.Where(o => o.Countermeasure is not null), o =>
            {
                Assert.Equal(target, o.Countermeasure!.TargetTorpedoId);
                Assert.Equal(LaunchMode.Manual, o.Countermeasure.Mode);
                Assert.Equal(launched.MotionTimeMs, o.Countermeasure.LaunchMotionTimeMs);
            });
            Assert.Equal(2, after.InstalledModules.Count(m => m.Defense?.State == DefenseState.Guiding));
            Assert.Empty(after.CombatJournal.Where(e => e.Roll is not null));
            Assert.Equal(target, screen.SelectedObjectId);
        }
        finally
        {
            screen.OnDeactivated();
            await handle.DisposeAsync();
            Directory.Delete(directory, true);
        }

        void Render()
        {
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, width, height);
        }
        void Click(SKRect rect) => screen.OnMouseDown(rect.MidX * scale, rect.MidY * scale);
        CommandButtonGeometry Button(string module)
        {
            Render();
            var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == module);
            if (!row.Opened) { Click(row.CaptionRect); Render(); }
            return screen.CommandsPanel.AllCommandButtons.Single(b => b.Weapon?.ModuleId == module && b.CommandTypeId == DefenseCommandTypes.Fire);
        }

        async Task<AuthoritativeSnapshot> Wait(Func<AuthoritativeSnapshot, bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (handle.Buffer.Latest is not { } buffered || !predicate(buffered.Snapshot))
            {
                Assert.Null(handle.Failure);
                await Task.Delay(10, timeout.Token);
            }
            return handle.Buffer.Latest!.Snapshot;
        }
    }
}
