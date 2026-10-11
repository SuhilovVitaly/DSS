using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using Fixture = DeepSpaceSaga.Client.Tests.TorpedoCommandPanelTests.Fixture;

namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeCombatInfoTests
{
    private static CountermeasureSnapshot Flight => new("player", "defense-1", "target", CountermeasurePhase.Guiding,
        new(1000, 1, TorpedoRoutePhase.Straight, true, [new(11000, 11000, 0, 12, 0, 60000)], ElapsedMs: 2000), 1000, 250,
        new(new("crew", "Operator", WeaponSkillType.CountermeasureDefense, 99, 55, 55), 5),
        Mode: LaunchMode.Manual, CapturedAccuracy: 30, CapturedTargetManeuverability: 5,
        MaxFlightTimeMs: 60000, ExpiresAtMotionTimeMs: 61000);

    private static InstalledModuleSnapshot Module(string id, decimal accuracy, int chance) => new(id,
        "module.countermeasure.launcher.basic", "Defense", id == "defense-1" ? 2 : 3,
        [DefenseCommandTypes.Fire], "On", "Ready", 60, Commands: [new(DefenseCommandTypes.Fire, "Fire", "object")],
        Defense: new(false, Flight.RatingBreakdown.DefenseOperator, DefenseState.Ready),
        DefensePreview: new("player", id, "target", chance, true, Accuracy: accuracy, TargetManeuverability: 5));

    private static CommandPanelGeometry Open(GameSessionScreen screen, string id)
    {
        TorpedoCommandPanelTests.Render(screen);
        var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == id);
        if (!row.Opened) screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        TorpedoCommandPanelTests.Render(screen);
        return screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == id);
    }

    [Fact]
    public async Task Each_panel_uses_own_authoritative_preview_and_active_frozen_chance()
    {
        await using var f = new Fixture();
        f.Update(s => s with
        {
            GameTimeMs = 999000,
            SimulationTimeMs = 3000,
            InstalledModules = s.InstalledModules.Add(Module("defense-1", 55, 500) with
            { Defense = new(false, Flight.RatingBreakdown.DefenseOperator, DefenseState.Guiding, "pr") })
                .Add(Module("defense-2", 75, 700)),
            Objects = s.Objects.Add(new("pr", 11000, 11000, 0, 0, Countermeasure: Flight))
        });
        TorpedoCommandPanelTests.SelectTarget(f);
        var first = Open(f.Screen, "defense-1");
        Assert.Contains(first.DetailLines, l => l.Contains("55") && l.Contains($"{50m:0.0}%"));
        Assert.Contains(first.DetailLines, l => l.Contains($"{25m:0.0}%"));
        Assert.Contains(first.DetailLines, l => l.Contains($"{58m:0.0}"));
        var second = Open(f.Screen, "defense-2");
        Assert.Contains(second.DetailLines, l => l.Contains("75") && l.Contains($"{70m:0.0}%"));
        Assert.DoesNotContain(second.DetailLines, l => l.Contains($"{25m:0.0}%"));
        Assert.Equal(first.DetailLines.ToArray(), Open(f.Screen, "defense-1").DetailLines.ToArray());
        f.Screen.OnMouseDown(740, 420);
        Assert.DoesNotContain(Open(f.Screen, "defense-2").DetailLines, l => l.Contains("→"));
    }

    [Fact]
    public async Task Flight_and_reload_timers_are_not_confused()
    {
        await using var f = new Fixture();
        f.Update(s => s with
        {
            SimulationTimeMs = 3000,
            InstalledModules = s.InstalledModules.Add(
            Module("defense-1", 55, 500) with { Defense = new(false, null, DefenseState.Reloading, ReloadDueMotionTimeMs: 8000) })
        });
        var row = Open(f.Screen, "defense-1");
        Assert.Contains($"{5m:0.0}", row.StatusBarText);
        Assert.Empty(row.DetailLines);
        Assert.StartsWith($"{58m:0.0}", GameSessionScreen.RemainingFlightText(Flight, 3000));
        Assert.StartsWith($"{1m:0.0}", GameSessionScreen.RemainingFlightText(Flight with
        { Phase = CountermeasurePhase.MissedCoast, MissExpiresAtMotionTimeMs = 4000 }, 3000));
        Assert.StartsWith($"{0m:0.0}", GameSessionScreen.RemainingFlightText(Flight, 999999));
        Assert.Equal("—", GameSessionScreen.RemainingFlightText(Flight with { ExpiresAtMotionTimeMs = null }, 3000));
    }

    [Fact]
    public void Selected_projectile_uses_captured_facts_and_physical_remaining_lifetime()
    {
        var lines = ObjectInfoPanel.CountermeasureLines(Flight);
        string Value(string key, string fallback) => lines.Single(l => l.Label == GameSessionScreen.CombatText(key, fallback)).Value;
        Assert.Equal("player", Value("Owner", "Носитель"));
        Assert.Equal("defense-1", Value("Module", "Аппарат"));
        Assert.Equal("target", Value("Target", "Цель"));
        Assert.Equal(GameSessionScreen.CombatText("Manual", "Ручной"), Value("LaunchMode", "Режим пуска"));
        Assert.Equal("30", Value("Accuracy", "Точность"));
        Assert.Equal("5", Value("Maneuverability", "манёвренность"));
        Assert.Equal($"{25m:0.0}%", Value("FrozenChance", "Шанс активной ПРО"));
        Assert.StartsWith($"{58m:0.0}", Value("RemainingFlight", "Осталось полёта"));
        Assert.StartsWith($"{60m:0.0}", Value("MaxFlight", "Лимит полёта"));
        Assert.DoesNotContain(lines, l => l.Value.Contains("50+"));
        Assert.Equal("99", Value("SkillMetadata", "Навык (справочно)"));
    }

    [Theory]
    [InlineData(200)]
    [InlineData(480)]
    public void Long_details_wrap_and_remain_scrollable(int height)
    {
        string detail = string.Join(" ", Enumerable.Repeat("captured accuracy and maneuverability", 8));
        var panel = new CommandsPanel(weaponDetails: _ => [detail], weaponCommandEnabled: (_, _) => true);
        using var bitmap = new SKBitmap(400, height);
        using var canvas = new SKCanvas(bitmap);
        var modules = new[] { Module("defense-1", 55, 500), Module("defense-2", 75, 700) };
        panel.Render(canvas, modules, height, "player");
        var row = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2");
        panel.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        panel.Render(canvas, modules, height, "player");
        row = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2");
        Assert.True(row.Opened);
        Assert.True(row.DetailLines.Length > 2);
        using var paint = new SKPaint { TextSize = 12, IsAntialias = true };
        Assert.All(row.DetailLines, l => Assert.True(paint.MeasureText(l) <= CommandsPanel.PanelWidth - 20));
        Assert.True(panel.OnMouseWheel(100, 100, -100));
        panel.Render(canvas, modules, height, "player");
        Assert.InRange(panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2").StatusBarRect.Bottom, 40, height);
    }
}
