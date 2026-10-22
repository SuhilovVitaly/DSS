using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;
using System.Diagnostics;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0002Tests
{
    [Theory]
    [InlineData(0, 40, 0)]
    [InlineData(2500, 30, .25)]
    [InlineData(9975, .1, .9975)]
    [InlineData(10000, 0, 1)]
    public void Weapon_reload_ring_uses_physical_deadline_with_tenth_second_precision(double motionMs, double seconds, double fraction)
    {
        var module = new InstalledModuleSnapshot("launcher", "type", "Launcher", 0, [],
            LauncherCombat: new(6, 0, 200, ReadyAtMotionTimeMs: 10000, ReloadDurationMs: 10000, Profiles: [new("slow", 6, 9)]));
        var panel = new CommandsPanel(motionTime: () => motionMs, motionMultiplier: () => .25);
        using var bitmap = new SKBitmap(500, 1800); using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, [module], 1800, "player");
        var row = Assert.Single(panel.CommandPanelRows, r => r.Weapon?.ModuleId == "launcher");
        Assert.Equal(seconds, row.RemainingReloadRealSeconds, 6); Assert.Equal(fraction, row.ReloadFraction, 6);
        Assert.False(row.ReloadRingRect.IsEmpty); Assert.True(row.ReloadBarRect.IsEmpty);
        Assert.True(row.CaptionRect.Contains(row.ReloadRingRect));
    }

    [Fact]
    public async Task Weapon_panels_show_profile_reload_and_operator_without_rolls()
    {
        await using var world = new PointCombatSessionFixture(PointCombatSessionFixture.Registry(.02));
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(world.Engine.CaptureSnapshot());
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1800); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1800);
        var rows = screen.CommandsPanel.CommandPanelRows.Where(r => r.Weapon is not null).ToArray();
        Assert.Equal(buffer.Presented!.Snapshot.InstalledModules.Count(m => m.LauncherCombat is not null || m.Defense is not null), rows.Length);
        Assert.Contains(rows, r => r.Weapon!.ModuleId == PointCombatSessionFixture.Torpedo);
        foreach (var row in rows)
        {
            Assert.False(row.ReloadRingRect.IsEmpty); Assert.NotNull(row.OperatorText);
            Assert.Contains(GameSessionScreen.CombatText("Accuracy", "Точность"), row.OperatorText);
            Assert.Contains(row.DetailLines, line => line.Contains("σ"));
            Assert.DoesNotContain(row.DetailLines, line => line.Contains("шанс", StringComparison.OrdinalIgnoreCase) || line.Contains("chance", StringComparison.OrdinalIgnoreCase) || line.Contains("roll", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(row.Buttons, b => b.CommandTypeId is CombatCommandTypes.Fire or CombatCommandTypes.SelfDestruct or DefenseCommandTypes.Fire or DefenseCommandTypes.Enable or DefenseCommandTypes.Disable);
        }
        screen.OnKeyDown(Key.Q); screen.OnKeyDown(Key.Number2); screen.Render(canvas, 1920, 1800);
        var torpedo = Assert.Single(screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatSessionFixture.Torpedo);
        Assert.Contains(torpedo.DetailLines, line => line.StartsWith("> medium", StringComparison.Ordinal));
        Assert.Contains(torpedo.Buttons, b => b.CommandTypeId == CombatCommandTypes.Launch && b.Label!.Contains("[Q]"));
        Assert.Contains(torpedo.Buttons, b => b.CommandTypeId == CombatCommandTypes.Detonate && b.Label == GameSessionScreen.CombatText("Detonate", "Подрыв"));
        var toggle = torpedo.RangeToggleRect; Assert.True(torpedo.RangeVisible);
        screen.CommandsPanel.OnMouseDown(toggle.MidX, toggle.MidY); screen.Render(canvas, 1920, 1800);
        Assert.False(screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == PointCombatSessionFixture.Torpedo).RangeVisible);
        if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, "weapon-panels-headless.png")); png.SaveTo(file);
        }
        screen.OnDeactivated();
    }

    [Fact]
    public async Task Counter_effective_scatter_is_authoritative_and_foreign_projection_stays_hidden()
    {
        var source = PointCombatSessionFixture.Registry();
        var rules = source.CombatRules! with { Scatter = new(30, .01, .3) };
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m =>
                m.CategoryTypeId == "module.countermeasure.launcher" ? m with { ScatterPercent = .08 } : m),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: rules);
        await using var world = new PointCombatSessionFixture(registry);
        var snapshot = world.Engine.CaptureSnapshot();
        var counters = snapshot.InstalledModules.Where(m => m.Defense is not null).ToArray();
        Assert.NotEmpty(counters);
        foreach (var module in counters)
        {
            var counter = module.Defense!;
            Assert.Equal(0, counter.Operator!.Skill);
            Assert.Equal(.08, counter.ScatterPercent);
            Assert.Equal(.104, counter.EffectiveScatterPercent!.Value, 9);
        }
        foreach (var foreign in snapshot.Objects.Where(o => o.ObjectId != snapshot.PlayerShipObjectId))
            Assert.Null(foreign.Defense);
    }

    [Fact]
    public void Pause_freezes_reload_and_never_optimistically_resets_ring()
    {
        var module = new InstalledModuleSnapshot("launcher", "type", "Launcher", 0, [],
            LauncherCombat: new(6, 0, 200, ReadyAtMotionTimeMs: 10000, ReloadDurationMs: 10000, Profiles: [new("slow", 6, 9)]));
        var panel = new CommandsPanel(motionTime: () => 2500, motionMultiplier: () => 0);
        using var bitmap = new SKBitmap(500, 1800); using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, [module], 1800, "player");
        var before = panel.CommandPanelRows.Single(r => r.Weapon is not null);
        panel.Render(canvas, [module], 1800, "player");
        var after = panel.CommandPanelRows.Single(r => r.Weapon is not null);
        Assert.Equal(30, after.RemainingReloadRealSeconds); Assert.Equal(before.ReloadFraction, after.ReloadFraction);
        Assert.Equal(.25, after.ReloadFraction);
    }

    [Fact]
    public async Task Detonate_button_submits_selected_own_flight_through_real_transport()
    {
        await using var world = new PointCombatSessionFixture();
        long uiMs = 0;
        await using var handle = new GameSessionHandle(world.Connection, new SnapshotBuffer(() => uiMs * Stopwatch.Frequency / 1000));
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, timestampProvider: () => uiMs * Stopwatch.Frequency / 1000);
        using var bitmap = new SKBitmap(1920, 1800); using var canvas = new SKCanvas(bitmap);
        async Task Wait(Func<bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            while (true)
            {
                uiMs += 16; screen.Render(canvas, 1920, 1800); Assert.Null(handle.Failure);
                if (predicate()) return;
                await Task.Delay(10, timeout.Token);
            }
        }
        await Wait(() => screen.RenderStates.Any(s => s.IsPlayerShip));
        await handle.SendPointLaunchAsync(PointCombatSessionFixture.Player, PointCombatSessionFixture.Torpedo, ProjectileKind.Torpedo, 600, 0, "fast", out _);
        await handle.SetSpeedAsync(SimulationSpeed.Speed1); world.SetReal(2000); await handle.SetSpeedAsync(SimulationSpeed.Speed0);
        await Wait(() => handle.Buffer.Presented!.Snapshot.MotionTimeMs >= 500 &&
            screen.RenderStates.Any(s => s.Source.OwnProjectile is not null && s.Pose.X > 40));
        var flight = Assert.Single(screen.RenderStates, s => s.Source.OwnProjectile is not null);
        float x = (float)(960 + (flight.Pose.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
        float y = (float)(900 + (flight.Pose.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
        screen.OnMouseDown(x, y); screen.Render(canvas, 1920, 1800);
        Assert.Equal(flight.Source.ObjectId, screen.SelectedObjectId);
        var button = Assert.Single(screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.Detonate);
        Assert.True(button.Enabled);
        screen.CommandsPanel.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        await Wait(() => handle.Buffer.Presented!.Snapshot.CombatJournal.Any(e => e.Type == CombatEventType.ManuallyDetonated && e.ProjectileObjectId == flight.Source.ObjectId));
        Assert.DoesNotContain(handle.Buffer.Presented!.Snapshot.Objects, o => o.ObjectId == flight.Source.ObjectId);
        screen.OnDeactivated();
    }
}
