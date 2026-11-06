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
            LauncherCombat: new(200, ReadyAtMotionTimeMs: 10000, ReloadDurationMs: 10000, Profiles: [new("slow", 6, 9)]));
        var panel = new CommandsPanel(motionTime: () => motionMs, motionMultiplier: () => .25);
        using var bitmap = new SKBitmap(500, 1800); using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, [module], 1800, "player");
        var row = Assert.Single(panel.CommandPanelRows, r => r.Weapon?.ModuleId == "launcher");
        Assert.Equal(seconds, row.RemainingReloadRealSeconds, 6); Assert.Equal(fraction, row.ReloadFraction, 6);
        Assert.False(row.ReloadRingRect.IsEmpty); Assert.True(row.ReloadBarRect.IsEmpty);
        Assert.True(row.CaptionRect.Contains(row.ReloadRingRect));
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
            LauncherCombat: new(200, ReadyAtMotionTimeMs: 10000, ReloadDurationMs: 10000, Profiles: [new("slow", 6, 9)]));
        var panel = new CommandsPanel(motionTime: () => 2500, motionMultiplier: () => 0);
        using var bitmap = new SKBitmap(500, 1800); using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, [module], 1800, "player");
        var before = panel.CommandPanelRows.Single(r => r.Weapon is not null);
        panel.Render(canvas, [module], 1800, "player");
        var after = panel.CommandPanelRows.Single(r => r.Weapon is not null);
        Assert.Equal(30, after.RemainingReloadRealSeconds); Assert.Equal(before.ReloadFraction, after.ReloadFraction);
        Assert.Equal(.25, after.ReloadFraction);
    }
}
