using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0001Tests
{
    private static ImmutableArray<InstalledModuleSnapshot> Modules => Enumerable.Range(0, 9).Select(i =>
        new InstalledModuleSnapshot("m" + i, "type", "Module " + i, i, [], ArmorCurrent: 20, ArmorMax: 40,
            StructureCurrent: 50, StructureMax: 100, IsDestroyed: i < 3, IsCritical: i == 4,
            DisabledUntilMotionTimeMs: i == 5 ? 8000 : null)).ToImmutableArray();

    [Fact]
    public void Module_rows_have_armor_structure_disable_and_critical_marker()
    {
        var diagram = new ModuleDiagram(); var session = Guid.NewGuid();
        diagram.Update(session, 1, "player", Modules, [], 0, .25, 0);
        Assert.Equal(9, diagram.Tiles.Count); Assert.Equal(3, diagram.DestroyedCount);
        Assert.True(diagram.Tiles[4].Critical); Assert.Equal(32, diagram.Tiles[5].DisabledRealSeconds);
        Assert.Equal(20, diagram.Tiles[4].Armor); Assert.Equal(50, diagram.Tiles[4].Structure);
        using var bitmap = new SKBitmap(480, 300); using var canvas = new SKCanvas(bitmap);
        var panel = new CommandsPanel { OwnModuleDiagram = diagram, DestroyedModuleLimit = 3 };
        panel.Render(canvas, Modules, 300, "player");
        Assert.Contains(panel.CommandPanelRows.SelectMany(r => r.DetailLines), l => l.Contains(Localization.Combat("Panel.CriticalIcon")));
        Assert.Contains(panel.CommandPanelRows.SelectMany(r => r.DetailLines), l => l.Contains(Localization.Combat("Destroyed")));
    }

    [Fact]
    public void Own_hit_blinks_once_and_foreign_numeric_hit_is_ignored()
    {
        var d = new ModuleDiagram(); var session = Guid.NewGuid();
        CombatJournalEntry Hit(long id, string target) => new(id, 0, CombatEventType.ModuleHit, "enemy", target, "shot", 0, 0, Damage: 37, HitModuleId: "m4");
        d.Update(session, 1, "player", Modules, [Hit(1, "player")], 0, .25, 0);
        Assert.Null(d.Tiles[4].HitDamage); // loading history is not a new hit
        d.Update(session, 2, "player", Modules, [Hit(1, "player"), Hit(2, "enemy")], 0, .25, 100);
        Assert.Null(d.Tiles[4].HitDamage);
        d.Update(session, 3, "player", Modules, [Hit(3, "player")], 0, .25, 200);
        Assert.Equal(37, d.Tiles[4].HitDamage);
        d.Update(session, 3, "player", Modules, [Hit(3, "player")], 0, .25, 1000);
        Assert.Null(d.Tiles[4].HitDamage);
        d.Update(Guid.NewGuid(), 1, "player", Modules, [Hit(3, "player")], 0, .25, 1001);
        Assert.Null(d.Tiles[4].HitDamage);
    }

    [Fact]
    public void Hit_indicator_expires_in_real_time_while_disabled_clock_stays_paused()
    {
        var d = new ModuleDiagram();
        d.Update(null, 1, "player", Modules, [], 1000, 0, 0);
        CombatJournalEntry hit = new(1, 1000, CombatEventType.ModuleHit, "enemy", "player", "shot", 0, 0, Damage: 37, HitModuleId: "m4");
        using var bitmap = new SKBitmap(480, 300); using var canvas = new SKCanvas(bitmap);
        d.Update(null, 2, "player", Modules, [hit], 1000, 0, 100);
        var lines = CommandsPanel.ModuleDetails("Navigation", null, Modules, d);
        Assert.Contains(lines, l => l.Contains("37"));
        d.Update(null, 2, "player", Modules, [hit], 1000, 0, 225);
        lines = CommandsPanel.ModuleDetails("Navigation", null, Modules, d);
        Assert.Contains(lines, l => l.Contains("37"));
        Assert.Equal(37, d.Tiles[4].HitDamage);
        Assert.Equal(28, d.Tiles[5].DisabledRealSeconds);
    }

    [Fact]
    public async Task Critical_flag_comes_from_actual_configured_engine_rules()
    {
        var source = PointCombatSessionFixture.Registry();
        // Use an installed role rather than assuming a particular module registry order.
        await using var baseline = new PointCombatSessionFixture(source, launchHostile: false);
        string role = baseline.Engine.CaptureSnapshot().InstalledModules.First(m => m.CombatRole is not null).CombatRole!;
        var rules = source.CombatRules! with { ShipDestruction = source.CombatRules!.ShipDestruction with { CriticalRoles = [role] } };
        await using var world = new PointCombatSessionFixture(PointCombatSessionFixture.Registry(rules: rules), launchHostile: false);
        var snapshot = world.Engine.CaptureSnapshot();
        Assert.Contains(snapshot.InstalledModules, m => m.IsCritical);
        foreach (var module in snapshot.InstalledModules) Assert.Equal(module.CombatRole == role, module.IsCritical);
    }

    [Theory]
    [InlineData(1f, 1920, 1080)]
    [InlineData(1.5f, 1920, 1080)]
    [InlineData(1.5f, 1280, 720)]
    public void Actual_screen_moves_module_details_to_left_panels(float scale, int width, int height)
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip)], "player",
            InstalledModules: Modules, CombatJournal: [], Encounter: new(CombatEncounterState.Active, 0, null, new()));
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, uiScale: scale);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        Assert.Equal(9, screen.CommandsPanel.OwnModuleDiagram!.Tiles.Count);
        Assert.True(screen.ObjectInfoPanel.RowBodyRects[0].Height > 0);
        Assert.Equal(9, screen.CommandsPanel.OwnModuleDiagram!.Tiles.Count);
        Assert.Contains(screen.CommandsPanel.CommandPanelRows.SelectMany(r => r.DetailLines), s => s.Contains(Localization.Combat("Panel.Armor")) && s.Contains("20"));
        Assert.Contains(screen.CommandsPanel.CommandPanelRows.SelectMany(r => r.DetailLines), s => s.Contains(Localization.Combat("Panel.CriticalIcon")));
        if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, $"module-diagram-{width}-{height}-{scale}.png")); png.SaveTo(file);
        }
        screen.OnDeactivated();
    }
}
